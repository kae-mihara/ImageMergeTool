using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;

namespace ImageMerge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel VM => DataContext as ViewModel;
        public MainWindow()
        {
            InitializeComponent();    
        }

        void ReadSettings()
        {
            try
            {
                //System.Configuration.ConfigurationManager.
                var settings = Properties.Settings.Default;
                VM.ConcatNum = settings.ConcatNum;                       
                VM.IsMergeLastToPrevious = settings.MergeLastToPrevious; 
                VM.ImageQuality = settings.ImageQuality; 
                VM.ResizeRatio = settings.ResizeRatio;   
                VM.FormatStr = settings.FormatStr;       
            }
            catch
            {
                VM.ConcatNum = 5;
                VM.IsMergeLastToPrevious = false;
                VM.ImageQuality = 100;
                VM.ResizeRatio = 100;
                VM.FormatStr = "jpg";
            }
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            VM.FolderPath = System.Configuration.ConfigurationManager.AppSettings["LastFolderPath"];
            ReadSettings();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            var settings = Properties.Settings.Default;
            settings.ConcatNum = VM.ConcatNum;
            settings.LastFolderPath = VM.FolderPath;
            settings.Save();
        }

        private void OnBroserFolderClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog(this) ?? false;
            if (result)
            {
                var name = dialog.FileName;
                var folderPath = name.Substring(0, name.LastIndexOf('\\'));
                VM.SetFilePaths(folderPath);
            }
        }
        private void OnOpenFolderClick(object sender, RoutedEventArgs e) => VM.SetFilePaths();
        private async void OnSaveClickAsync(object sender, RoutedEventArgs e)
        {
            await VM.MergeAndSaveImagesAsync(this.Dispatcher);

        }
        private void OnOpenOutputClick(object sender, RoutedEventArgs e)
        {
            if (!VM.IsFinished) return;
            Process.Start("explorer", VM.OutputPath);
        }

        private void OnSelectItemClick(object sender, RoutedEventArgs e)
        {
            VM.OnPropertyChanged("SelectedImagePaths");
            VM.PreviewMergeResult();
            //var vm = (sender as FrameworkElement).DataContext as SelectImageViewModel;        
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            var result = new OptionWindow().ShowDialog() ?? false;
            if (result)
            {
                ReadSettings();
            }
        }
    }
}
