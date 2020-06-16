using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAP
{
    class SAPSelectorItem : SelectorItem
    {
        public SAPSelectorItem() { }
        public SAPSelectorItem(SelectorItem item)
        {
            SetBackingFieldValues(item._backingFieldValues);
            Properties = item.Properties;
        }
        public SAPSelectorItem(SAPElement element, bool isRoot)
        {
            this.Element = element;
            string n = null;
            if (!string.IsNullOrEmpty(element.Name)) n = element.Name;
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (isRoot)
            {
                Properties.Add(new SelectorItemProperty("Selector", "SAP"));
                if (!string.IsNullOrEmpty(element.SystemName)) Properties.Add(new SelectorItemProperty("SystemName", element.SystemName));
                Enabled = true;
                canDisable = false;
                return;
            }
            if (!string.IsNullOrEmpty(element.id)) Properties.Add(new SelectorItemProperty("id", element.id));
            if (!string.IsNullOrEmpty(element.Path)) Properties.Add(new SelectorItemProperty("path", element.Path));
            if (!string.IsNullOrEmpty(element.Cell)) Properties.Add(new SelectorItemProperty("cell", element.Cell));
            //if (!string.IsNullOrEmpty(element.Name)) Properties.Add(new SelectorItemProperty("Name", element.Name));
            //if (!string.IsNullOrEmpty(element.Role)) Properties.Add(new SelectorItemProperty("Role", element.Role));
            //if (!string.IsNullOrEmpty(element.Tip)) Properties.Add(new SelectorItemProperty("Tip", element.Tip));
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
        public string path
        {
            get
            {
                var e = Properties.Where(x => x.Name == "path").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string cell
        {
            get
            {
                var e = Properties.Where(x => x.Name == "cell").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string SystemName
        {
            get
            {
                var e = Properties.Where(x => x.Name == "SystemName").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        //public string Name
        //{
        //    get
        //    {
        //        var e = Properties.Where(x => x.Name == "Name").FirstOrDefault();
        //        if (e == null) return null;
        //        return e.Value;
        //    }
        //}
        //public string Role
        //{
        //    get
        //    {
        //        var e = Properties.Where(x => x.Name == "Role").FirstOrDefault();
        //        if (e == null) return null;
        //        return e.Value;
        //    }
        //}
        //public string Tip
        //{
        //    get
        //    {
        //        var e = Properties.Where(x => x.Name == "Tip").FirstOrDefault();
        //        if (e == null) return null;
        //        return e.Value;
        //    }
        //}
        //public string IndexInParent
        //{
        //    get
        //    {
        //        var e = Properties.Where(x => x.Name == "IndexInParent").FirstOrDefault();
        //        if (e == null) return null;
        //        return e.Value;
        //    }
        //}
        public SAPSelectorItem(SAPElement element)
        {
            this.Element = element;
            if (this.Element == null) throw new Exception("Error!!!");
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (!string.IsNullOrEmpty(element.Name)) Properties.Add(new SelectorItemProperty("Name", element.Name));
            if (!string.IsNullOrEmpty(element.Role)) Properties.Add(new SelectorItemProperty("Role", element.Role));
            if (!string.IsNullOrEmpty(element.Tip)) Properties.Add(new SelectorItemProperty("Tip", element.Tip));
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
            if (Properties.Where(x => x.Name == "Role").Count() == 1) result.Add("Role");
            if (Properties.Where(x => x.Name == "Tip").Count() == 1) result.Add("Tip");
            if (Properties.Where(x => x.Name == "IndexInParent").Count() == 1) result.Add("IndexInParent");
            return result.ToArray();
        }
        public void EnumNeededProperties(SAPElement element, SAPElement parent)
        {
            string name = null;
            if (!string.IsNullOrEmpty(element.Name)) name = element.Name;
            if (!string.IsNullOrEmpty(element.Role)) name = element.Role;
            if (!string.IsNullOrEmpty(element.Tip)) name = element.Tip;
            if (!string.IsNullOrEmpty(element.id)) name = element.id;
            var props = GetProperties();
            //int i = props.Length -1;
            int i = 1;
            int matchcounter = 0;

            foreach (var p in Properties) p.Enabled = false;
            do
            {
                Log.Selector("#*******************************#");
                Log.Selector("# " + i);
                var selectedProps = props.Take(i).ToArray();
                foreach (var p in Properties) p.Enabled = selectedProps.Contains(p.Name);
                SAPElement[] children = { };
                if(element.Parent != null) { children = element.Parent.Children; }
                matchcounter = 0;
                foreach (SAPElement elementNode in children)
                {
                    Log.Selector("Match using " + i + " properties.");
                    if (Match(elementNode)) matchcounter++;
                    if (matchcounter > 1)
                    {
                        break;
                    }
                }
                if (matchcounter != 1) { ++i; }
                //if (matchcounter > 1)
                //{
                //    Log.Selector("EnumNeededProperties match with " + i + " gave more than 1 result");
                //    ++i;
                //    if (i >= props.Count()) break;
                //}
            } while (matchcounter != 1 && i < props.Count());

            Log.Selector("EnumNeededProperties match with " + i + " gave " + matchcounter + " result");
            Properties.ForEach((e) => e.Enabled = false);
            foreach (var p in props.Take(i).ToArray())
            {
                Properties.Where(x => x.Name == p).First().Enabled = true;
            }
        }
        public SAPElement[] matches(SAPElement element)
        {
            int counter = 0;
            do
            {
                try
                {
                    var matchs = new List<SAPElement>();
                    SAPElement[] elements = element.Children;
                    foreach (SAPElement elementNode in elements)
                    {
                        if (Match(elementNode)) matchs.Add(elementNode);
                    }
                    Log.Selector("match count: " + matchs.Count);
                    return matchs.ToArray();
                }
                catch (Exception)
                {
                    ++counter;
                    if (counter == 2) throw;
                }
            } while (counter < 2);
            return new SAPElement[] { };
        }
        public override string ToString()
        {
            // if (!string.IsNullOrEmpty(Tip)) return Tip;
            // return "id:" + id + " Role:" + Role + " Name: " + Name;
            if (!string.IsNullOrEmpty(SystemName)) return SystemName;
            return "id:" + id;
        }
        public bool Match(SAPElement m)
        {
            return Match(this, m);
        }
        public static bool Match(SelectorItem item, SAPElement m)
        {
            foreach (var p in item.Properties.Where(x => x.Enabled == true && x.Value != null))
            {
                if (p.Name == "Name")
                {
                    if (!string.IsNullOrEmpty(m.Name))
                    {
                        var v = m.Name;
                        if (!PatternMatcher.FitsMask(v, p.Value))
                        {
                            Log.Selector(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Selector(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "Role")
                {
                    if (!string.IsNullOrEmpty(m.Role))
                    {
                        var v = m.Role;
                        if (!PatternMatcher.FitsMask(m.Role, p.Value))
                        {
                            Log.Selector(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Selector(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "Tip")
                {
                    if (!string.IsNullOrEmpty(m.Tip))
                    {
                        var v = m.Tip;
                        if (!PatternMatcher.FitsMask(m.Tip, p.Value))
                        {
                            Log.Selector(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Selector(p.Name + " does not exists, but needed value '" + p.Value + "'");
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
                            Log.Selector(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Selector(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }

            }
            return true;
        }
    }
}
