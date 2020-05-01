using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OpenRPA.Interfaces.Selector
{
    public class SelectorModel : ObservableObject
    {
        public string PluginName { get; set; }
        public IRecordPlugin Plugin { get; set; }
        private string _json = null;
        public string json
        {
            get
            {
                if (Selector == null) return null;
                //if (_json == null)
                //{
                //    _json = Selector.ToString();
                //}
                if(_json==null) _json = Selector.ToString();
                return _json;
            }
            set
            {
                _json = value;
                if (_json != null) _json = _json.Trim();
                //if (Selector == null && !string.IsNullOrEmpty(_json))
                //{
                //    Selector = new Selector(_json);
                //    OnPropertyChanged("Selector");
                //}
                try
                {
                    Selector = new Selector(_json);
                }
                catch (Exception)
                {
                }
                OnPropertyChanged("json");
            }
        }
        public int maxresult { get; set; }
        public bool Highlight { get; set; }
        public BitmapFrame HighlightImage { get; set; }
        public void init(treeelement[] treeelements)
        {
            HighlightImage = Extensions.GetImageSourceFromResource("search.png");
            NotifyPropertyChanged("HighlightImage");
            Directories.Clear();
            foreach (var te in treeelements) Directories.Add(te);
            foreach (var te in Directories) te.PropertyChanged += (sender, e) =>
                {
                    OnPropertyChanged("json");
                };

            Selector.ItemPropertyChanged += (sender, e) =>
            {
                OnPropertyChanged("json");
            };
            //Selector.ElementPropertyChanged += (sender, e) =>
            //{
            //    OnPropertyChanged("json");
            //};
        }
        public SelectorModel(SelectorWindow window)
        {
            Highlight = true;
            Directories = new ExtendedObservableCollection<treeelement>();
            this.window = window;
            Directories.ItemPropertyChanged += (sender, e) =>
            {
                NotifyPropertyChanged("json");
            };

        }
        public SelectorModel(SelectorWindow window, Selector Selector, Selector Anchor = null)
        {
            Highlight = true;
            this.Anchor = Anchor;
            this.Selector = Selector;
            Directories = new ExtendedObservableCollection<treeelement>();
            this.window = window;
            Directories.ItemPropertyChanged += (sender, e) =>
            {
                NotifyPropertyChanged("json");
            };
        }
        private SelectorWindow window;
        public Selector Selector { get { return GetProperty<Selector>(); } set { SetProperty(value); } }
        public Selector Anchor { get { return GetProperty<Selector>(); } set { SetProperty(value); } }
        private ExtendedObservableCollection<treeelement> directories;
        public ExtendedObservableCollection<treeelement> Directories
        {
            get
            {
                return directories;
            }
            set
            {
                directories = value;
                NotifyPropertyChanged("Directories");
            }
        }
        public void FocusElement(Selector Selector)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                ObservableCollection<treeelement> current = Directories;
                foreach (var node in Selector.Where(x => x.Selector == null))
                {
                    bool found = false;
                    foreach (var treenode in current)
                    {
                        if (node.Element.Equals(treenode.Element) && found == false)
                        {
                            found = true;
                            treenode.IsExpanded = true;
                            current = treenode.Children;
                            Highlight = false;
                            treenode.IsSelected = true;
                            Highlight = true;
                            continue;
                        }
                        else if (found == false)
                        {
                            var c = treenode.Children.FirstOrDefault();
                            if (c != null && node.Element.Equals(c.Element) && found == false)
                            {
                                found = true;
                                treenode.IsExpanded = true;
                                treenode.IsSelected = true;


                                current = c.Children;
                                c.IsExpanded = true;
                                Highlight = false;
                                c.IsSelected = true;
                                Highlight = true;
                                continue;
                            }
                        }
                    }
                    if (!found) { return; }
                }
            }, null);
        }
        public bool doHighlight()
        {
            HighlightImage = Extensions.GetImageSourceFromResource(".x.png");
            NotifyPropertyChanged("HighlightImage");
            // IElement[] results = new IElement[] { };
            Task.Run(() =>
            {
                var results = new List<IElement>();
                if(Anchor!=null)
                {
                    var _base = Plugin.GetElementsWithSelector(Anchor, null, 10);
                    foreach (var _e in _base)
                    {
                        var res = Plugin.GetElementsWithSelector(Selector, _e, maxresult);
                        results.AddRange(res);
                    }
                } else
                {
                    var res = Plugin.GetElementsWithSelector(Selector, null, maxresult);
                    results.AddRange(res);

                }
                GenericTools.RunUI(() =>
                {
                    foreach (var element in results)
                    {
                        element.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
                    }
                    if (results.Count() > 0)
                    {
                        HighlightImage = Extensions.GetImageSourceFromResource("check.png");
                        NotifyPropertyChanged("HighlightImage");
                    }
                });
            });
            // return (results.Count() > 0);
            return true;
        }
        public System.Windows.Input.ICommand SelectCommand { get { return new RelayCommand<treeelement>(onSelect); } }
        private void onSelect(treeelement item)
        {
            Task.Run(() =>
            {
                try
                {
                    Selector s = null; treeelement parent;
                    if(Anchor!=null)
                    {
                        parent = item.Parent;
                        while (parent != null && parent.Parent != null) parent = parent.Parent;
                        if (parent == null)
                        {
                            System.Windows.MessageBox.Show("Cannot select self");
                            return;
                        }
                        s = Plugin.GetSelector(null, parent);  
                    }
                    var selector = Plugin.GetSelector(s, item);
                    Selector = selector;

                    OnPropertyChanged("Selector");
                    OnPropertyChanged("json");

                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
            // FocusElement(selector);
        }
    }
}
