using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using SkiaSharp;

namespace ImageMerge
{
    class OptionViewModel : INotifyPropertyChanged
    {
        public OptionViewModel() { }
        private bool _isSelected = true;

        public bool IsSelected
        {
            get { return _isSelected; }
            set 
            {
                if (_isSelected == value) return;
                _isSelected = value; OnPropertyChanged("IsSelected");
            }
        } 
        public int _imageQuality { get; set; }
        public int ImageQuality
        {
            get => _imageQuality;
            set
            {
                if (value == _imageQuality) return;
                _imageQuality = value;
                OnPropertyChanged("ImageQuality");
            }
        }
        public int _resizeRatio { get; set; }
        public int ResizeRatio
        {
            get => _resizeRatio;
            set
            {
                if (value == _resizeRatio) return;
                _resizeRatio = value;
                OnPropertyChanged("ResizeRatio");
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
            }
        }
        public string FormatStr { get; internal set; }

        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
