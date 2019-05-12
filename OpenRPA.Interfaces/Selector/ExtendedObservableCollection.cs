using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.Selector
{
    public class ExtendedObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        public ExtendedObservableCollection()
        {
        }
        public ExtendedObservableCollection(IEnumerable<T> collection) : base(collection)
        {
        }
        public ExtendedObservableCollection(List<T> list) : base(list)
        {
        }
        public event PropertyChangedEventHandler ItemPropertyChanged;
        private void _ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ItemPropertyChanged?.Invoke(this, e);
        }

        protected override void ClearItems()
        {
            foreach (var item in Items) item.PropertyChanged -= _ItemPropertyChanged;
            base.ClearItems();
        }
        public void AddRange(IEnumerable<T> range)
        {
            var rangeList = range as IList<T> ?? range.ToList();
            if (rangeList.Count == 0) { return; }
            if (rangeList.Count == 1)
            {
                Add(rangeList[0]);
                return;
            }
            foreach (var item in rangeList)
            {
                Items.Add(item);
            }
            foreach (var item in range) item.PropertyChanged += _ItemPropertyChanged;
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public void RemoveRange(int index, int count)
        {
            if (count <= 0 || index >= Items.Count) { return; }
            if (count == 1)
            {
                RemoveAt(index);
                return;
            }
            for (var i = 0; i < count; i++)
            {
                Items.RemoveAt(index);
            }
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public void RemoveAll(Predicate<T> match)
        {
            var removedItem = false;
            for (var i = Items.Count - 1; i >= 0; i--)
            {
                if (match(Items[i]))
                {
                    Items.RemoveAt(i);
                    removedItem = true;
                }
            }
            if (removedItem)
            {
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            }
        }
        protected override void InsertItem(int index, T item)
        {
            item.PropertyChanged += _ItemPropertyChanged;
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            this[index].PropertyChanged -= _ItemPropertyChanged;
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, T item)
        {
            this[index].PropertyChanged -= _ItemPropertyChanged;
            item.PropertyChanged += _ItemPropertyChanged;
            base.SetItem(index, item);
        }

        public void Reset(IEnumerable<T> range)
        {
            ClearItems();
            AddRange(range);
        }
    }
}
