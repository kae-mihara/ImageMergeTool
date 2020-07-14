using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace ImageMerge
{
    class SelectImageViewModel : INotifyPropertyChanged
    {
        public SelectImageViewModel() { }
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
        public string ImageName { get; set; }
        public string ImageShortName
        {
            get
            {
                var name = ImageName;
                var len = name.Length;
                if (len <= 23) return name;
                string last10 = name.Substring(len - 10, 10);
                return $"{name[0..10]}...{last10}";
            }
        }

        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
