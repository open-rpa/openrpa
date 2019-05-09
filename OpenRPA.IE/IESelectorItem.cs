using FlaUI.Core;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    class IESelectorItem : SelectorItem
    {
        public IESelectorItem() { }
        public IESelectorItem(SelectorItem item) {
            SetBackingFieldValues(item._backingFieldValues);
            Properties = item.Properties;
        }
        // public IEElement element { get { return GetProperty<IEElement>(); } set { SetProperty(value); } }

        public string tagName
        {
            get
            {
                var e = Properties.Where(x => x.Name == "tagName").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
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
        public string className
        {
            get
            {
                var e = Properties.Where(x => x.Name == "ClassName").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string AutomationId
        {
            get
            {
                var e = Properties.Where(x => x.Name == "AutomationId").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }

        public IESelectorItem(mshtml.HTMLDocument Document)
        {
            // this.element = new IEElement(element);
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (!string.IsNullOrEmpty(Document.title)) Properties.Add(new SelectorItemProperty("title", Document.title));
            Properties.Add(new SelectorItemProperty("Selector", "IE"));
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
        public IESelectorItem(mshtml.IHTMLElement element)
        {
            this.Element = new IEElement(element);;
            if (this.Element == null) throw new Exception("Error!!!");
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (!string.IsNullOrEmpty(element.tagName)) Properties.Add(new SelectorItemProperty("tagName", element.tagName));
            if (!string.IsNullOrEmpty(element.className)) Properties.Add(new SelectorItemProperty("className", element.className));
            if (!string.IsNullOrEmpty(element.id)) Properties.Add(new SelectorItemProperty("id", element.id));
            //Enabled = (Properties.Count > 1);
            //canDisable = true;
            //Enabled = true;
            //canDisable = true;
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
            if (Properties.Where(x => x.Name == "tagName").Count() == 1) result.Add("tagName");
            if (Properties.Where(x => x.Name == "className").Count() == 1) result.Add("className");
            if (Properties.Where(x => x.Name == "id").Count() == 1) result.Add("id");
            if (Properties.Where(x => x.Name == "Index").Count() == 1) result.Add("Index");
            return result.ToArray();
        }
        public void EnumNeededProperties(mshtml.IHTMLElement element, mshtml.IHTMLElement parent)
        {
            string name = null;
            if (!string.IsNullOrEmpty(element.tagName)) name = element.tagName;
            if (!string.IsNullOrEmpty(element.className)) name = element.className;
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
                mshtml.IHTMLElementCollection children = element.children;
                foreach (mshtml.IHTMLElement elementNode in children)
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
        public mshtml.IHTMLElement[] matches(mshtml.IHTMLElement element)
        {
            int counter = 0;
            do
            {
                try
                {
                    var matchs = new List<mshtml.IHTMLElement>();
                    mshtml.IHTMLElementCollection elements = element.children;
                    foreach (mshtml.IHTMLElement elementNode in elements)
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
            return new mshtml.IHTMLElement[] { };
        }
        public bool match(mshtml.IHTMLElement m)
        {
            foreach (var p in Properties.Where(x => x.Enabled == true && x.Value != null))
            {
                if (p.Name == "tagName")
                {
                    if (!string.IsNullOrEmpty(m.tagName))
                    {
                        var v = m.tagName;
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
                if (p.Name == "className")
                {
                    if (!string.IsNullOrEmpty(m.className))
                    {
                        var v = m.className;
                        if (!PatternMatcher.FitsMask(m.className, p.Value))
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
                if (p.Name == "type" && m.tagName.ToLower() == "input")
                {
                    mshtml.HTMLInputElement ele = (mshtml.HTMLInputElement)m;
                    if (!string.IsNullOrEmpty(ele.type))
                    {
                        var v = ele.type;
                        if (!PatternMatcher.FitsMask(ele.type, p.Value))
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
            return tagName + " " + (!string.IsNullOrEmpty(id) ? id : className);
        }

    }
}
