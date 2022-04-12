//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Collections.Specialized;
//using System.ComponentModel;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OpenRPA
//{
//    public sealed class TrulyObservableCollection<T> : ObservableCollection<T>
//        where T : INotifyPropertyChanged
//    {
//        public TrulyObservableCollection()
//        {
//            CollectionChanged += FullObservableCollectionCollectionChanged;
//        }
//        public TrulyObservableCollection(IEnumerable<T> pItems) : this()
//        {
//            foreach (var item in pItems)
//            {
//                Add(item);
//            }
//        }
//        private void FullObservableCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
//        {
//            if (e.NewItems != null)
//            {
//                foreach (object item in e.NewItems)
//                {
//                    ((INotifyPropertyChanged)item).PropertyChanged += ItemPropertyChanged;
//                }
//            }
//            if (e.OldItems != null)
//            {
//                foreach (object item in e.OldItems)
//                {
//                    ((INotifyPropertyChanged)item).PropertyChanged -= ItemPropertyChanged;
//                }
//            }
//        }
//        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
//        {
//            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T)sender));
//            OnCollectionChanged(args);
//        }
//        public void AddRange(IEnumerable<T> range)
//        {
//            var rangeList = range as IList<T> ?? range.ToList();
//            if (rangeList.Count == 0) { return; }
//            if (rangeList.Count == 1)
//            {
//                Add(rangeList[0]);
//                return;
//            }
//            foreach (var item in rangeList)
//            {
//                Items.Add(item);
//            }
//            //foreach (var item in range) item.PropertyChanged += ItemPropertyChanged;
//            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
//            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
//            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
//        }
//        public void RemoveRange(int index, int count)
//        {
//            if (count <= 0 || index >= Items.Count) { return; }
//            if (count == 1)
//            {
//                RemoveAt(index);
//                return;
//            }
//            for (var i = 0; i < count; i++)
//            {
//                Items.RemoveAt(index);
//            }
//            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
//            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
//            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
//        }
//    }
//}
