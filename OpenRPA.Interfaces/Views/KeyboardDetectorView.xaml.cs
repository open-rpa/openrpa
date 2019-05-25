    using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenRPA.Interfaces.Views
{
    /// <summary>
    /// Interaction logic for KeyboardDetectorView.xaml
    /// </summary>
    public partial class KeyboardDetectorView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public KeyboardDetectorView(IDetectorPlugin plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
            DataContext = this;
        }
        private IDetectorPlugin plugin;
        public entity.Detector Entity
        {
            get
            {
                return plugin.Entity as entity.Detector;
            }
        }
        public string EntityName
        {
            get
            {
                if (Entity == null) return string.Empty;
                return Entity.name;
            }
            set
            {
                Entity.name = value;
                NotifyPropertyChanged("Entity");
            }
        }
    }
}
