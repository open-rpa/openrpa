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
            ItemPropertyChanged?.Invoke(sender, e);
        }
        protected override void ClearItems()
        {
            foreach (var item in Items) item.PropertyChanged -= _ItemPropertyChanged;
            base.ClearItems();
        }
        public new void Add(T item)
        {
            GenericTools.RunUI(() => base.Add(item));
        }
        public new void Clear()
        {
            GenericTools.RunUI(() => base.Clear());
        }
        public new void Remove(T item)
        {
            if(item == null) return;
            item.PropertyChanged -= _ItemPropertyChanged;
            GenericTools.RunUI(() => base.Remove(item));
        }
        public new void RemoveAt(int index)
        {
            Items[index].PropertyChanged -= _ItemPropertyChanged;
            GenericTools.RunUI(() => base.RemoveAt(index));
        }
        public new void CopyTo(T[] array, int index)
        {
            GenericTools.RunUI(() => base.CopyTo(array, index));
        }
        public new void Insert(int index, T item)
        {
            item.PropertyChanged += _ItemPropertyChanged;
            GenericTools.RunUI(() => base.Insert(index, item));
        }
        public new void Move(int oldIndex, int newIndex)
        {
            GenericTools.RunUI(() => base.Move(oldIndex, newIndex));
        }
        public void AddRange(IEnumerable<T> range)
        {
            IList<T> rangeList = null;
            try
            {
                rangeList = range as IList<T>;
                if (rangeList == null)
                {
                    var _enum = range as IEnumerable<T>;
                    if (_enum != null) rangeList = _enum.ToList();
                }
            }
            catch (Exception)
            {
            }
            if (rangeList == null)
            {
                rangeList = range.ToList();
            }
            if (rangeList.Count == 0) { return; }
            if (rangeList.Count == 1)
            {
                Add(rangeList[0]);
                return;
            }
            foreach (var item in rangeList)
            {
                GenericTools.RunUI(() =>
                {
                    item.PropertyChanged += _ItemPropertyChanged;
                    Items.Add(item);
                });
            }
            GenericTools.RunUI(() =>
            {
                try
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                    OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
            });
        }
        public void RemoveRange(int index, int count)
        {
            if (count <= 0 || index >= Items.Count) { return; }
            if (count == 1)
            {
                Items[index].PropertyChanged -= _ItemPropertyChanged;
                RemoveAt(index);
                return;
            }
            GenericTools.RunUI(() =>
            {
                for (var i = 0; i < count; i++)
                {
                    Items[index].PropertyChanged -= _ItemPropertyChanged;
                    Items.RemoveAt(index);
                }
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            });
        }
        public void RemoveRange(IEnumerable<T> range)
        {
            foreach (var item in range)
            {
                Remove(item);
            }
            GenericTools.RunUI(() =>
            {
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            });
        }
        public void RemoveAll(Predicate<T> match)
        {
            var removedItem = false;
            for (var i = Items.Count - 1; i >= 0; i--)
            {
                if (match(Items[i]))
                {
                    Items[i].PropertyChanged -= _ItemPropertyChanged;
                    Items.RemoveAt(i);
                    removedItem = true;
                }
            }
            if (removedItem)
            {
                GenericTools.RunUI(() =>
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                    OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
                });
            }
        }
        protected override void InsertItem(int index, T item)
        {
            item.PropertyChanged += _ItemPropertyChanged;
            GenericTools.RunUI(() =>
            {
                base.InsertItem(index, item);
            });
        }
        protected override void RemoveItem(int index)
        {
            this[index].PropertyChanged -= _ItemPropertyChanged;
            GenericTools.RunUI(() =>
            {
                base.RemoveItem(index);
            });
        }
        protected override void SetItem(int index, T item)
        {
            this[index].PropertyChanged -= _ItemPropertyChanged;
            item.PropertyChanged += _ItemPropertyChanged;
            GenericTools.RunUI(() =>
            {
                base.SetItem(index, item);
            });
        }
        public void UpdateItem(T Item, T NewItem)
        {
            NewItem.CopyPropertiesTo(Item);
            GenericTools.RunUI(() =>
            {
                try
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, Item));
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.Message);
                }
            });
            return;
        }
        public void Reset(IEnumerable<T> range)
        {
            ClearItems();
            AddRange(range);
        }
        public void UpdateCollection(IEnumerable<T> newCollection)
        {
            IEnumerator<T> newCollectionEnumerator = newCollection.GetEnumerator();
            IEnumerator<T> collectionEnumerator = Items.GetEnumerator();

            Collection<T> itemsToDelete = new Collection<T>();
            while (collectionEnumerator.MoveNext())
            {
                T item = collectionEnumerator.Current;

                // Store item to delete (we can't do it while parse collection.
                if (!newCollection.Contains(item))
                {
                    itemsToDelete.Add(item);
                }
            }

            // Handle item to delete.
            foreach (T itemToDelete in itemsToDelete)
            {
                Items.Remove(itemToDelete);
            }

            var i = 0;
            while (newCollectionEnumerator.MoveNext())
            {
                T item = newCollectionEnumerator.Current;

                // Handle new item.
                if (!Items.Contains(item))
                {
                    Items.Insert(i, item);
                }

                // Handle existing item, move at the good index.
                if (Items.Contains(item))
                {
                    int oldIndex = Items.IndexOf(item);
                    if (oldIndex != i)
                    {
                        // Items.Move(oldIndex, i);
                        Move(oldIndex, i);
                    }
                }

                i++;
            }
        }
    }
    public class ExtendedIBaseObservableCollection<T> : ExtendedObservableCollection<T> where T : INotifyPropertyChanged, IBase
    {
        public T FindById(string id) => Items.Where(x => x._id == id).FirstOrDefault();
        public ExtendedIBaseObservableCollection() : base()
        {
        }
        public ExtendedIBaseObservableCollection(IEnumerable<T> pItems) : base()
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
    public class FilteredObservableCollection<T> : ExtendedObservableCollection<T> where T : INotifyPropertyChanged
    {
        private Predicate<T> _filter;
        private ExtendedObservableCollection<T> basecollection;
        public FilteredObservableCollection(ExtendedObservableCollection<T> collection, Predicate<T> Filter) : base()
        {
            _filter = Filter;
            basecollection = collection;
            var _list = new List<T>();
            foreach (var item in collection) if (_filter(item) == true) Items.Add(item);
            collection.CollectionChanged += new NotifyCollectionChangedEventHandler(OnBaseCollectionChanged);
            basecollection.ItemPropertyChanged += Basecollection_ItemPropertyChanged;
        }
        public void Refresh()
        {
            Items.Clear();
            foreach (var item in basecollection) if (_filter(item) == true) base.Add(item);
        }
        private void Basecollection_ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            T item = (T)sender;
            if (e.PropertyName == "_id" || e.PropertyName == "projectid")
            {
                if (Items.Contains(item))
                {
                    if (_filter(item) != true) base.Remove(item);
                }
                else
                {
                    if (_filter(item) == true) base.Add(item);
                }
            }
        }
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
                            try
                            {
                                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, replacelist));
                            }
                            catch (Exception)
                            {
                            }
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
    public class CompositionObservableCollection : ExtendedIBaseObservableCollection<IBase>
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
            AddRange(itemsToAdd);
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
                    this.RemoveRange(oldItems);
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
            this.RemoveRange(itemsToDelete);
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
            foreach (var itemToAdd in newItems)
            {
                var indexOfNewItemToAdd = CalculateIndexForInsertNewItem(sender);
                InsertItem(indexOfNewItemToAdd, itemToAdd);
            }
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
        public static void CopyPropertiesTo(this object fromObject, object toObject)
        {
            PropertyInfo[] toObjectProperties = toObject.GetType().GetProperties();
            foreach (PropertyInfo propTo in toObjectProperties)
            {
                PropertyInfo propFrom = fromObject.GetType().GetProperty(propTo.Name);
                if (propFrom != null && propFrom.CanWrite)
                    propTo.SetValue(toObject, propFrom.GetValue(fromObject, null), null);
            }
        }

    }
}
