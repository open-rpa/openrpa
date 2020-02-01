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

namespace OpenRPA.PDPlugin.Views
{
    /// <summary>
    /// Interaction logic for WindowsClickDetectorView.xaml
    /// </summary>
    public partial class PluginView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        private PDPlugin plugin = null;
        public PluginView(PDPlugin plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
            DataContext = this;
            enabled_mouse_recording.IsChecked = PluginConfig.enabled_mouse_recording;
            enabled_keyboard_recording.IsChecked = PluginConfig.enabled_keyboard_recording;
            collectionname.Text = PluginConfig.collectionname;
        }
        private void enabled_mouse_recording_IsEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (enabled_mouse_recording.IsChecked == null) return;
            PluginConfig.enabled_mouse_recording = enabled_mouse_recording.IsChecked.Value;
            Config.Save();
            plugin.Initialize();
        }
        private void enabled_keyboard_recording_IsEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (enabled_keyboard_recording.IsChecked == null) return;
            PluginConfig.enabled_keyboard_recording = enabled_keyboard_recording.IsChecked.Value;
            Config.Save();
            plugin.Initialize();
        }
        private void collectionname_Changed(object sender, RoutedEventArgs e)
        {
            if ( string.IsNullOrEmpty(collectionname.Text)) return;
            PluginConfig.collectionname = collectionname.Text;
            Config.Save();
            plugin.Initialize();
        }
    }
}
