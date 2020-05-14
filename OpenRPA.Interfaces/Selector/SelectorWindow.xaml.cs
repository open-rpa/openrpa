using OpenRPA.Interfaces.Selector;
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
using System.Windows.Shapes;

namespace OpenRPA.Interfaces.Selector
{
    /// <summary>
    /// Interaction logic for Selector.xaml
    /// </summary>
    public partial class SelectorWindow : Window, INotifyPropertyChanged
    {
        public SelectorModel vm;
        Interfaces.Overlay.OverlayWindow _overlayWindow = null;
        public SelectorWindow(string pluginname, int maxresult)
        {
            InitializeComponent();
            vm = new SelectorModel(this)
            {
                maxresult = maxresult
            };
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
        public SelectorWindow(string pluginname, Selector selector, Selector anchor, int maxresult)
        {
            InitializeComponent();
            vm = new SelectorModel(this, selector, anchor)
            {
                maxresult = maxresult
            };
            DataContext = vm;
            vm.PluginName = pluginname;
            vm.Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            vm.Plugin.OnUserAction += Plugin_OnUserAction;
            // var treeelements = vm.Plugin.GetRootElements();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi)
            {
                if (tvi.DataContext is treeelement treeele)
                {
                    //treeele.IsExpanded = !treeele.IsExpanded;
                    //tvi.BringIntoView();
                }
            }
        }
        private void TreeViewSelectedHandler(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item)
            {
                item.BringIntoView();
                e.Handled = true;
                if (item.DataContext is treeelement treeele)
                {
                    if (vm.Highlight) { treeele.Element.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1)); }
                    treeele.LoadDetails();
                    vm.NotifyPropertyChanged("SelectedItemDetails");
                }

            }
        }
        public void OnMouseMove(IRecordPlugin sender, IRecordEvent e)
        {
            if (!Config.local.record_overlay) return;
            if (vm.Plugin != null)
            {
                if (!vm.Plugin.ParseMouseMoveAction(ref e)) return;
            }
            else
            {
                foreach (var p in Plugins.recordPlugins)
                {
                    if (p.Name != sender.Name)
                    {
                        if (p.ParseMouseMoveAction(ref e)) continue;
                    }
                }
            }

            // e.Element.Highlight(false, System.Drawing.Color.PaleGreen, TimeSpan.FromSeconds(1));
            if (e.Element != null && _overlayWindow != null)
            {

                GenericTools.RunUI(_overlayWindow, () =>
                {
                    try
                    {
                        _overlayWindow.Visible = true;
                        _overlayWindow.Bounds = e.Element.Rectangle;
                    }
                    catch (Exception)
                    {
                    }
                });
            }
            else if (_overlayWindow != null)
            {
                GenericTools.RunUI(_overlayWindow, () =>
                {
                    try
                    {
                        _overlayWindow.Visible = false;
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            vm.Plugin.Start();
            GenericTools.Minimize();
            GenericTools.Minimize(this);
            // OpenRPA.Input.InputDriver.Instance.CallNext = false;

            OpenRPA.Input.InputDriver.Instance.onCancel += OnCancel;
            StartOverlay();
        }
        private void StartOverlay()
        {
            var p = Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
            if (Config.local.record_overlay) p.OnMouseMove += OnMouseMove;
            p.Start();
            if (_overlayWindow == null && Config.local.record_overlay)
            {
                _overlayWindow = new Interfaces.Overlay.OverlayWindow(true)
                {
                    BackColor = System.Drawing.Color.PaleGreen,
                    Visible = true,
                    TopMost = true
                };
            }
        }
        private void CancelOverlay()
        {
            var p = Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
            if (Config.local.record_overlay) p.OnMouseMove -= OnMouseMove;
            p.Stop();
            if (_overlayWindow != null)
            {
                GenericTools.RunUI(_overlayWindow, () =>
                {
                    try
                    {
                        _overlayWindow.Visible = true;
                        _overlayWindow.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
            _overlayWindow = null;
        }
        private void OnCancel()
        {
            CancelOverlay();
            vm.Plugin.Stop();
            OpenRPA.Input.InputDriver.Instance.onCancel -= OnCancel;
            // OpenRPA.Input.InputDriver.Instance.CallNext = true;
            GenericTools.Restore(this);
        }
        private void Plugin_OnUserAction(IRecordPlugin sender, IRecordEvent e)
        {
            CancelOverlay();
            vm.Plugin.Stop();
            OpenRPA.Input.InputDriver.Instance.onCancel -= OnCancel;
            // OpenRPA.Input.InputDriver.Instance.CallNext = true;
            e.ClickHandled = true;
            GenericTools.Restore(this);
            vm.Selector = e.Selector;
            vm.json = vm.Selector.ToString();
            vm.FocusElement(e.Selector);
            vm.NotifyPropertyChanged("json");
            // e.Element
        }
        private treeelement SearchTree(ExtendedObservableCollection<treeelement> item, SelectorItem s)
        {
            foreach (var treenode in item)
            {
                if (vm.Plugin.Match(s, treenode.Element))
                {
                    return treenode;
                }
                var currentNode = new ExtendedObservableCollection<treeelement>();
                foreach (var subc in treenode.Children) { currentNode.Add(subc); if (subc.Children.Count() == 0) subc.AddSubElements(); }
                var res = SearchTree(currentNode, s);
                if (res != null) return res;
            }
            return null;
        }
        private void ExpandToRoot(treeelement treenode)
        {
            treenode.IsExpanded = true;
            treenode.IsSelected = true;
            if (treenode.Parent != null) ExpandToRoot(treenode.Parent);
        }
        private void BtnSyncTree_Click(object sender, RoutedEventArgs e)
        {
            var currentNode = vm.Directories;
            vm.Highlight = false;
            var found = false;
            if (vm.Selector.Count>1)
            {
                var s = vm.Selector[1];
                var p = s.Properties.Where(x => x.Name == "xpath").FirstOrDefault();
                if (p!=null)
                {
                    var treenode = SearchTree(vm.Directories, s);
                    if(treenode!=null)
                    {
                        ExpandToRoot(treenode);
                        found = true;
                        treenode.IsExpanded = true;
                        treenode.IsSelected = true;
                        currentNode = new ExtendedObservableCollection<treeelement>();
                        foreach (var subc in treenode.Children) currentNode.Add(subc);
                        //currentNode = c.Children;
                    }
                }
            }
            if (!found)
                for (var i = 1; i < vm.Selector.Count; i++)
                {
                    found = false;
                    var s = vm.Selector[i]; 
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
            if(!found)
            {
                var last = vm.Selector.Last();
                var _treenode = SearchTree(vm.Directories, last);
                if (_treenode != null)
                {
                    ExpandToRoot(_treenode);
                    found = true;
                    _treenode.IsExpanded = true;
                    _treenode.IsSelected = true;
                    currentNode = new ExtendedObservableCollection<treeelement>();
                    foreach (var subc in _treenode.Children) currentNode.Add(subc);

                }
            }

            vm.Highlight = true;
        }
        private void BtnHighlight_Click(object sender, RoutedEventArgs e)
        {
            if (vm.doHighlight())
            {
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            vm.Directories.Add(new treeelement(null) { Name = "Loading" });
            Task.Run(() =>
            {
                var treeelements = vm.Plugin.GetRootElements(vm.Anchor);
                GenericTools.RunUI(this, () =>
                {
                    Log.Debug("init selector model, with " + treeelements.Count() + " root elements");
                    vm.init(treeelements);
                    // vm.FocusElement(vm.Selector);
                });
            });
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            vm.Plugin.OnUserAction -= Plugin_OnUserAction;
            GenericTools.Restore();
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
