using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
            Log.FunctionIndent("DetectorsView", "DetectorsView");
            InitializeComponent();
            this.main = main;
            DataContext = this;
            detectorPlugins.ItemPropertyChanged += DetectorPlugins_ItemPropertyChanged;
            Log.FunctionOutdent("DetectorsView", "DetectorsView");
        }
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private async void DetectorPlugins_ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Log.FunctionIndent("DetectorsView", "DetectorPlugins_ItemPropertyChanged");
            try
            {
                await semaphoreSlim.WaitAsync();
                var list = (ExtendedObservableCollection<OpenRPA.Interfaces.IDetectorPlugin>)sender;
                foreach(var p in list.ToList())
                {
                    var Entity = (p.Entity as Interfaces.entity.Detector);
                    if (global.isConnected)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(Entity._id))
                            {
                                var result = await global.webSocketClient.InsertOne("openrpa", 0, false, Entity);
                                Entity._id = result._id;
                                Entity._acl = result._acl;
                            }
                            else
                            {
                                var result = await global.webSocketClient.UpdateOne("openrpa", 0, false, p.Entity);
                                Entity._acl = result._acl;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    } else
                    {
                        if (string.IsNullOrEmpty(Entity._id)) Entity._id = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", ""); 
                    }
                    Entity.SaveFile();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                semaphoreSlim.Release();
            }
            Log.FunctionOutdent("DetectorsView", "DetectorPlugins_ItemPropertyChanged");
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Log.FunctionIndent("DetectorsView", "Button_Click");
            try
            {
                var btn = sender as System.Windows.Controls.Button;
                var kv = (System.Collections.Generic.KeyValuePair<string, System.Type>)btn.DataContext;
                var d = new Interfaces.entity.Detector(); d.Plugin = kv.Value.FullName;
                IDetectorPlugin dp = null;
                d.Path = Interfaces.Extensions.ProjectsDirectory;
                dp = Plugins.AddDetector(RobotInstance.instance, d);
                dp.OnDetector += main.OnDetector;
                NotifyPropertyChanged("detectorPlugins");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("DetectorsView", "Button_Click");
        }
        private async void LidtDetectors_KeyUp(object sender, KeyEventArgs e)
        {
            Log.FunctionIndent("DetectorsView", "LidtDetectors_KeyUp");
            try
            {
                if (e.Key == Key.Delete)
                {
                    var item = lidtDetectors.SelectedValue as IDetectorPlugin;
                    item.Stop();
                    item.OnDetector -= main.OnDetector;
                    var d = item.Entity as OpenRPA.Interfaces.entity.Detector;
                    if (d != null) d.Delete();
                    var kd = item.Entity;
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
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("DetectorsView", "LidtDetectors_KeyUp");
        }
    }
}
