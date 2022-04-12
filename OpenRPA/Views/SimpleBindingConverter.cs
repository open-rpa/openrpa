using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OpenRPA.Views
{
    /// <summary>
    /// A converter that organizes several collections into (optional)
    /// child collections that are put into <see cref="FolderItem"/>
    /// containers.
    /// </summary>
    public class SimpleBindingConverter : IMultiValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //get folder name listing...
            string folder = parameter as string ?? "";
            var folders = folder.Split(',').Select(f => f.Trim()).ToList();
            //...and make sure there are no missing entries
            while (values.Length > folders.Count) folders.Add(String.Empty);
            //this is the collection that gets all top level items
            List<object> items = new List<object>();
            for (int i = 0; i < values.Length; i++)
            {
                //make sure were working with collections from here...
                IEnumerable childs = values[i] as IEnumerable ?? new List<object> { values[i] };
                foreach (var child in childs) { items.Add(child); }
            }
            return items;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot perform reverse-conversion");
        }
    }
    public class IndexerConverter : IValueConverter
    {
        public string CollectionName { get; set; }
        public string IndexName { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type type = value.GetType();
            dynamic collection = type.GetProperty(CollectionName).GetValue(value, null);
            dynamic index = type.GetProperty(IndexName).GetValue(value, null);
            return collection[index];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    //public class CollectionChangedHandlingValueConverter : IMultiValueConverter
    //{
    //    DependencyObject myTarget;
    //    DependencyProperty myTargetProperty;

    //    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {

    //        foreach(var value in values)
    //        {
    //            INotifyCollectionChanged collection = value as INotifyCollectionChanged;
    //            if (collection != null)
    //            {
    //                //It notifies of collection changed, try again when it changes
    //                collection.CollectionChanged += DataCollectionChanged;
    //                collection.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) =>
    //                {

    //                };
    //            }
    //        }
    //        //get folder name listing...
    //        string folder = parameter as string ?? "";
    //        var folders = folder.Split(',').Select(f => f.Trim()).ToList();
    //        //...and make sure there are no missing entries
    //        while (values.Length > folders.Count) folders.Add(String.Empty);
    //        //this is the collection that gets all top level items
    //        List<object> items = new List<object>();
    //        for (int i = 0; i < values.Length; i++)
    //        {
    //            //make sure were working with collections from here...
    //            IEnumerable childs = values[i] as IEnumerable ?? new List<object> { values[i] };
    //            foreach (var child in childs) { items.Add(child); }
    //        }
    //        return items;

    //        //Do whatever conversions here
    //    }
    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        throw new NotSupportedException("Cannot perform reverse-conversion");
    //    }
    //    void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    //    {
    //        if ((myTarget != null) && (myTargetProperty != null))
    //        {
    //            BindingOperations.GetBindingExpressionBase(myTarget, myTargetProperty).UpdateTarget();
    //        }
    //    }
    //}


    public class SortConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Collections.IList collection = value as System.Collections.IList;
            ListCollectionView view = new ListCollectionView(collection);
            SortDescription sort = new SortDescription(parameter.ToString(), ListSortDirection.Ascending);
            view.SortDescriptions.Add(sort);

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

}
