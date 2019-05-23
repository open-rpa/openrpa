using OpenRPA.Interfaces;
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

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for DetectorsView.xaml
    /// </summary>
    public partial class DetectorsView : UserControl, INotifyPropertyChanged
    {
        private MainWindow main = null;
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ICollection<IDetectorPlugin> detectorPlugins
        {
            get
            {
                return Plugins.detectorPlugins;
            }
        }
        public Dictionary<string, Type> DetectorTypes
        {
            get
            {
                return Plugins.detectorPluginTypes;
            }
        }

        public DetectorsView(MainWindow main)
        {
            InitializeComponent();
            this.main = main;
            DataContext = this;
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var kv = (System.Collections.Generic.KeyValuePair<string, System.Type>)btn.DataContext;
            var d = new Interfaces.entity.Detector(); d.Plugin = kv.Value.FullName;
            IDetectorPlugin dp = null;
            dp = Plugins.AddDetector(d);
            dp.OnDetector += main.OnDetector;
            NotifyPropertyChanged("detectorPlugins");
        }

        private void ContentPresenter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var b = true;

        }
    }
}
