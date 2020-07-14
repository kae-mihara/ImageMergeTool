using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SkiaSharp;

namespace ImageMerge
{
    class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {

        }
        #region properties

        private List<SelectImageViewModel>  _selectImageViewModels;

        public List<SelectImageViewModel>  SelectImageViewModels
        {
            get { return _selectImageViewModels; }
            set 
            {
                if (_selectImageViewModels != value)
                {
                    _selectImageViewModels = value;
                    OnPropertyChanged("SelectImageViewModels");
                    OnPropertyChanged("SelectedImagePaths");
                    PreviewMergeResult();
                }
            }
        }

        public List<(string,string)> SelectedImagePaths => SelectImageViewModels
            ?.Where(vm => vm.IsSelected)
            ?.Select(vm => (vm.ImageName, vm.ImageShortName)).ToList() 
            ?? null;
        private string _folderPath;

        public string FolderPath
        {
            get { return _folderPath; }
            set { _folderPath = value; OnPropertyChanged("FolderPath"); }
        }
        public string OutputPath => $"{FolderPath ?? ""}\\Merged Images";

        private int _concatNum;

        public int ConcatNum
        {
            get { return _concatNum; }
            set 
            { 
                if(value <= 1 || value >= (_selectImageViewModels?.Count ?? int.MaxValue)) { return; }
                _concatNum = value; 
                OnPropertyChanged("ConcatNum"); PreviewMergeResult(); 
            }
        }

        private List<PreviewImageViewModel> _previewImageViewModels;

        public List<PreviewImageViewModel> PreviewImageViewModels
        {
            get { return _previewImageViewModels; }
            set { _previewImageViewModels = value; OnPropertyChanged("PreviewImageViewModels"); }
        }

        private int _progressValue;

        public int ProgressValue
        {
            get { return _progressValue; }
            set { _progressValue = value; OnPropertyChanged("ProgressValue"); OnPropertyChanged("IsFinished"); }
        }
        public bool IsFinished => _progressValue == (_previewImageViewModels?.Count ?? 0) && _progressValue != 0;
        private bool _checkAll = true;

        public bool CheckAll
        {
            get { return _checkAll; }
            set {
                if (_checkAll == value || SelectImageViewModels is null) return;
                _checkAll = value;
                //SelectImageViewModels.ForEach(vm => vm.SetIsSelectedWithoutNotify(value));
                SelectImageViewModels.ForEach(vm => vm.IsSelected = value);
                OnPropertyChanged("CheckAll");
                OnPropertyChanged("SelectedImagePaths");
                PreviewMergeResult();
            }
        }
        public bool InverseCheck
        {
            set
            {
                if(SelectImageViewModels is null) return;
                SelectImageViewModels.ForEach(vm => vm.IsSelected = !vm.IsSelected);
                OnPropertyChanged("SelectedImagePaths");
                PreviewMergeResult();
            }
        }
        #endregion

