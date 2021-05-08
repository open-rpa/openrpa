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
                foreach (var p in list.ToList())
                {
                    var Entity = (p.Entity as Detector);
                    if (global.isConnected)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(Entity._id) || Entity.isLocalOnly)
                            {
                                var result = await global.webSocketClient.InsertOne("openrpa", 0, false, Entity);
                                Entity._id = result._id;
                                Entity._acl = result._acl;
                                Entity.isLocalOnly = false;
                                Entity.isDirty = false;
                            }
                            else
                            {
                                var result = await global.webSocketClient.UpdateOne("openrpa", 0, false, p.Entity);
                                if (result != null) Entity._acl = result._acl;
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
            finally
            {
                semaphoreSlim.Release();
            }
            Log.FunctionOutdent("DetectorsView", "DetectorPlugins_ItemPropertyChanged");
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Log.FunctionIndent("DetectorsView", "Button_Click");
            try
            {
                var btn = sender as System.Windows.Controls.Button;
                var kv = (System.Collections.Generic.KeyValuePair<string, System.Type>)btn.DataContext;
                var d = new Detector(); d.Plugin = kv.Value.FullName;
                IDetectorPlugin dp = null;
                NotifyPropertyChanged("detectorPlugins");
                d.name = kv.Value.Name;
                if (global.isConnected)
                {
                    var result = await global.webSocketClient.InsertOne("openrpa", 0, false, d);
                    d._id = result._id;
                    d._acl = result._acl;
                }
                else
                {
                    d._id = Guid.NewGuid().ToString();
                    d.isDirty = true;
                    d.isLocalOnly = true;
                }
                IDetectorPlugin exists = Plugins.detectorPlugins.Where(x => x.Entity._id == d._id).FirstOrDefault();
                if (exists == null)
                {
                    dp = Plugins.AddDetector(RobotInstance.instance, d);
                    dp.OnDetector += main.OnDetector;
                }
                var dexists = RobotInstance.instance.Detectors.FindById(d._id);
                if (dexists == null) RobotInstance.instance.Detectors.Insert(d);
                if (dexists != null) RobotInstance.instance.Detectors.Update(d);
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
                    var index = lidtDetectors.SelectedIndex;
                    var items = new List<object>();
                    foreach (var item in lidtDetectors.SelectedItems) items.Add(item);
                    foreach (var item in items)
                    {
                        var d = item as IDetectorPlugin;
                        if (d != null)
                        {
                            var entity = d.Entity as Detector;
                            d.OnDetector -= main.OnDetector;
                            d.Stop();
                            Plugins.detectorPlugins.Remove(d);
                            if (entity != null)
                            {
                                await entity.Delete();
                            }
                        }
                    }
                    if (index > -1)
                    {
                        if (index >= lidtDetectors.Items.Count)
                        {
                            index = lidtDetectors.Items.Count - 1;
                        }
                        if (index > -1)
                        {
                            ((ListBoxItem)lidtDetectors.ItemContainerGenerator.ContainerFromIndex((lidtDetectors.Items.Count > 1 ? (index == 0 ? 1 : index - 1) : 0)))?.Focus();
                        }
                    }
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
