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
        public SelectorWindow(string pluginname)
        {
            InitializeComponent();
            vm = new SelectorModel(this);
            DataContext = vm;
            vm.PluginName = pluginname;
            vm.Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            vm.Plugin.OnUserAction += Plugin_OnUserAction;
        }
        public SelectorWindow(string pluginname, Selector selector)
        {
            InitializeComponent();
            vm = new SelectorModel(this, selector);
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

        }
        private void BtnHighlight_Click(object sender, RoutedEventArgs e)
        {
            vm.Highlight = !vm.Highlight;
            vm.doHighlight();
        }
        public ICommand SelectCommand { get { return new RelayCommand<treeelement>(onSelect); } }
        private void onSelect(treeelement item)
        {
            var selector = vm.Plugin.GetSelector(item);
            vm.Selector = selector;
            vm.FocusElement(selector);
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
                    //vm.FocusElement(vm.Selector);
                });

                
            });
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            vm.Plugin.OnUserAction -= Plugin_OnUserAction;
        }
    }
}
