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
        bool init = false;
        public RecordPluginView()
        {
            InitializeComponent();
            DataContext = this;
            csharp_intellisense.IsChecked = PluginConfig.csharp_intellisense;
            vb_intellisense.IsChecked = PluginConfig.vb_intellisense;
            use_embedded_python.IsChecked = PluginConfig.use_embedded_python;
            init = true;
        }
        private void on_Checked(object sender, RoutedEventArgs e)
        {
            if (!init) return;
            if (csharp_intellisense.IsChecked == null) return;
            if (vb_intellisense.IsChecked == null) return;
            if (csharp_intellisense.IsChecked == null) return;
            if (use_embedded_python.IsChecked == null) return;
            PluginConfig.csharp_intellisense = csharp_intellisense.IsChecked.Value;
            PluginConfig.vb_intellisense = vb_intellisense.IsChecked.Value;
            PluginConfig.use_embedded_python = use_embedded_python.IsChecked.Value;
            Config.Save();
        }
    }
}
