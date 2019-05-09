using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Java
{
    class JavaSelectorItem : SelectorItem
    {
        public JavaSelectorItem() { }
        public JavaSelectorItem(SelectorItem item)
        {
            SetBackingFieldValues(item._backingFieldValues);
            Properties = item.Properties;
        }
        public JavaSelectorItem(JavaElement element, bool isRoot)
        {
            this.Element = element;
            string n = null;
            if (!string.IsNullOrEmpty(element.Name)) n = element.Name;
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (isRoot)
            {
                Properties.Add(new SelectorItemProperty("Selector", "Java"));
                Enabled = true;
                canDisable = false;
                return;
            }
            if (!string.IsNullOrEmpty(element.id)) Properties.Add(new SelectorItemProperty("id", element.id));
            if (!string.IsNullOrEmpty(element.Name)) Properties.Add(new SelectorItemProperty("Name", element.Name));
            if (!string.IsNullOrEmpty(element.role)) Properties.Add(new SelectorItemProperty("role", element.role));
            if (!string.IsNullOrEmpty(element.title)) Properties.Add(new SelectorItemProperty("title", element.title));
            if (element.IndexInParent > -1) Properties.Add(new SelectorItemProperty("Index", element.IndexInParent.ToString()));
            Enabled = (Properties.Count > 1);
            foreach (var p in Properties)
            {
                p.Enabled = true;
                p.canDisable = true;
            };
            foreach (var p in Properties) p.PropertyChanged += (sender, e) =>
            {
                OnPropertyChanged("Displayname");
                OnPropertyChanged("json");
            };
            canDisable = true;
        }
        public string id
        {
            get
            {
                var e = Properties.Where(x => x.Name == "id").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string Name
        {
            get
            {
                var e = Properties.Where(x => x.Name == "Name").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string role
        {
            get
            {
                var e = Properties.Where(x => x.Name == "role").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string title
        {
            get
            {
                var e = Properties.Where(x => x.Name == "title").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string Index
        {
            get
            {
                var e = Properties.Where(x => x.Name == "Index").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public JavaSelectorItem(JavaElement element)
        {
            this.Element = element;
            if (this.Element == null) throw new Exception("Error!!!");
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (!string.IsNullOrEmpty(element.Name)) Properties.Add(new SelectorItemProperty("Name", element.Name));
            if (!string.IsNullOrEmpty(element.role)) Properties.Add(new SelectorItemProperty("role", element.role));
            if (!string.IsNullOrEmpty(element.title)) Properties.Add(new SelectorItemProperty("title", element.title));
            if (!string.IsNullOrEmpty(element.id)) Properties.Add(new SelectorItemProperty("id", element.id));
            foreach (var p in Properties)
            {
                p.Enabled = true;
                p.canDisable = (Properties.Count > 1);
            };
            foreach (var p in Properties) p.PropertyChanged += (sender, e) =>
            {
                OnPropertyChanged("Displayname");
                OnPropertyChanged("json");
            };
        }
        private string[] GetProperties()
        {
            var result = new List<string>();
            if (Properties.Where(x => x.Name == "id").Count() == 1) result.Add("id");
            if (Properties.Where(x => x.Name == "Name").Count() == 1) result.Add("Name");
            if (Properties.Where(x => x.Name == "role").Count() == 1) result.Add("role");
            if (Properties.Where(x => x.Name == "title").Count() == 1) result.Add("title");
            if (Properties.Where(x => x.Name == "Index").Count() == 1) result.Add("Index");
            return result.ToArray();
        }
        public void EnumNeededProperties(JavaElement element, JavaElement parent)
        {
            string name = null;
            if (!string.IsNullOrEmpty(element.Name)) name = element.Name;
            if (!string.IsNullOrEmpty(element.role)) name = element.role;
            if (!string.IsNullOrEmpty(element.title)) name = element.title;
            if (!string.IsNullOrEmpty(element.id)) name = element.id;
            var props = GetProperties();
            int i = props.Length -1;
            int matchcounter = 0;

            Log.Debug("#****************************************#");
            Log.Debug("# EnumNeededProperties ");
            do
            {
                Log.Debug("#****************************************#");
                Log.Debug("# " + i);
                var selectedProps = props.Take(i).ToArray();
                foreach (var p in Properties) p.Enabled = selectedProps.Contains(p.Name);
                JavaElement[] children = element.Children;
                foreach (JavaElement elementNode in children)
                {
                    if (match(elementNode)) matchcounter++;
                    if (matchcounter > 1) break;
                }
                if (matchcounter != 1)
                {
                    Log.Debug("EnumNeededProperties match with " + i + " gave more than 1 result");
                    ++i;
                    if (i >= props.Count()) break;
                }
            } while (matchcounter != 1 && i < props.Count());

            Log.Debug("EnumNeededProperties match with " + i + " gave " + matchcounter + " result");
            Properties.ForEach((e) => e.Enabled = false);
            foreach (var p in props.Take(i).ToArray())
            {
                Properties.Where(x => x.Name == p).First().Enabled = true;
            }
        }
        public JavaElement[] matches(JavaElement element)
        {
            int counter = 0;
            do
            {
                try
                {
                    var matchs = new List<JavaElement>();
                    JavaElement[] elements = element.Children;
                    foreach (JavaElement elementNode in elements)
                    {
                        if (match(elementNode)) matchs.Add(elementNode);
                    }
                    Log.Debug("match count: " + matchs.Count);
                    return matchs.ToArray();
                }
                catch (Exception)
                {
                    ++counter;
                    if (counter == 2) throw;
                }
            } while (counter < 2);
            return new JavaElement[] { };
        }
        public bool match(JavaElement m)
        {
            foreach (var p in Properties.Where(x => x.Enabled == true && x.Value != null))
            {
                if (p.Name == "Name")
                {
                    if (!string.IsNullOrEmpty(m.Name))
                    {
                        var v = m.Name;
                        if (!PatternMatcher.FitsMask(v, p.Value))
                        {
                            Log.Debug(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Debug(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "role")
                {
                    if (!string.IsNullOrEmpty(m.role))
                    {
                        var v = m.role;
                        if (!PatternMatcher.FitsMask(m.role, p.Value))
                        {
                            Log.Debug(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Debug(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "title")
                {
                    if (!string.IsNullOrEmpty(m.title))
                    {
                        var v = m.title;
                        if (!PatternMatcher.FitsMask(m.title, p.Value))
                        {
                            Log.Debug(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Debug(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "Id")
                {
                    if (!string.IsNullOrEmpty(m.id))
                    {
                        var v = m.id;
                        if (!PatternMatcher.FitsMask(m.id, p.Value))
                        {
                            Log.Debug(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Debug(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
            }
            Log.Debug("match: " + ToString());
            return true;
        }
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(title)) return title;
            return "id:" + id + " role:" + role + " Name: " + Name;
        }

    }
}
