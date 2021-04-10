using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using OpenRPA.Net;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public static class Extensions
    {
        /// <summary>
        /// Select specified item in a TreeView
        /// </summary>
        public static void SelectItem(this System.Windows.Controls.TreeView treeView, object item)
        {
            var tvItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as System.Windows.Controls.TreeViewItem;
            if (tvItem == null)
            {
                treeView.UpdateLayout();
                tvItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as System.Windows.Controls.TreeViewItem;
            }
            if (tvItem == null)
            {
                tvItem = treeView.ItemContainerGenerator.ContainerFromIndex(0) as System.Windows.Controls.TreeViewItem;
            }            
            if (tvItem != null)
            {
                tvItem.IsSelected = true;
            }
        }
        public static object PropertyValue(this object obj, string propertyName)
        {
            try
            {
                if (obj != null)
                {
                    PropertyInfo prop = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                    if (prop != null)
                    {
                        object val = prop.GetValue(obj, null);
                        return val;
                        //string sVal = Convert.ToString(val);
                        //if (sVal == propertyValue)
                        //{
                        //    Log.Debug(obj.GetType().FullName + "Has the Value" + propertyValue);
                        //    return true;
                        //}
                    }
                }

                Log.Debug("No property with this value");
                return null;
            }
            catch
            {
                Log.Debug("An error occurred.");
                return false;
            }
        }
        public static string replaceEnvironmentVariable(this string filename)
        {
            var USERPROFILE = Environment.GetEnvironmentVariable("USERPROFILE");
            var windir = Environment.GetEnvironmentVariable("windir");
            var SystemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            var PUBLIC = Environment.GetEnvironmentVariable("PUBLIC");

            if (!string.IsNullOrEmpty(USERPROFILE)) filename = filename.Replace(USERPROFILE, "%USERPROFILE%");
            if (!string.IsNullOrEmpty(windir)) filename = filename.Replace(windir, "%windir%");
            if (!string.IsNullOrEmpty(SystemRoot)) filename = filename.Replace(SystemRoot, "%SystemRoot%");
            if (!string.IsNullOrEmpty(PUBLIC)) filename = filename.Replace(PUBLIC, "%PUBLIC%");

            var ProgramData = Environment.GetEnvironmentVariable("ProgramData");
            if (!string.IsNullOrEmpty(ProgramData)) filename = filename.Replace(ProgramData, "%ProgramData%");
            var ProgramFilesx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (!string.IsNullOrEmpty(ProgramFilesx86)) filename = filename.Replace(ProgramFilesx86, "%ProgramFiles(x86)%");
            var ProgramFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(ProgramFiles)) filename = filename.Replace(ProgramFiles, "%ProgramFiles%");
            var LOCALAPPDATA = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(LOCALAPPDATA)) filename = filename.Replace(LOCALAPPDATA, "%LOCALAPPDATA%");
            var APPDATA = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(APPDATA)) filename = filename.Replace(APPDATA, "%APPDATA%");


            //var = Environment.GetEnvironmentVariable("");
            //if (!string.IsNullOrEmpty()) filename = filename.Replace(, "%%");

            return filename;
        }

        public static string NormalizePath(string path)
        {
            return System.IO.Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }
        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }

        public static System.Windows.Media.Imaging.BitmapFrame GetImageSourceFromResource(string resourceName)
        {
            string[] names = typeof(Extensions).Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    return System.Windows.Media.Imaging.BitmapFrame.Create(typeof(Extensions).Assembly.GetManifestResourceStream(name));
                }
            }
            return null;
        }
        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }
            if (obj is System.Activities.Expressions.Literal<T>)
            {
                result = (T)((System.Activities.Expressions.Literal<T>)obj).Value;
                return true;
            }

            result = default(T);
            return false;
        }
        public static T TryCast<T>(this object obj)
        {
            T result = default(T);
            if (TryCast<T>(obj, out result))
                return result;
            return result;
        }
        public static T GetValue<T>(this System.Activities.Presentation.Model.ModelItem model, string name)
        {
            T result = default(T);
            if (model.Properties[name] != null)
            {
                if (model.Properties[name].Value == null) return result;
                if (model.Properties[name].Value.Properties["Expression"] != null)
                {
                    result = model.Properties[name].Value.Properties["Expression"].ComputedValue.TryCast<T>();
                    if(result==null)
                    {
                        try
                        {
                            var outresult = model.Properties[name].Value.Properties["Expression"].ComputedValue.TryCast<VisualBasicReference<T>>();
                            result = outresult.ExpressionText.TryCast<T>();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if(result==null)
                    {
                        try
                        {
                            var inresult = model.Properties["Value"].Value.Properties["Expression"].ComputedValue.TryCast<System.Activities.Expressions.Literal<T>>();
                            result = inresult.Value.TryCast<T>();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    return result;
                }
                result = model.Properties[name].ComputedValue.TryCast<T>();
                return result;
            }
            return result;
        }
        public static bool IsSerializable2(this Type t)
        {
            return Attribute.IsDefined(t, typeof(System.Runtime.Serialization.DataContractAttribute)) || t.IsSerializable;

        }
        /// <summary>
        /// Replaces the collection without destroy it
        /// Note that we don't Clear() and repopulate collection to avoid and UI winking
        /// </summary>
        /// <param name="collection">Collection.</param>
        /// <param name="newCollection">New collection.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static void UpdateCollection<T>(this ObservableCollection<T> collection, IEnumerable<T> newCollection)
        {
            IEnumerator<T> newCollectionEnumerator = newCollection.GetEnumerator();
            IEnumerator<T> collectionEnumerator = collection.GetEnumerator();

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
                collection.Remove(itemToDelete);
            }

            var i = 0;
            while (newCollectionEnumerator.MoveNext())
            {
                T item = newCollectionEnumerator.Current;

                // Handle new item.
                if (!collection.Contains(item))
                {
                    collection.Insert(i, item);
                }

                // Handle existing item, move at the good index.
                if (collection.Contains(item))
                {
                    int oldIndex = collection.IndexOf(item);
                    if (oldIndex != i)
                    {
                        collection.Move(oldIndex, i);
                    }
                }

                i++;
            }
        }
    }
}
