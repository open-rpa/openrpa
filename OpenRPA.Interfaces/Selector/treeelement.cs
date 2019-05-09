using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.Selector
{
    public class treeelement : NotifyChange, IEnumerable
    {
        public IElement Element { get; set; }
        public treeelement(treeelement parent)
        {
            // ItemDetails = new rpaactivities.ExtendedObservableCollection<DetailGroupViewModel>();
            Children = new ObservableCollection<treeelement>();
            Name = "unknown";
            Parent = parent;
        }
        private ObservableCollection<treeelement> _Children;
        public ObservableCollection<treeelement> Children
        {
            get
            {
                return _Children;
            }
            set
            {
                if (_Children != value)
                {
                    _Children = value;
                    NotifyPropertyChanged("SubDirectories");
                }
            }
        }
        public string Name { get; set; }
        private bool _IsExpanded = false;
        public bool IsExpanded
        {
            get
            {
                return _IsExpanded;
            }
            set
            {
                _IsExpanded = value;
                if (!_IsExpanded)
                {
                    //Children.Clear();
                    NotifyPropertyChanged("Children");
                    NotifyPropertyChanged("IsExpanded");
                    return;
                }
                //Children.Clear();
                //Children.Add(new treeelement(null)); // dummy, so we can expand in UI
                //Children.Clear();
                if (Children.Count() == 0) AddSubElements();
                foreach (var ele in Children)
                {
                    ele.AddSubElements();
                }
                NotifyPropertyChanged("Children");
                NotifyPropertyChanged("IsExpanded");
            }
        }
        private bool _IsSelected = false;
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                _IsSelected = value;
                NotifyPropertyChanged("IsSelected");

            }
        }
        public treeelement Parent { get; set; }
        public IEnumerator GetEnumerator()
        {
            return _Children.GetEnumerator();
        }
        public virtual void AddSubElements() { }
        public virtual void LoadDetails() { }
        public override string ToString()
        {
            return Name;
        }
        // public rpaactivities.ExtendedObservableCollection<DetailGroupViewModel> ItemDetails { get; set; }
    }

}
