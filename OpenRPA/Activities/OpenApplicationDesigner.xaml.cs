using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Activities
{
    public partial class OpenApplicationDesigner
    {
        public OpenApplicationDesigner()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void Open_Selector(object sender, RoutedEventArgs e)
        {
            string SelectorString = ModelItem.GetValue<string>("Selector");
            //int maxresult = ModelItem.GetValue<int>("MaxResults");
            int maxresult = 1;

            if (string.IsNullOrEmpty(SelectorString)) SelectorString = "[{Selector: 'Windows'}]";
            var selector = new Interfaces.Selector.Selector(SelectorString);
            var pluginname = selector.First().Selector;
            var selectors = new Interfaces.Selector.SelectorWindow(pluginname, selector, maxresult);

            if (selectors.ShowDialog() == true)
            {
                ModelItem.Properties["Selector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(selectors.vm.json) });
            }
        }
        private async void Highlight_Click(object sender, RoutedEventArgs e)
        {
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            var selector = new Interfaces.Selector.Selector(SelectorString);

            var pluginname = selector.First().Selector;
            var Plugin = Interfaces.Plugins.recordPlugins.Where(x => x.Name == pluginname).First();

            var elements = Plugin.GetElementsWithSelector(selector, null, maxresults);
            foreach (var ele in elements) await ele.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
        }
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Interfaces.GenericTools.Minimize(Interfaces.GenericTools.MainWindow);
            StartRecordPlugins();
        }
        private void StartRecordPlugins()
        {
            var p = Interfaces.Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
            p.OnUserAction += OnUserAction;
            p.Start();
        }
        private void StopRecordPlugins()
        {
            var p = Interfaces.Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
            p.OnUserAction -= OnUserAction;
            p.Stop();
        }
        public void OnUserAction(Interfaces.IRecordPlugin sender, Interfaces.IRecordEvent e)
        {
            StopRecordPlugins();
            AutomationHelper.syncContext.Post(o =>
            {
                Interfaces.GenericTools.Restore(Interfaces.GenericTools.MainWindow);
                foreach (var p in Interfaces.Plugins.recordPlugins)
                {
                    if (p.Name != sender.Name)
                    {
                        if (p.ParseUserAction(ref e)) continue;
                    }
                }
                e.Selector.RemoveRange(3, e.Selector.Count - 3);
                ModelItem.Properties["Selector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(e.Selector.ToString() ) });
            }, null);
        }
    }
}