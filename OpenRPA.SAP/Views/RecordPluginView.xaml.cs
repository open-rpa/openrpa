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

namespace OpenRPA.SAP.Views
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
        public RecordPluginView()
        {
            InitializeComponent();
            DataContext = this;
            auto_launch_sap_bridge.IsChecked = PluginConfig.auto_launch_sap_bridge;
            record_with_get_element.IsChecked = PluginConfig.record_with_get_element;
            bridge_timeout_seconds.Text = PluginConfig.bridge_timeout_seconds.ToString();
        }
        private void auto_launch_SAP_bridge_Checked(object sender, RoutedEventArgs e)
        {
            if (auto_launch_sap_bridge.IsChecked == null) return;
            PluginConfig.auto_launch_sap_bridge = auto_launch_sap_bridge.IsChecked.Value;
            PluginConfig.record_with_get_element = record_with_get_element.IsChecked.Value;
            if(string.IsNullOrEmpty(bridge_timeout_seconds.Text))
            {
                PluginConfig.bridge_timeout_seconds = 60;
            }
            else
            {
                try
                {
                    PluginConfig.bridge_timeout_seconds = int.Parse(bridge_timeout_seconds.Text);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            Config.Save();
        }
        private void launch_SAP_bridge_Click(object sender, RoutedEventArgs e)
        {
            SAPhook.EnsureSAPBridge();
        }
    }
}