        internal void SetFilePaths(string folderPath = null)
        {
            if (folderPath == FolderPath) return;
            if (!string.IsNullOrWhiteSpace(folderPath)) { FolderPath = folderPath; }
            if (string.IsNullOrWhiteSpace(FolderPath)) { return; }
            try
            {
                var filePaths = System.IO.Directory.GetFiles(FolderPath);
                var fileNames = filePaths.Select(path => path[(path.LastIndexOf('\\') + 1)..path.Length])
                                         .Where(path => path.EndsWith("png", StringComparison.OrdinalIgnoreCase) ||
                                                        path.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
                                                        path.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase));             
                SelectImageViewModels = fileNames
                    .Select(str => new SelectImageViewModel() { ImageName = str })
                    .ToList();
                ProgressValue = 0;
            }
            catch
            {
                MessageBox.Show("输入的文件夹路径无效");
            }
        }
        internal bool PreviewMergeResult()
        {
            var selectedImagePaths = SelectedImagePaths;
            if (selectedImagePaths is null) return false;
            var previewVMs = new List<PreviewImageViewModel>((selectedImagePaths.Count / ConcatNum));

            int final_legenth = selectedImagePaths.Count % ConcatNum;
            int final_index = selectedImagePaths.Count - final_legenth;
            for (int i = 0; i < selectedImagePaths.Count; i = i + ConcatNum)
            {
                var currentGroupImagePaths = selectedImagePaths.GetRange(i, i == final_index ? final_legenth : ConcatNum).ToList();
                previewVMs.Add(new PreviewImageViewModel()
                {
                    AllImageNames = currentGroupImagePaths.Aggregate(new StringBuilder(), (sb, t)=>sb.AppendLine(t.Item2)).ToString(),
                    ImageNames =  currentGroupImagePaths.Select(t => t.Item1).ToList(),
                    IsCreated = false
                });
            }
            PreviewImageViewModels = previewVMs;
            return true;
        }
        private static void SaveImage(string savePath, SKImage mergedImage)
        {
            using SKData encoded = mergedImage.Encode(SKEncodedImageFormat.Jpeg, 100);
            using var outFile = File.OpenWrite(savePath);
            encoded.SaveTo(outFile);
        }
        internal async Task MergeAndSaveImagesAsync(Dispatcher uiDispatcher)
        {
            if(_previewImageViewModels is null && !PreviewMergeResult())
            {
                return;
            }
            
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            await Task.Run(() =>
            {
                //每次循环处理一组图片
                for (int i = 0; i < _previewImageViewModels.Count; i++)
                {
                    //读取图片
                    var currentPreviewVM = _previewImageViewModels[i];
                    var mergingImages = currentPreviewVM.ImageNames
                        .Select(imgName =>
                        {
                            try
                            {
                                var path = $"{FolderPath}\\{imgName}";
                                using var file = new SKManagedStream(File.Open(path, FileMode.Open));
                                return SKBitmap.Decode(file);
                            }
                            catch { return null; }
                        })
                        .Where(image => image != null)
                        .ToList();
                    try
                    {
                        //开始拼接图片
                        var mergedWidth = mergingImages.First().Width;
                        var mergedHeight = mergingImages.Aggregate(0, (accumHeight, image) => accumHeight + image.Height);

                        using (var tempSurface = SKSurface.Create(new SKImageInfo(mergedWidth, mergedHeight)))
                        {
                            var canvas = tempSurface.Canvas;
                            //set background color
                            canvas.Clear(SKColors.Transparent);

                            //go through each image and draw it on the final image
                            int offsetX = 0;
                            int offsetY = 0;
                            foreach (var image in mergingImages)
                            {
                                if (image.Width > mergedWidth)
                                {
                                    var resizeHeight = (int)((float)image.Height / image.Width * mergedWidth);
                                    using var resizedImage = image.Resize(new SKImageInfo(mergedWidth, resizeHeight), SKFilterQuality.High);
                                    canvas.DrawBitmap(image, SKRect.Create(offsetX, offsetY, resizedImage.Width, resizedImage.Height));
                                    offsetY += resizeHeight;
                                }
                                else
                                {
                                    canvas.DrawBitmap(image, SKRect.Create(offsetX, offsetY, image.Width, image.Height));
                                    offsetY += image.Height;
                                }
                            }
                            var mergedImage = tempSurface.Snapshot(SKRectI.Create(0, 0, mergedWidth, offsetY));
                            //保存图片
                            SaveImage($"{OutputPath}\\{i:D2}.jpg", mergedImage);

                            uiDispatcher.Invoke(() =>
                            {
                                currentPreviewVM.IsCreated = true;
                                ProgressValue = i + 1;
                            });
                        }
                    }
                    finally
                    {
                        //释放资源
                        foreach (var image in mergingImages)
                        {
                            image.Dispose();
                        }
                    }
                }
            }).ConfigureAwait(true);
        }

        public void OnPropertyChanged(string name)=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}