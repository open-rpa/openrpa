using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.Selector
{
    public class SelectorModel : ObservableObject
    {
        public void init(treeelement[] treeelements)
        {
            Directories.Clear();
            //string anchortype = null;
            //IElement anchorelement = null;
            foreach(var te in treeelements) Directories.Add(te);


            //if (Anchor != null)
            //{
            //    anchortype = Anchor.First().Selector;
            //    if (anchortype == null) anchortype = "uia3";
            //    anchorelement = Interfaces.Selector.Selector.GetElementsWithSelector(Anchor.ToString()).FirstOrDefault();
            //}
        }
        public SelectorModel(treeelement[] treeelements, SelectorWindow window)
        {
            Directories = new ObservableCollection<treeelement>();
            this.window = window;
            init(treeelements);
        }
        public SelectorModel(treeelement[] treeelements, SelectorWindow window, Interfaces.Selector.Selector anchor)
        {
            this.Anchor = anchor;
            Directories = new ObservableCollection<treeelement>();
            this.window = window;
            init(treeelements);
        }
        private SelectorWindow window;
        public Interfaces.Selector.Selector Selector { get; set; }
        private Interfaces.Selector.Selector Anchor;

        private ObservableCollection<treeelement> directories;
        public ObservableCollection<treeelement> Directories
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


        internal void FocusElement(Selector Selector)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                ObservableCollection<treeelement> current = Directories;
                foreach (var node in Selector.Where(x => x.Selector==null))
                {
                    System.Diagnostics.Trace.WriteLine("****************************************");
                    var s = node.ToString();
                    System.Diagnostics.Trace.WriteLine(node.ToString());
                    bool found = false;
                    foreach (var treenode in current)
                    {
                        System.Diagnostics.Trace.WriteLine(treenode.ToString());
                        if (node.Element.Equals(treenode.Element) && found == false)
                        {
                            found = true;
                            treenode.IsExpanded = true;
                            current = treenode.Children;
                            treenode.IsSelected = true;
                            continue;
                        }
                        else if(found == false)
                        {
                            var c = treenode.Children.FirstOrDefault();
                            if (c != null) { System.Diagnostics.Trace.WriteLine(c.ToString()); }
                            if (c != null && node.Element.Equals(c.Element) && found == false)
                            {
                                found = true;
                                treenode.IsExpanded = true;
                                treenode.IsSelected = true;


                                current = c.Children;
                                c.IsExpanded = true;
                                c.IsSelected = true;
                                continue;
                            }
                        }
                    }
                    if(!found) { return; }
                }
            }, null);
        }

    }
}
