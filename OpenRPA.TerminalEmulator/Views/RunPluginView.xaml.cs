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
using OpenRPA.TerminalEmulator;

namespace OpenRPA.TerminalEmulator.Views
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
            _ = PluginConfig.auto_close;
            DataContext = this;
        }
        public bool auto_close
        {
            get
            {
                return PluginConfig.auto_close;
            }
            set
            {
                PluginConfig.auto_close = value;
                Config.Save();
                NotifyPropertyChanged("auto_close");
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
