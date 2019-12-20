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

namespace OpenRPA.Script.Views
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
            csharp_intellisense.IsChecked = PluginConfig.csharp_intellisense;
            vb_intellisense.IsChecked = PluginConfig.vb_intellisense;
        }
        private void csharp_intellisense_Checked(object sender, RoutedEventArgs e)
        {
            if (csharp_intellisense.IsChecked == null) return;
            PluginConfig.csharp_intellisense = csharp_intellisense.IsChecked.Value;
            Config.Save();
        }

        private void vb_intellisense_Checked(object sender, RoutedEventArgs e)
        {
            if (vb_intellisense.IsChecked == null) return;
            PluginConfig.vb_intellisense = vb_intellisense.IsChecked.Value;
            Config.Save();
        }
    }
}
