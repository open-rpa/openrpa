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

namespace OpenRPA.Image.Views
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
            recording_mouse_move_time.Text = PluginConfig.recording_mouse_move_time.ToString();
            init = false;
        }
        private void on_Changed(object sender, RoutedEventArgs e)
        {
            if (init) return;
            if (string.IsNullOrEmpty(recording_mouse_move_time.Text))
            {
                PluginConfig.recording_mouse_move_time = 60;
            }
            else
            {
                try
                {
                    PluginConfig.recording_mouse_move_time = int.Parse(recording_mouse_move_time.Text);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            Config.Save();
        }
    }
}
