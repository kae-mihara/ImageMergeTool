using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace ImageMerge
{
    class PreviewImageViewModel:INotifyPropertyChanged
    {
        private bool _isCreated;

        public bool IsCreated
        {
            get { return _isCreated; }
            set { _isCreated = value; OnPropertyChanged("IsCreated"); }
        }

        private List<string> _imageNames;

        public List<string> ImageNames
        {
            get { return _imageNames; }
            set { _imageNames = value; OnPropertyChanged("AllImageNames"); }
        }

        public string AllImageNames { get; set; }

        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
