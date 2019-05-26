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
        public KeyboardDetectorView(KeyboardDetectorPlugin plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
            DataContext = this;
        }
        private KeyboardDetectorPlugin plugin;
        public entity.Detector Entity
        {
            get
            {
                return plugin.Entity;
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
        public string Processname
        {
            get
            {
                if (Entity == null) return string.Empty;
                return plugin.Processname;
            }
            set
            {
                plugin.Processname = value;
                NotifyPropertyChanged("Processname");
            }
        }
        public string Keys
        {
            get
            {
                return plugin.Keys;
            }
            set
            {
                plugin.Keys = value;
                NotifyPropertyChanged("Keys");
                NotifyPropertyChanged("Entity");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var view = new KeyboardSeqWindow();
            if(view.ShowDialog() == true)
            {
                Keys = view.Text;
                plugin.ParseText(view.Text);
            }

        }
    }
}
