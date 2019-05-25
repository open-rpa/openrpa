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
        private bool isSaving = false;
        private async void DetectorPlugins_ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (isSaving) return;
                var list = (ExtendedObservableCollection<OpenRPA.Interfaces.IDetectorPlugin>)sender;
                foreach(var p in list.ToList())
                {
                    var Entity = (p.Entity as Interfaces.entity.Detector);
                    isSaving = true;
                    Entity.SaveFile();
                    if (global.isConnected)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(Entity._id))
                            {
                                var result = await global.webSocketClient.InsertOne("openrpa", Entity);
                                Entity._id = result._id;
                                Entity._acl = result._acl;
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
                    isSaving = false;
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
        }
        private async void LidtDetectors_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                var item = lidtDetectors.SelectedValue as IDetectorPlugin;
                item.Stop();
                item.OnDetector -= main.OnDetector;
                var d = item.Entity as OpenRPA.Interfaces.entity.Detector;
                if (d != null) d.Delete();
                var kd = item.Entity as OpenRPA.Interfaces.entity.KeyboardDetector;
                if (kd != null) kd.Delete();
                if (global.isConnected)
                {
                    var _id = (item.Entity as Interfaces.entity.Detector)._id;
                    if (!string.IsNullOrEmpty(_id))
                    {
                        await global.webSocketClient.DeleteOne("openrpa", _id);
                    }
                }
                detectorPlugins.Remove(item);

            }
        }
    }
}
