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

namespace OpenRPA.AviRecorder.Views
{
    /// <summary>
    /// Interaction logic for RunPluginView.xaml
    /// </summary>
    public partial class RunPluginView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public RunPluginView()
        {
            InitializeComponent();
            DataContext = this;
        }
        public bool addonEnabled
        {
            get
            {
                return PluginConfig.enabled;
            }
            set
            {
                PluginConfig.enabled = value;
                NotifyPropertyChanged("addonEnabled");
            }
        }
        public bool keepsuccessful
        {
            get
            {
                return PluginConfig.keepsuccessful;
            }
            set
            {
                PluginConfig.keepsuccessful = value;
                NotifyPropertyChanged("keepsuccessful");
            }
        }
        public string folder
        {
            get
            {
                return PluginConfig.folder;
            }
            set
            {
                PluginConfig.folder = value;
                NotifyPropertyChanged("folder");
            }
        }
        public string codec
        {
            get
            {
                return PluginConfig.codec;
            }
            set
            {
                PluginConfig.codec = value;
                NotifyPropertyChanged("codec");
            }
        }
        public string quality
        {
            get
            {
                return PluginConfig.quality.ToString();
            }
            set
            {
                try
                {
                    PluginConfig.quality = int.Parse(value);
                }
                catch (Exception)
                {
                }
                NotifyPropertyChanged("quality");
            }
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Config.Save();
            }
            catch (Exception)
            {
            }
        }
    }
}
