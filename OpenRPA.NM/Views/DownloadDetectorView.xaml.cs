using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
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

namespace OpenRPA.NM.Views
{
    /// <summary>
    /// Interaction logic for WindowsClickDetectorView.xaml
    /// </summary>
    public partial class DownloadDetectorView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public DownloadDetectorView(DownloadDetectorPlugin plugin)
        {
            InitializeComponent();
            DataContext = this;
            // wait_for_tab_after_set_value.IsChecked = PluginConfig.wait_for_tab_after_set_value;
        }
        private void value_Changed(object sender, RoutedEventArgs e)
        {
            // if (wait_for_tab_after_set_value.IsChecked == null) return;
            Config.Save();
        }
    }
}
