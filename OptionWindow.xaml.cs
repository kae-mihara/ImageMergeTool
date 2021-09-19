using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageMerge
{
    /// <summary>
    /// Option.xaml 的交互逻辑
    /// </summary>
    public partial class OptionWindow : Window
    {
        private OptionViewModel VM => this.DataContext as OptionViewModel;
        public OptionWindow()
        {
            InitializeComponent();
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            var vm = VM;
            try
            {
                var settings = Properties.Settings.Default;
                VM.IsMergeLastToPrevious = settings.MergeLastToPrevious;
                VM.ImageQuality = settings.ImageQuality;
                VM.ResizeRatio = settings.ResizeRatio;
                VM.FormatStr = settings.FormatStr;
            }
            catch
            {
                VM.IsMergeLastToPrevious = false;
                VM.ImageQuality = 100;
                VM.ResizeRatio = 100;
                VM.FormatStr = "jpg";
            }
            switch (vm.FormatStr)
            {
                case "jpg": btnJpg.IsChecked = true; break;
                //case "bmp": btnBmp.IsChecked = true; break;
                case "png": btnPng.IsChecked = true; break;
                case "webp": btnWbp.IsChecked = true; break;
            };
        }

        private void SaveExit_Click(object sender, RoutedEventArgs e)
        {
            var vm = VM;
            var settings = Properties.Settings.Default;
            var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
            settings.MergeLastToPrevious = vm.IsMergeLastToPrevious;
            settings.ImageQuality = vm.ImageQuality;
            settings.ResizeRatio = vm.ResizeRatio;
            settings.FormatStr = vm.FormatStr;
            settings.Save();
            this.DialogResult = true;
            this.Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var btn = (sender as RadioButton);
            VM.FormatStr = (btn.Content as string);          
        }
    }
}
