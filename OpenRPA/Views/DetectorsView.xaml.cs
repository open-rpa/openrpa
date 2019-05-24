using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
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
        public ExtendedObservableCollection<IDetectorPlugin> detectorPlugins
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
            detectorPlugins.ItemPropertyChanged += DetectorPlugins_ItemPropertyChanged;
        }

        private async void DetectorPlugins_ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                var list = (ExtendedObservableCollection<OpenRPA.Interfaces.IDetectorPlugin>)sender;
                foreach(var p in list.ToList())
                {
                    Console.WriteLine(p.Entity.name);
                    p.Entity.SaveFile();
                    if (global.isConnected)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(p.Entity._id))
                            {
                                var result = await global.webSocketClient.InsertOne("openrpa", p.Entity);
                                p.Entity._id = result._id;
                                p.Entity._acl = result._acl;
                            }
                            else
                            {
                                await global.webSocketClient.UpdateOne("openrpa", p.Entity);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var kv = (System.Collections.Generic.KeyValuePair<string, System.Type>)btn.DataContext;
            var d = new Interfaces.entity.Detector(); d.Plugin = kv.Value.FullName;
            IDetectorPlugin dp = null;
            d.Path = Extensions.projectsDirectory;
            dp = Plugins.AddDetector(d);
            dp.OnDetector += main.OnDetector;
            NotifyPropertyChanged("detectorPlugins");
        }

        private void ContentPresenter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var b = true;

        }

        private async void LidtDetectors_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                var item = lidtDetectors.SelectedValue as IDetectorPlugin;
                item.Stop();
                item.OnDetector -= main.OnDetector;
                if (global.isConnected)
                {
                    if (!string.IsNullOrEmpty(item.Entity._id))
                    {
                        await global.webSocketClient.DeleteOne("openrpa", item.Entity._id);
                    }
                }
                detectorPlugins.Remove(item);

            }
        }
    }
}
