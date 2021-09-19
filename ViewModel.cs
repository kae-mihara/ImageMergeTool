using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                if(value <= 1 || value > (_selectImageViewModels?.Count ?? int.MaxValue)) { return; }
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
        private bool _mergeOrder = true;

        public bool MergeOrder
        {
            get { return _mergeOrder; }
            set
            {
                if (_mergeOrder == value) return;
                _mergeOrder = value;
                OnPropertyChanged("MergeOrder");
                PreviewMergeResult();
            }
        }
        private bool _mergeOrientation = true;

        public bool MergeOrientation
        {
            get { return _mergeOrientation; }
            set
            {
                if (_mergeOrientation == value) return;
                _mergeOrientation = value;
                OnPropertyChanged("MergeOrientation");
                //PreviewMergeResult();
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

        private bool _isMergeLastToPrevious;

        public bool IsMergeLastToPrevious
        {
            get { return _isMergeLastToPrevious; }
            set 
            {
                if (_isMergeLastToPrevious == value) return;
                _isMergeLastToPrevious = value;
                OnPropertyChanged("IsMergeLastToPrevious");
                PreviewMergeResult();
            }
        }

        public int ImageQuality { get; internal set; }
        public int ResizeRatio { get; internal set; }
        public string FormatStr { get; set; }
        public SKEncodedImageFormat Format => FormatStr switch
        {
            "jpg" => SKEncodedImageFormat.Jpeg,
            "bmp" => SKEncodedImageFormat.Bmp,
            "png" => SKEncodedImageFormat.Png,
            "webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg
        };
    #endregion

    private Regex _getNumFromFilePath = new Regex(@"\d+", RegexOptions.RightToLeft);
        internal void SetFilePaths(string folderPath = null)
        {
            //if (folderPath == FolderPath) return;
            if (!string.IsNullOrWhiteSpace(folderPath)) { FolderPath = folderPath; }
            if (string.IsNullOrWhiteSpace(FolderPath)) { return; }
            try
            {
                var filePaths = System.IO.Directory.GetFiles(FolderPath);
                var fileNames = filePaths.Select(path => path[(path.LastIndexOf('\\') + 1)..path.Length])
                                         .Where(path => path.EndsWith("bmp", StringComparison.OrdinalIgnoreCase) ||
                                                        path.EndsWith("png", StringComparison.OrdinalIgnoreCase) ||
                                                        path.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
                                                        path.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase));
                //Sort Files
                var orderedFileNames = new SortedDictionary<int, string> ();
                var  unorderedFileNames = new List<string>();
                foreach (var s in fileNames)
                {
                    var numStr = _getNumFromFilePath.Match(s).Value;
                    if (string.IsNullOrWhiteSpace(numStr))
                    {
                        unorderedFileNames.Add(s);
                    }
                    else
                    {
                        orderedFileNames[int.Parse(numStr)] = s;
                    }
                }

                SelectImageViewModels = orderedFileNames.Values
                    .Concat(unorderedFileNames)
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
            if (!MergeOrder) selectedImagePaths.Reverse();

            int lastLegenth = selectedImagePaths.Count % ConcatNum;
            var groupCount = selectedImagePaths.Count / ConcatNum;
            if(lastLegenth != 0)
            {
                if (_isMergeLastToPrevious) { lastLegenth += _concatNum; }
                else { groupCount++; }
            }
            else { lastLegenth = _concatNum; }

            var previewVMs = new List<PreviewImageViewModel>(groupCount);

            for (int i = 0; i < groupCount; i++)
            {
                var curGroupLength = (i < groupCount - 1) ? _concatNum : lastLegenth;
                var currentGroupImagePaths = selectedImagePaths.GetRange(i*_concatNum, curGroupLength).ToList();
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
        private void SaveImage(string savePath, SKImage mergedImage)
        {
            using SKData encoded = mergedImage.Encode(Format, ImageQuality);
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
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                MessageBox.Show("请先打开一个文件夹");
            }
            var errorMsg = new StringBuilder();
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
                            catch(FileNotFoundException e)
                            {
                                errorMsg.AppendLine($"没有找到{ e.FileName}");
                                return null;
                            }
                            catch{ return null; }
                        })
                        .Where(image => image != null)
                        .ToList();
                    try
                    {
                        //开始拼接图片
                        var resizeRatio = ResizeRatio * 0.01;
                        var mergedHeight = 0;
                        var mergedWidth = 0;
                        var mergeDirectionLength = 0;
                        var targetWidth = 0;
                        if (_mergeOrientation)
                        {
                            mergedWidth = (int)Math.Round(mergingImages.First().Width * resizeRatio);
                            mergedHeight = (int)Math.Round(mergingImages.Aggregate(0, (accumHeight, image) => accumHeight + image.Height) * resizeRatio);
                            mergeDirectionLength = mergedHeight;
                            targetWidth = mergedWidth;
                        }
                        else
                        {
                            mergedWidth = (int)Math.Round(mergingImages.Aggregate(0, (accumWidth, image) => accumWidth + image.Width) * resizeRatio);
                            mergedHeight = (int)Math.Round(mergingImages.First().Height * resizeRatio);
                            mergeDirectionLength = mergedWidth;
                            targetWidth = mergedHeight;
                        }

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
                                var mergeDirectionStep = _mergeOrientation ? image.Height : image.Width;
                                var stackWidth = _mergeOrientation ? image.Width : image.Height;
                                if (stackWidth > targetWidth)
                                {
                                    var resizeLength = (int)((float)mergeDirectionStep / stackWidth * targetWidth);
                                    var resizeTargetSize = _mergeOrientation
                                    ? new SKImageInfo(targetWidth, resizeLength)
                                    : new SKImageInfo(resizeLength, targetWidth);
                                    using var resizedImage = image.Resize(resizeTargetSize, SKFilterQuality.High);
                                    canvas.DrawBitmap(resizedImage, SKRect.Create(offsetX, offsetY, resizedImage.Width, resizedImage.Height));
                                    if (_mergeOrientation) { offsetY += resizeLength; }
                                    else { offsetX += resizeLength; }
                                }
                                else
                                {
                                    canvas.DrawBitmap(image, SKRect.Create(offsetX, offsetY, image.Width, image.Height));
                                    if (_mergeOrientation) { offsetY += image.Height; }
                                    else { offsetX += image.Width; }
                                }
                            }

                            var mergedImage =  tempSurface.Snapshot(_mergeOrientation
                                ? SKRectI.Create(0, 0, mergedWidth, offsetY)
                                : SKRectI.Create(0, 0, offsetX, mergedHeight));
                            //保存图片
                            SaveImage($"{OutputPath}\\{i:D2}.{FormatStr}", mergedImage);

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
            //弹出错误框
            var msg = errorMsg.ToString();
            if (!string.IsNullOrWhiteSpace(msg))
            {
                MessageBox.Show(msg);
            }
        }

        public void OnPropertyChanged(string name)=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}