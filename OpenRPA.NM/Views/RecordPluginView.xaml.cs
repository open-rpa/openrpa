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
            wait_for_tab_after_set_value.IsChecked = PluginConfig.wait_for_tab_after_set_value;
            wait_for_tab_click.IsChecked = PluginConfig.wait_for_tab_click;
            compensate_for_old_addon.IsChecked = PluginConfig.compensate_for_old_addon;
            debug_console_output.IsChecked = PluginConfig.debug_console_output;
        }
        private void value_Changed(object sender, RoutedEventArgs e)
        {
            if (wait_for_tab_after_set_value.IsChecked == null) return;
            PluginConfig.wait_for_tab_after_set_value = wait_for_tab_after_set_value.IsChecked.Value;
            PluginConfig.wait_for_tab_click = wait_for_tab_click.IsChecked.Value;
            PluginConfig.compensate_for_old_addon = compensate_for_old_addon.IsChecked.Value;
            PluginConfig.debug_console_output = debug_console_output.IsChecked.Value;
            Config.Save();
        }
    }
}
