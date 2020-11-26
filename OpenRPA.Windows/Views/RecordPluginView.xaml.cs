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

namespace OpenRPA.Windows.Views
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
            allow_child_searching.IsChecked = PluginConfig.allow_child_searching;
            allow_multiple_hits_mid_selector.IsChecked = PluginConfig.allow_multiple_hits_mid_selector;
            enum_properties.IsChecked = PluginConfig.enum_selector_properties;
            get_elements_in_different_thread.IsChecked = PluginConfig.get_elements_in_different_thread;
            traverse_selector_both_ways.IsChecked = PluginConfig.traverse_selector_both_ways;
            search_descendants.IsChecked = PluginConfig.search_descendants;
            enable_cache.IsChecked = PluginConfig.enable_cache;
            force_beta_selector.IsChecked = PluginConfig.force_beta_selector;
        }
        private void allow_child_searching_IsEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (allow_child_searching.IsChecked == null) return;
            PluginConfig.allow_child_searching = allow_child_searching.IsChecked.Value;
            Config.Save();
        }
        private void allow_multiple_hits_mid_selector_IsEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (allow_multiple_hits_mid_selector.IsChecked == null) return;
            PluginConfig.allow_multiple_hits_mid_selector = allow_multiple_hits_mid_selector.IsChecked.Value;
            Config.Save();
        }
        private void enum_properties_IsEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (enum_properties.IsChecked == null) return;
            PluginConfig.enum_selector_properties = enum_properties.IsChecked.Value;
            Config.Save();
        }
        private void get_elements_in_different_thread_IsEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (get_elements_in_different_thread.IsChecked == null) return;
            PluginConfig.get_elements_in_different_thread = get_elements_in_different_thread.IsChecked.Value;
            Config.Save();
        }
        private void traverse_selector_both_ways_IsEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (traverse_selector_both_ways.IsChecked == null) return;
            PluginConfig.traverse_selector_both_ways = traverse_selector_both_ways.IsChecked.Value;
            Config.Save();
        }
        private void search_descendants_Checked(object sender, RoutedEventArgs e)
        {
            if (search_descendants.IsChecked == null) return;
            PluginConfig.search_descendants = search_descendants.IsChecked.Value;
            Config.Save();
        }
        private void enable_cache_Checked(object sender, RoutedEventArgs e)
        {
            if (enable_cache.IsChecked == null) return;
            PluginConfig.enable_cache = enable_cache.IsChecked.Value;
            Config.Save();
        }
        private void force_beta_selector_Checked(object sender, RoutedEventArgs e)
        {
            if (force_beta_selector.IsChecked == null) return;
            PluginConfig.force_beta_selector = force_beta_selector.IsChecked.Value;
            Config.Save();
        }
    }
}
