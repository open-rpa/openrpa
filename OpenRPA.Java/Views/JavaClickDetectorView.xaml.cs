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

namespace OpenRPA.Java.Views
{
    /// <summary>
    /// Interaction logic for JavaClickDetectorView.xaml
    /// </summary>
    public partial class JavaClickDetectorView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public JavaClickDetectorView(JavaClickDetectorPlugin plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
            HighlightImage.Source = Extensions.GetImageSourceFromResource("search.png");
            DataContext = this;
        }
        private JavaClickDetectorPlugin plugin;
        public Detector Entity
        {
            get
            {
                return plugin.Entity;
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
        public string Selector
        {
            get
            {
                if (plugin.Selector == null) return "";
                return plugin.Selector.Replace(Environment.NewLine, "");
            }
            set
            {
                plugin.Selector = value;
                NotifyPropertyChanged("Selector");
            }
        }
        private void Open_Selector_Click(object sender, RoutedEventArgs e)
        {
            string SelectorString = plugin.Selector;
            Interfaces.Selector.SelectorWindow selectors;
            if (!string.IsNullOrEmpty(SelectorString))
            {
                var selector = new JavaSelector(SelectorString);
                selectors = new Interfaces.Selector.SelectorWindow("Java", selector, null, 10);
            }
            else
            {
                var selector = new JavaSelector("[{Selector: 'Java'}]");
                selectors = new Interfaces.Selector.SelectorWindow("Java", selector, null, 10);
            }
            if (selectors.ShowDialog() == true)
            {
                plugin.Selector = selectors.vm.json;
                NotifyPropertyChanged("Selector");
            }
        }
        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            HighlightImage.Source = Extensions.GetImageSourceFromResource(".x.png");
            string SelectorString = plugin.Selector;
            var selector = new JavaSelector(SelectorString);
            var elements = JavaSelector.GetElementsWithuiSelector(selector, null, 10);
            if (elements.Count() > 0)
            {
                HighlightImage.Source = Extensions.GetImageSourceFromResource("check.png");
            }
            foreach (var ele in elements) ele.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
        }
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Interfaces.GenericTools.Minimize();
            StartRecordPlugins();
        }
        private void StartRecordPlugins()
        {
            var p = Interfaces.Plugins.recordPlugins.Where(x => x.Name == "Java").First();
            p.OnUserAction += OnUserAction;
            p.Start();
        }
        private void StopRecordPlugins()
        {
            var p = Interfaces.Plugins.recordPlugins.Where(x => x.Name == "Java").First();
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
                var jelement = e.Element as JavaElement;
                EntityName = jelement.title;
                plugin.Selector = e.Selector.ToString();
                NotifyPropertyChanged("EntityName");
                NotifyPropertyChanged("Selector");
            }, null);
        }
    }
}
