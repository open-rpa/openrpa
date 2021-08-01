using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
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
            Loaded += (sender, e) =>
            {
                var Variables = ModelItem.Properties[nameof(OpenApplication.Variables)].Collection;
                if (Variables != null && Variables.Count == 0)
                {
                    Variables.Add(new Variable<int>("Index", 0));
                    Variables.Add(new Variable<int>("Total", 0));
                }
            };
        }
        private void Open_Selector(object sender, RoutedEventArgs e)
        {
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresult = 1;

            if (string.IsNullOrEmpty(SelectorString)) SelectorString = "[{Selector: 'Windows'}]";
            var selector = new Interfaces.Selector.Selector(SelectorString);
            var pluginname = selector.First().Selector;
            var selectors = new Interfaces.Selector.SelectorWindow(pluginname, selector, maxresult);
            // selectors.Owner = GenericTools.MainWindow;  -- Locks up and never returns ?
            if (selectors.ShowDialog() == true)
            {
                ModelItem.Properties["Selector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(selectors.vm.json) });
                var Plugin = Interfaces.Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
                var _base = Plugin.GetElementsWithSelector(selector, null, 10);
                if (_base == null || _base.Length == 0) return;
                var ele = _base[0];
                if (ele != null && !(ele is UIElement))
                {
                    var automation = AutomationUtil.getAutomation();
                    var p = new System.Drawing.Point(ele.Rectangle.X + 10, ele.Rectangle.Y + 10);
                    if (p.X > 0 && p.Y > 0)
                    {
                        var _temp = automation.FromPoint(p);
                        if (_temp != null)
                        {
                            ele = new UIElement(_temp);
                        }
                    }
                }
                if (ele is UIElement ui)
                {
                    var window = ui.GetWindow();
                    if (window == null) return;
                    if (!string.IsNullOrEmpty(window.Name))
                    {
                        ModelItem.Properties["DisplayName"].SetValue(window.Name);
                    }
                    if (window.Properties.BoundingRectangle.IsSupported)
                    {
                        var bound = window.BoundingRectangle;
                        ModelItem.Properties["X"].SetValue(new InArgument<int>() { Expression = new Literal<int>(bound.X) });
                        ModelItem.Properties["Y"].SetValue(new InArgument<int>() { Expression = new Literal<int>(bound.Y) });
                        ModelItem.Properties["Width"].SetValue(new InArgument<int>() { Expression = new Literal<int>(bound.Width) });
                        ModelItem.Properties["Height"].SetValue(new InArgument<int>() { Expression = new Literal<int>(bound.Height) });
                    }
                }

            }
        }
        private async void Highlight_Click(object sender, RoutedEventArgs e)
        {
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = 1;
            var selector = new Interfaces.Selector.Selector(SelectorString);

            var pluginname = selector.First().Selector;
            var Plugin = Interfaces.Plugins.recordPlugins.Where(x => x.Name == pluginname).First();

            var elements = Plugin.GetElementsWithSelector(selector, null, maxresults);
            foreach (var ele in elements) await ele.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
        }
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Interfaces.GenericTools.Minimize();
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
                Interfaces.GenericTools.Restore();
                foreach (var p in Interfaces.Plugins.recordPlugins)
                {
                    if (p.Name != sender.Name)
                    {
                        if (p.ParseUserAction(ref e)) continue;
                    }
                }
                e.Selector.RemoveRange(2, e.Selector.Count - 2);
                ModelItem.Properties["Selector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(e.Selector.ToString()) });
                var ele = e.Element;
                if (ele != null && !(ele is UIElement))
                {
                    var automation = AutomationUtil.getAutomation();
                    var p = new System.Drawing.Point(ele.Rectangle.X + 10, ele.Rectangle.Y + 10);
                    if (p.X > 0 && p.Y > 0)
                    {
                        var _temp = automation.FromPoint(p);
                        if (_temp != null)
                        {
                            ele = new UIElement(_temp);
                        }
                    }
                }

                if (ele is UIElement ui)
                {
                    var window = ui.GetWindow();
                    if (window == null) return;
                    if (!string.IsNullOrEmpty(window.Name))
                    {
                        ModelItem.Properties["DisplayName"].SetValue(window.Name);
                    }
                    if (window.Properties.BoundingRectangle.IsSupported)
                    {
                        var bound = window.BoundingRectangle;
                        ModelItem.Properties["X"].SetValue(new InArgument<int>() { Expression = new Literal<int>(bound.X) });
                        ModelItem.Properties["Y"].SetValue(new InArgument<int>() { Expression = new Literal<int>(bound.Y) });
                        ModelItem.Properties["Width"].SetValue(new InArgument<int>() { Expression = new Literal<int>(bound.Width) });
                        ModelItem.Properties["Height"].SetValue(new InArgument<int>() { Expression = new Literal<int>(bound.Height) });
                    }
                }
            }, null);
        }
    }
}