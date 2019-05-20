using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    class NMSelectorItem : SelectorItem
    {
        public NMSelectorItem() { }
        public NMSelectorItem(SelectorItem item)
        {
            SetBackingFieldValues(item._backingFieldValues);
            Properties = item.Properties;
        }
        public NMSelectorItem(NMElement element, bool isRoot)
        {
            this.Element = element;
            string n = null;
            if (!string.IsNullOrEmpty(element.Name)) n = element.Name;
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (isRoot)
            {
                Properties.Add(new SelectorItemProperty("Selector", "NM"));
                Properties.Add(new SelectorItemProperty("browser", element.message.browser));
                Properties.Add(new SelectorItemProperty("frame", element.message.frame));
                Properties.Add(new SelectorItemProperty("url", element.message.tab.url));
                Enabled = true;
                canDisable = false;
                return;
            }
            if (!string.IsNullOrEmpty(element.xpath)) Properties.Add(new SelectorItemProperty("xpath", element.xpath));
            if (!string.IsNullOrEmpty(element.cssselector)) Properties.Add(new SelectorItemProperty("cssselector", element.cssselector) { Enabled = false });

            if (!string.IsNullOrEmpty(element.id)) Properties.Add(new SelectorItemProperty("id", element.id) { Enabled = false });
            if (!string.IsNullOrEmpty(element.Name)) Properties.Add(new SelectorItemProperty("Name", element.Name) { Enabled = false });
            if (!string.IsNullOrEmpty(element.type)) Properties.Add(new SelectorItemProperty("type", element.type) { Enabled = false });
            if (!string.IsNullOrEmpty(element.tagname)) Properties.Add(new SelectorItemProperty("tagname", element.tagname) { Enabled = false });
            Enabled = (Properties.Count > 1);
            //foreach (var p in Properties)
            //{
            //    p.Enabled = true;
            //    p.canDisable = true;
            //};
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
        public string type
        {
            get
            {
                var e = Properties.Where(x => x.Name == "type").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string tagname
        {
            get
            {
                var e = Properties.Where(x => x.Name == "tagname").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        
        public NMSelectorItem(NMElement element)
        {
            this.Element = element;
            if (this.Element == null) throw new Exception("Error!!!");
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (!string.IsNullOrEmpty(element.Name)) Properties.Add(new SelectorItemProperty("Name", element.Name));
            if (!string.IsNullOrEmpty(element.type)) Properties.Add(new SelectorItemProperty("type", element.type));
            if (!string.IsNullOrEmpty(element.tagname)) Properties.Add(new SelectorItemProperty("tagname", element.tagname));
           
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
            if (Properties.Where(x => x.Name == "xpath").Count() == 1) result.Add("xpath");
            if (Properties.Where(x => x.Name == "cssselector").Count() == 1) result.Add("cssselector");
            if (Properties.Where(x => x.Name == "id").Count() == 1) result.Add("id");
            if (Properties.Where(x => x.Name == "Name").Count() == 1) result.Add("Name");
            if (Properties.Where(x => x.Name == "type").Count() == 1) result.Add("type");
            if (Properties.Where(x => x.Name == "tagname").Count() == 1) result.Add("tagname");
            
            return result.ToArray();
        }
        public void EnumNeededProperties(NMElement element, NMElement parent)
        {
            string name = null;
            if (!string.IsNullOrEmpty(element.Name)) name = element.Name;
            if (!string.IsNullOrEmpty(element.type)) name = element.type;
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
                NMElement[] children = { };
                if(element.Parent != null) { children = element.Parent.Children; }
                matchcounter = 0;
                foreach (NMElement elementNode in children)
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
        public NMElement[] matches(NMElement element)
        {
            int counter = 0;
            do
            {
                try
                {
                    var matchs = new List<NMElement>();
                    NMElement[] elements = element.Children;
                    foreach (NMElement elementNode in elements)
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
            return new NMElement[] { };
        }
        public override string ToString()
        {
            return "id:" + id + " type:" + type + " Name: " + Name;
        }
        public bool Match(NMElement m)
        {
            return Match(this, m);
        }
        public static bool Match(SelectorItem item, NMElement m)
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
                if (p.Name == "tagname")
                {
                    if (!string.IsNullOrEmpty(m.tagname))
                    {
                        var v = m.tagname;
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
                if (p.Name == "xpath")
                {
                    if (!string.IsNullOrEmpty(m.xpath))
                    {
                        var v = m.xpath;
                        if (!PatternMatcher.FitsMask(m.xpath, p.Value))
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
                if (p.Name == "cssselector")
                {
                    if (!string.IsNullOrEmpty(m.cssselector))
                    {
                        var v = m.cssselector;
                        if (!PatternMatcher.FitsMask(m.cssselector, p.Value))
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
                if (p.Name == "type")
                {
                    if (!string.IsNullOrEmpty(m.type))
                    {
                        var v = m.type;
                        if (!PatternMatcher.FitsMask(m.type, p.Value))
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
