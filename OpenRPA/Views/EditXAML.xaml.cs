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

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for EditXAML.xaml
    /// </summary>
    public partial class EditXAML : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public string _XAML;
        public string XAML
        {
            get { return _XAML; }
            set
            {
                _XAML = value;
                NotifyPropertyChanged("XAML");
            }
        }
        public EditXAML()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
