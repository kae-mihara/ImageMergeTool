using Microsoft.Win32;
using System;
using System.ComponentModel;
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
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            try
            {
                VM.ConcatNum = int.Parse(System.Configuration.ConfigurationManager.AppSettings["ConcatNum"]);
                VM.FolderPath = System.Configuration.ConfigurationManager.AppSettings["LastFolderPath"];
            }
            catch
            {
                VM.ConcatNum = 5;
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
            config.AppSettings.Settings["ConcatNum"].Value = VM.ConcatNum.ToString();
            config.AppSettings.Settings["LastFolderPath"].Value = VM.FolderPath;
            config.Save(System.Configuration.ConfigurationSaveMode.Modified);
            System.Configuration.ConfigurationManager.RefreshSection("appSettings");
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
    }
}
