using System;
using System.ComponentModel;

namespace OpenRPA.Activities
{
    public partial class GetPasswordDesigner
    {
        public GetPasswordDesigner()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
