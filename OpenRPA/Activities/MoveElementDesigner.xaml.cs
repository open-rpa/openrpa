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
    public partial class MoveElementDesigner
    {
        public MoveElementDesigner()
        {
            InitializeComponent();
        }
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (Config.local.minimize) Interfaces.GenericTools.Minimize();
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
                // ModelItem.Properties["Selector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(e.Selector.ToString()) });
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
                        var newbound = new System.Drawing.Rectangle(bound.X, bound.Y, bound.Width, bound.Height);
                        var p = new System.Drawing.Point(bound.X, bound.Y);
                        var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                        int screen = 0;
                        for (var i = 0; i < allScreens.Count; i++)
                        {
                            var work = allScreens[i].WorkingArea;
                            if (work.Contains(bound) || allScreens[i].WorkingArea.Contains(p))
                            {
                                screen = i;
                                newbound.X = newbound.X - work.X;
                                newbound.Y = newbound.Y - work.Y;
                                break;
                            }
                        }
                        ModelItem.Properties["Screen"].SetValue(new InArgument<int>() { Expression = new Literal<int>(screen) });
                        ModelItem.Properties["X"].SetValue(new InArgument<int>() { Expression = new Literal<int>(newbound.X) });
                        ModelItem.Properties["Y"].SetValue(new InArgument<int>() { Expression = new Literal<int>(newbound.Y) });
                        ModelItem.Properties["Width"].SetValue(new InArgument<int>() { Expression = new Literal<int>(newbound.Width) });
                        ModelItem.Properties["Height"].SetValue(new InArgument<int>() { Expression = new Literal<int>(newbound.Height) });
                    }
                }
            }, null);
        }

    }
}