using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Collections;
using System.Reflection;

namespace OpenRPA.Interfaces
{
    public class IBaseObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged, IBase
    {
        public T FindById(string id) => Items.Where(x => x._id == id).FirstOrDefault();
        public IBaseObservableCollection() : base()
        {
        }
        public IBaseObservableCollection(IEnumerable<T> pItems) : base()
        {
            foreach (var item in pItems)
            {
                Add(item);
            }
        }
        public void UpdateCollectionById(IEnumerable<T> newCollection)
        {
            var current = newCollection.ToArray();
            for (var i = 0; i < current.Count(); i++)
            {
                var exists = Items.Where(x => x._id == current[i]._id);
                if (exists.Count() == 0) Items.Add(current[i]);
            }
            var list = Items.ToArray();
            for (var i = 0; i < list.Count(); i++)
            {
                var exists = current.Where(x => x._id == list[i]._id);
                if (exists.Count() == 0) Items.Remove(list[i]);
            }
        }
    }
    public class FilteredObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        private Predicate<T> _filter;
        private ObservableCollection<T> basecollection;
        public FilteredObservableCollection(ObservableCollection<T> collection, Predicate<T> Filter) : base()
        {
            _filter = Filter;
            basecollection = collection;
            var _list = new List<T>();
            foreach (var item in collection) if (_filter(item) == true) Items.Add(item);
            collection.CollectionChanged += new NotifyCollectionChangedEventHandler(OnBaseCollectionChanged);
            // basecollection.onFilterUpdated += Basecollection_onFilterUpdated;
        }
        public void Refresh()
        {
            Items.Clear();
            foreach (var item in basecollection) if (_filter(item) == true) base.Add(item);
        }

