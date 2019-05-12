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
        public string pluginname;
        IPlugin plugin = null;
        public SelectorWindow(string pluginname)
        {
            InitializeComponent();
            this.pluginname = pluginname;
            plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            plugin.OnUserAction += Plugin_OnUserAction;
            vm = new SelectorModel(this);
            DataContext = vm;
        }
        public SelectorWindow(string pluginname, Selector selector)
        {
            InitializeComponent();
            this.pluginname = pluginname;
            plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            plugin.OnUserAction += Plugin_OnUserAction;
            var treeelements = plugin.GetRootElements();
            vm = new SelectorModel(this, selector);
            DataContext = vm;
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
            plugin.Start();
            GenericTools.minimize(GenericTools.mainWindow);
            GenericTools.minimize(this);
        }
        private void Plugin_OnUserAction(IPlugin sender, IRecordEvent e)
        {
            plugin.Stop();
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

        }
        public ICommand SelectCommand { get { return new RelayCommand<treeelement>(onSelect); } }
        private void onSelect(treeelement item)
        {
            var selector = plugin.GetSelector(item);
            vm.Selector = selector;
            vm.FocusElement(selector);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var treeelements = plugin.GetRootElements();
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
            plugin.OnUserAction -= Plugin_OnUserAction;
        }
    }
}
