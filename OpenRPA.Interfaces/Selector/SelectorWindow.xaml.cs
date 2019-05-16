using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace OpenRPA.Interfaces.Selector
{
    /// <summary>
    /// Interaction logic for Selector.xaml
    /// </summary>
    public partial class SelectorWindow : Window
    {
        public SelectorModel vm;
        public SelectorWindow(string pluginname, int maxresult)
        {
            InitializeComponent();
            vm = new SelectorModel(this);
            vm.maxresult = maxresult;
            DataContext = vm;
            vm.PluginName = pluginname;
            vm.Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            vm.Plugin.OnUserAction += Plugin_OnUserAction;
        }
        public SelectorWindow(string pluginname, Selector selector, int maxresult)
        {
            InitializeComponent();
            vm = new SelectorModel(this, selector);
            vm.maxresult = maxresult;
            DataContext = vm;
            vm.PluginName = pluginname;
            vm.Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            vm.Plugin.OnUserAction += Plugin_OnUserAction;
            // var treeelements = vm.Plugin.GetRootElements();
        }
        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;
            if (tvi != null)
            {
                var treeele = tvi.DataContext as treeelement;
                if (treeele != null)
                {
                    //treeele.IsExpanded = !treeele.IsExpanded;
                    //tvi.BringIntoView();
                }
            }
        }
        private void TreeViewSelectedHandler(object sender, RoutedEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item != null)
            {
                item.BringIntoView();
                e.Handled = true;
                var treeele = item.DataContext as treeelement;
                if (treeele != null)
                {
                    if (vm.Highlight) { treeele.Element.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1)); }
                    treeele.LoadDetails();
                    vm.NotifyPropertyChanged("SelectedItemDetails");
                }

            }
        }
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            vm.Plugin.Start();
            GenericTools.minimize(GenericTools.mainWindow);
            GenericTools.minimize(this);
        }
        private void Plugin_OnUserAction(IPlugin sender, IRecordEvent e)
        {
            vm.Plugin.Stop();
            e.ClickHandled = true;
            // GenericTools.restore(GenericTools.mainWindow);
            GenericTools.restore(this);
            vm.Selector = e.Selector;
            vm.FocusElement(e.Selector);
            // e.Element
        }
        private void BtnSyncTree_Click(object sender, RoutedEventArgs e)
        {
            var currentNode = vm.Directories;
            vm.Highlight = false;
            for (var i = 1; i < vm.Selector.Count; i++)
            {
                var s = vm.Selector[i]; var found = false;
                foreach (var treenode in currentNode)
                {
                    if (vm.Plugin.Match(s, treenode.Element))
                    {
                        found = true;
                        treenode.IsExpanded = true;
                        treenode.IsSelected = true;
                        currentNode = new ExtendedObservableCollection<treeelement>();
                        foreach (var subc in treenode.Children) currentNode.Add(subc);
                        //currentNode = c.Children;
                        continue;
                    }
                }
                if(!found)
                {
                    foreach (var treenode in currentNode)
                    {
                        foreach (var subtreenode in treenode.Children)
                        {
                            if (vm.Plugin.Match(s, subtreenode.Element))
                            {
                                found = true;
                                subtreenode.IsExpanded = true;
                                
                                subtreenode.IsSelected = true;
                                currentNode = new ExtendedObservableCollection<treeelement>();
                                foreach (var subc in subtreenode.Children) currentNode.Add(subc);
                                //currentNode = c.Children;
                                continue;
                            }
                        }
                    }
                }
                if (!found) break;
            }
            vm.Highlight = true;
        }
        private void BtnHighlight_Click(object sender, RoutedEventArgs e)
        {
            vm.doHighlight();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var treeelements = vm.Plugin.GetRootElements();
                GenericTools.RunUI(this, () =>
                {
                    System.Diagnostics.Trace.WriteLine("init selector model, with " + treeelements.Count() + " root elements", "Debug");
                    vm.init(treeelements);
                    // vm.FocusElement(vm.Selector);
                });
            });
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            vm.Plugin.OnUserAction -= Plugin_OnUserAction;
            GenericTools.restore(GenericTools.mainWindow);
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
