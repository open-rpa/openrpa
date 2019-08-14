using System;
using System.Collections.Generic;
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

namespace OpenRPA.AviRecorder.Activities
{
    /// <summary>
    /// Interaction logic for Playback.xaml
    /// </summary>
    public partial class Playback : Window
    {
        public Playback(string Filename)
        {
            InitializeComponent();
            mediaElement.Source = new Uri(Filename);
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            Topmost = true;
            Focus();
        }
    }
}