        //private void Basecollection_onFilterUpdated(object sender, EventArgs e)
        //{
        //    List<T> addlist = new List<T>();
        //    List<T> removelist = new List<T>();
        //    try
        //    {
        //        foreach (var item in Items.ToList())
        //        {
        //            if (_filter(item) == false)
        //            {
        //                Items.Remove(item);
        //                removelist.Add(item);
        //            }
        //        }
        //        foreach (var item in basecollection.ToList())
        //        {
        //            if (Items.Contains(item)) continue;
        //            if (_filter(item) == true)
        //            {
        //                Items.Add(item);
        //                addlist.Add(item);
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message);
        //    }
        //    if (removelist.Count > 0) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removelist));
        //    if (addlist.Count > 0) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addlist));
        //}
        public Predicate<T> Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }
        void OnBaseCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // ObservableCollection<T> collection = sender as ObservableCollection<T>;
            var collection = Items;
            if (collection != null)
            {
                // Check the NewItems
                List<T> newlist = new List<T>();
                if (e.NewItems != null)
                    foreach (T item in e.NewItems)
                        if (_filter(item) == true)
                            newlist.Add(item);

                // Check the OldItems
                List<T> oldlist = new List<T>();
                if (e.OldItems != null)
                    foreach (T item in e.OldItems)
                        if (_filter(item) == true)
                            oldlist.Add(item);

                // Create the Add/Remove/Replace lists
                List<T> addlist = new List<T>();
                List<T> removelist = new List<T>();
                List<T> replacelist = new List<T>();

                // Fill the Add/Remove/Replace lists
                foreach (T item in newlist)
                    if (oldlist.Contains(item))
                        replacelist.Add(item);
                    else
                        addlist.Add(item);
                foreach (T item in oldlist)
                    if (newlist.Contains(item))
                        continue;
                    else
                        removelist.Add(item);

                foreach (T item in addlist) Items.Add(item);
                foreach (T item in removelist) Items.Remove(item);
                // Send the corrected event
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                        if (addlist.Count > 0)
                        {
                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addlist));
                            // Log.Output("OnBaseCollectionChanged add " + addlist.Count);
                            //OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                            //OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                        }
                        if (replacelist.Count > 0)
                        {
                            // OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, replacelist, replacelist, 0));
                            // Log.Output("OnBaseCollectionChanged replace " + replacelist.Count);
                        }
                        if (removelist.Count > 0)
                        {
                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removelist));
                            // Log.Output("OnBaseCollectionChanged remove " + removelist.Count);
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        // sLog.Output("OnBaseCollectionChanged reset");
                        break;
                }
            }
        }
        new public void Move(int oldIndex, int newIndex) { basecollection.Move(oldIndex, newIndex); }
        new public void ClearItems() { basecollection.Clear(); }
        new public void RemoveItem(int index) { basecollection.RemoveAt(index); }
        new public void InsertItem(int index, T item) { basecollection.Insert(index, item); }
        new public void SetItem(int index, T item) { basecollection[index] = item; }
        new public void MoveItem(int oldIndex, int newIndex) { basecollection.Move(oldIndex, newIndex); }
        new public void Add(T item) { basecollection.Add(item); }
        new public void Remove(T item) { basecollection.Remove(item); }
    }
    public class CompositionObservableCollection : IBaseObservableCollection<IBase>
    {
        private readonly List<INotifyCollectionChanged> _observableCollections;
        public CompositionObservableCollection(params INotifyCollectionChanged[] observableCollections)
        {
            _observableCollections = observableCollections.ToList();
            InitItems();
            AttacheEvents();
        }
        private void InitItems()
        {
            var itemsToAdd = _observableCollections.OfType<IEnumerable<IBase>>().SelectMany(item => item);
            foreach(var item in itemsToAdd) Add(item);
            // AddRange(itemsToAdd);
        }
        private void AttacheEvents()
        {
            _observableCollections.ForEach(item => item.CollectionChanged += TestCollectionChanged);
        }
        private void TestCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var newItems = e.NewItems.ToListOfType<IBase>();
            var oldItems = e.OldItems.ToListOfType<IBase>();
            var notifyCollectionChanged = (INotifyCollectionChanged)sender;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddItems(newItems, notifyCollectionChanged);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    // this.RemoveRange(oldItems);
                    foreach (var item in oldItems) Remove(item);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    ReplaceItems(oldItems, newItems);
                    break;
                case NotifyCollectionChangedAction.Move:
                    MoveItems(newItems, e);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Reset();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void Reset()
        {
            var itemsToDelete = GetItemsToDelete().ToList();
            // this.RemoveRange(itemsToDelete);
            foreach(var item in itemsToDelete) Remove(item);
        }
        private void MoveItems(IEnumerable<IBase> newItems, NotifyCollectionChangedEventArgs e)
        {
            var deltaMoveIndex = e.NewStartingIndex - e.OldStartingIndex;
            foreach (var itemToMove in newItems)
            {
                var indexOfItemInTargetCollection = IndexOf(itemToMove);
                var newIndex = indexOfItemInTargetCollection + deltaMoveIndex;
                Move(indexOfItemInTargetCollection, newIndex);
            }
        }
        private void ReplaceItems(IList<IBase> oldItems, IList<IBase> newItems)
        {
            for (var i = 0; i < oldItems.Count; i++)
            {
                var currentIndexInTargetCollection = IndexOf(oldItems[i]);
                this[currentIndexInTargetCollection] = newItems[i];
            }
        }
        private void AddItems(IEnumerable<IBase> newItems, INotifyCollectionChanged sender)
        {
            //AddRange(newItems);
            foreach (var item in newItems) Add(item);
        }
        private IEnumerable<IBase> GetItemsToDelete()
        {
            var allSourceItems = _observableCollections.OfType<IEnumerable<IBase>>().SelectMany(item => item);
            var itemsToDelete = this.Except(allSourceItems);
            return itemsToDelete;
        }
        private int CalculateIndexForInsertNewItem(INotifyCollectionChanged sender)
        {
            var firstItem = CalculateFirstIndexOfItemStack(sender);
            var lastItemIndex = firstItem + ((IEnumerable<IBase>)sender).Count();
            return lastItemIndex - 1;
        }
        private int CalculateFirstIndexOfItemStack(INotifyCollectionChanged sender)
        {
            var indexInStack = _observableCollections.IndexOf(sender);
            if (indexInStack == 0)
            {
                return 0;
            }

            var itemsCount = CalculateItemsCountReverseFromIndex(indexInStack);
            return itemsCount;
        }
        private int CalculateItemsCountReverseFromIndex(int indexInStack)
        {
            var itemsCount = 0;
            for (var i = indexInStack - 1; i >= 0; i--)
            {
                itemsCount += _observableCollections.OfType<IEnumerable<IBase>>().ElementAt(i).Count();
            }
            return itemsCount;
        }
    }
    public static class EnumerableExtensions
    {
        public static List<T> ToListOfType<T>(this IEnumerable source)
        {
            return source?.OfType<T>().ToList() ?? new List<T>();
        }
        public static void CopyPropertiesTo(this object fromObject, object toObject, bool SimpleOnly)
        {
            PropertyInfo[] toObjectProperties = toObject.GetType().GetProperties();
            foreach (PropertyInfo propTo in toObjectProperties)
            {
                PropertyInfo propFrom = fromObject.GetType().GetProperty(propTo.Name);


                var JsonIgnore = propTo.GetCustomAttributes(typeof(Newtonsoft.Json.JsonIgnoreAttribute), false);
                if (JsonIgnore != null && JsonIgnore.Length > 0) continue;
                bool copy = false;
                if(SimpleOnly) {
                    if (propTo.PropertyType == typeof(int)) copy = true;
                    if (propTo.PropertyType == typeof(Int64)) copy = true;
                    if (propTo.PropertyType == typeof(double)) copy = true;
                    if (propTo.PropertyType == typeof(long)) copy = true;
                    if (propTo.PropertyType == typeof(float)) copy = true;
                    if (propTo.PropertyType == typeof(bool)) copy = true;
                    if (propTo.PropertyType == typeof(string)) copy = true;
                }
                else
                {
                    copy = true;
                }
                if (propFrom != null && propFrom.CanWrite && copy)
                    propTo.SetValue(toObject, propFrom.GetValue(fromObject, null), null);
            }
        }

    }
}
