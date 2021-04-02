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

namespace OpenRPA.IE.Views
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
            enable_xpath_support.IsChecked = PluginConfig.enable_xpath_support;
            enable_caching_browser.IsChecked = PluginConfig.enable_caching_browser;
            browser_timeout.Text = PluginConfig.open_browser_url_timeout.ToString();
            init = false;
        }
        private void value_Changed(object sender, RoutedEventArgs e)
        {
            if (init) return;
            PluginConfig.enable_xpath_support = enable_xpath_support.IsChecked.Value;
            PluginConfig.enable_caching_browser = enable_caching_browser.IsChecked.Value;
            try
            {
                TimeSpan t;
                if(TimeSpan.TryParse(browser_timeout.Text, out t))
                {
                    browser_timeout.Background = Brushes.White;
                    PluginConfig.open_browser_url_timeout = t;
                }
                else
                {
                    browser_timeout.Background = Brushes.Red;
                }
            }
            catch (Exception)
            {

                throw;
            }
            Config.Save();
        }
    }
}
