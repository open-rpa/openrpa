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

namespace OpenRPA.Java.Views
{
    /// <summary>
    /// Interaction logic for WindowsClickDetectorView.xaml
    /// </summary>
    public partial class RecordPluginView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        bool init = true;
        public RecordPluginView()
        {
            InitializeComponent();
            DataContext = this;
            auto_launch_java_bridge.IsChecked = PluginConfig.auto_launch_java_bridge;
            init = false;
        }
        private void auto_launch_java_bridge_Checked(object sender, RoutedEventArgs e)
        {
            if (init) return;
            if (auto_launch_java_bridge.IsChecked == null) return;
            PluginConfig.auto_launch_java_bridge = auto_launch_java_bridge.IsChecked.Value;
            Config.Save();
        }
        private void launch_java_bridge_Click(object sender, RoutedEventArgs e)
        {
            Javahook.EnsureJavaBridge();
        }
    }
}
