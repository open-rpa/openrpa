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

namespace OpenRPA.NM.Views
{
    /// <summary>
    /// Interaction logic for URLDetectorView.xaml
    /// </summary>
    public partial class URLDetectorView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Entity.isDirty = true;
        }
        public URLDetectorView(URLDetectorPlugin plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
            DataContext = this;
        }
        private void value_Changed(object sender, RoutedEventArgs e)
        {
            // if (wait_for_tab_after_set_value.IsChecked == null) return;
            Entity.Save();
        }
        private void plugin_ignore_caseChanged(object sender, RoutedEventArgs e)
        {
            if(plugin_ignore_case.IsChecked != null)
            {
                IgnoreCase = plugin_ignore_case.IsChecked.Value;
            }            
        }

        private IDetectorPlugin plugin;
        public IDetector Entity
        {
            get
            {
                return plugin.Entity as IDetector;
            }
        }
        public string Selector
        {
            get
            {
                if (Entity == null) return null;
                if (!Entity.Properties.ContainsKey("Selector")) return null;
                var _val = Entity.Properties["Selector"];
                if (_val == null) return null;
                return _val.ToString().Replace(Environment.NewLine, "");
            }
            set
            {
                Entity.Properties["Selector"] = value;
                NotifyPropertyChanged("Selector");
            }
        }
        public string EntityName
        {
            get
            {
                if (Entity == null) return string.Empty;
                return Entity.name;
            }
            set
            {
                Entity.name = value;
                NotifyPropertyChanged("EntityName");
            }
        }
        public string URL
        {
            get
            {
                if (Entity == null) return null;
                if (!Entity.Properties.ContainsKey("URL")) return null;
                var _val = Entity.Properties["URL"];
                if (_val == null) return null;
                return _val.ToString();
            }
            set
            {
                Entity.Properties["URL"] = value;
                NotifyPropertyChanged("URL");
            }
        }
        public bool IgnoreCase
        {
            get
            {
                if (Entity == null) return false;
                if (!Entity.Properties.ContainsKey("IgnoreCase")) return false;
                var _val = Entity.Properties["URL"];
                if (_val == null) return IgnoreCase;
                return _val.ToString().ToLower() == "true";
            }
            set
            {
                Entity.Properties["IgnoreCase"] = value.ToString();
                NotifyPropertyChanged("IgnoreCase");
            }
        }
        private void Open_Selector_Click(object sender, RoutedEventArgs e)
        {
            //string SelectorString = Selector;
            //Interfaces.Selector.SelectorWindow selectors;
            //if (!string.IsNullOrEmpty(SelectorString))
            //{
            //    var selector = new WindowsSelector(SelectorString);
            //    selectors = new Interfaces.Selector.SelectorWindow("Windows", selector, null, 10);
            //}
            //else
            //{
            //    var selector = new WindowsSelector("[{Selector: 'Windows'}]");
            //    selectors = new Interfaces.Selector.SelectorWindow("Windows", selector, null, 10);
            //}
            //// selectors.Owner = GenericTools.MainWindow;  -- Locks up and never returns ?
            //if (selectors.ShowDialog() == true)
            //{
            //    Selector = selectors.vm.json;
            //    NotifyPropertyChanged("EntityName");
            //    NotifyPropertyChanged("Selector");
            //}
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Interfaces.GenericTools.Minimize();
            StartRecordPlugins();
        }
        private void StartRecordPlugins()
        {
            var p = Interfaces.Plugins.recordPlugins.Where(x => x.Name == "NM").First();
            p.OnUserAction += OnUserAction;
            p.Start();
        }
        private void StopRecordPlugins()
        {
            var p = Interfaces.Plugins.recordPlugins.Where(x => x.Name == "NM").First();
            p.OnUserAction -= OnUserAction;
            p.Stop();
        }
        public void OnUserAction(Interfaces.IRecordPlugin sender, Interfaces.IRecordEvent e)
        {
            StopRecordPlugins();
            AutomationHelper.syncContext.Post(o =>
            {
                Interfaces.GenericTools.Restore();
                foreach (var p in Interfaces.Plugins.recordPlugins)
                {
                    if (p.Name != sender.Name)
                    {
                        if (p.ParseUserAction(ref e)) continue;
                    }
                }
                try
                {
                    var url = "";
                    if(e is OpenRPA.NM.RecordEvent evt)
                    {
                        if(evt.Selector.Count > 0)
                        {
                            var s1 = evt.Selector[0] as NMSelectorItem;
                            if(s1 != null)
                            {
                                url = s1.url;
                            }
                            
                        }
                    }
                    if (string.IsNullOrEmpty(url)) return;
                    Selector = e.Selector.ToString();
                    NotifyPropertyChanged("Selector");
                    NotifyPropertyChanged("URL");
                    URL = url;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }, null);
        }

    }
}
