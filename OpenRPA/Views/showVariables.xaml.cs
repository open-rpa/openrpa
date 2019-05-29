using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for showVariables.xaml
    /// </summary>
    public partial class showVariables : UserControl
    {
        public const int variable_max_enumorations = 100;
        public showVariables()
        {
            InitializeComponent();
        }

        public ObservableCollection<variable> variables { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            if (variables != null) return;
            variables = new ObservableCollection<variable>();
            addVariable("test1", "value1", typeof(string));
            addVariable("test2", Guid.NewGuid(), typeof(Guid));
            addVariable("test3", 32, typeof(int));
            addVariable("test4", true, typeof(bool));
            grid.SelectionMode = DataGridSelectionMode.Single;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var b = sender;
            variable item = (variable)grid.SelectedItem;
            var index = variables.IndexOf(item);
            if (item.IsExpanded)
            {
                //variables.Insert(index, new variable() { name = item.name + index, value = "value" + index, Level = item.Level + 1 });
                expandVariable(index);
                return;
            }
            index++;
            if (index >= variables.Count()) return;
            variable subitem = variables[index];
            while (subitem != null && subitem.Level > item.Level)
            {
                variables.Remove(subitem);
                if (variables.Count > index) subitem = variables[index];
                if (variables.Count == index) subitem = null;
            }
        }

        public void addVariable(string name, object obj, Type type)
        {
            int index = variables.Count;
            addVariable(name, obj, type, index, 0);
        }
        public void addVariable(string name, object obj, Type type, int index, int level)
        {
            try
            {
                var value = "(null)";
                var details = "(null)";
                try
                {
                    if (obj != null) value = obj.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        details = value;
                        value = value.Replace(System.Environment.NewLine, "");
                        if (value.Length > 25) value = value.Substring(0, 22) + "...";
                    }
                    var exception = obj as Exception;
                    if (exception != null)
                    {
                        value = exception.Message;
                    }
                }
                catch (Exception)
                {
                }
                var v = new variable() { name = name, obj = obj, value = value, type = type, details = details, Level = level };
                if (type != null) v.typename = type.FullName;
                if (type == typeof(string)) v.HasChildren = false;
                if (type == typeof(bool)) v.HasChildren = false;
                if (type == typeof(int)) v.HasChildren = false;
                if (type == typeof(Int16)) v.HasChildren = false;
                if (type == typeof(Int32)) v.HasChildren = false;
                if (type == typeof(Guid)) v.HasChildren = false;
                if (obj == null) v.HasChildren = false;
                //if (type == typeof()) v.HasChildren = false;

                try
                {
                    variables.Insert(index, v);
                }
                catch (Exception)
                {
                    throw;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private int expandFields(List<string> newVariables, variable item, Type type, object obj, int index)
        {
            if (obj == null && type == null) return index;
            if (type == null) type = obj.GetType();
            var fields = type.GetFields();
            //var fields = type.GetFields();
            foreach (var f in fields)
            {
                if (newVariables.Contains(f.Name)) continue;
                newVariables.Add(f.Name);
                index++;
                try
                {
                    var subo = f.GetValue(obj);
                    addVariable(f.Name, subo, f.FieldType, index, item.Level + 1);
                }
                catch (Exception ex)
                {
                    addVariable(f.Name, ex, ex.GetType(), index, item.Level + 1);
                }
            }
            return index;
        }
        private int expandProperties(List<string> newVariables, variable item, Type type, object obj, int index)
        {
            if (obj == null && type == null) return index;
            if (type == null) type = obj.GetType();
            var properties = type.GetProperties();
            foreach (var p in properties)
            {
                if (newVariables.Contains(p.Name)) continue;
                newVariables.Add(p.Name);
                try
                {
                    if (p.CanRead) // Does the property has a "Get" accessor
                    {
                        if (p.GetIndexParameters().Length == 0) // Ensure that the property does not requires any parameter
                        {
                            var subo = p.GetValue(obj);
                            addVariable(p.Name, subo, p.PropertyType, (index + 1), item.Level + 1);
                            index++;

                            //if (typeof(IEnumerable).IsAssignableFrom(obj.GetType()))
                            //{
                            //    var subo = obj;
                            //    try
                            //    {
                            //        subo = p.GetValue(obj);
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        var s = ex.ToString();
                            //    }
                            //    addVariable(p.Name, obj, p.PropertyType, (index + 1), item.Level + 1);
                            //    index++;
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    addVariable(p.Name, ex, ex.GetType(), index, item.Level + 1);
                }
            }
            return index;
        }
        private int expandInterfaces(List<string> newVariables, variable item, Type type, object obj, int index)
        {
            if (obj == null && type == null) return index;
            if (type == null) type = obj.GetType();
            foreach (var _type in type.GetInterfaces())
            {
                index = expandFields(newVariables, item, _type, item.obj, index);
                index = expandProperties(newVariables, item, _type, item.obj, index);
                index = expandInterfaces(newVariables, item, _type, item.obj, index);
            }
            return index;
        }
        public void expandVariable(int index)
        {
            var newVariables = new List<string>();
            var item = variables[index];

            index = expandFields(newVariables, item, item.type, item.obj, index);
            index = expandProperties(newVariables, item, item.type, item.obj, index);
            index = expandInterfaces(newVariables, item, item.type, item.obj, index);

            //try
            //{
            //    var ipm = new InterfacesPropertiesMap(item.type);
            //    foreach (var i in ipm.Interfaces)
            //    {
            //        foreach (var p in ipm[i])
            //        {
            //            if (newVariables.Contains(p.Name)) continue;
            //            newVariables.Add(p.Name);


            //            if (p.CanRead) // Does the property has a "Get" accessor
            //            {
            //                if (p.GetIndexParameters().Length == 0) // Ensure that the property does not requires any parameter
            //                {
            //                    var subo = p.GetValue(item.obj);
            //                    addVariable(p.Name, subo, p.PropertyType, (index + 1), item.Level + 1);
            //                    index++;
            //                }
            //            }


            //        }
            //    }

            //}
            //catch (Exception)
            //{
            //}


            //try
            //{
            //    Type dispatchType = DispatchUtility.GetType(item.obj, false);
            //    if (dispatchType != null)
            //    {
            //        index = expandFields(newVariables, item, dispatchType, item.obj, index);
            //        index = expandProperties(newVariables, item, dispatchType, item.obj, index);
            //    }
            //}
            //catch (Exception)
            //{
            //}

            //foreach (var type in item.type.GetInterfaces())
            //{
            //    index = expandFields(newVariables, item, type, item.obj, index);
            //    index = expandProperties(newVariables, item, type, item.obj, index);
            //}



            if (typeof(IEnumerable).IsAssignableFrom(item.obj.GetType()))
            {
                var arr = (IEnumerable)item.obj;
                int _index = 0;
                foreach (var v in arr)
                {
                    var elementtype = item.obj.GetType().GetElementType();
                    if (elementtype == null)
                    {
                        elementtype = item.obj.GetType().GetGenericArguments().FirstOrDefault();
                    }
                    index++;
                    addVariable(_index.ToString(), v, elementtype, index, item.Level + 1);
                    _index++;
                    if (_index > variable_max_enumorations) break;
                }
            }
        }


        void ShowHideDetails(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility =
                    row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }


    }
}
