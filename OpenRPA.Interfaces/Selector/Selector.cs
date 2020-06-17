using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.Selector
{
    public partial class Selector : ExtendedObservableCollection<SelectorItem>
    {
        public static string ReplaceVariables(string selector, System.Activities.WorkflowDataContext DataContext)
        {
            var vars = DataContext.GetProperties();
            int begin, end;
            do
            {
                begin = selector.IndexOf("{{");
                end = selector.IndexOf("}}");
                if (begin > -1 && begin < end)
                {
                    var str = selector.Substring(begin, (end - begin) + 2);
                    var strvar = str.Substring(2, str.Length - 4);

                    var v = vars.Find(strvar, true);
                    if (v != null)
                    {
                        var value = v.GetValue(DataContext);
                        if(value != null)
                        {
                            selector = selector.Replace(str, value.ToString());
                        }
                    }
                    else
                    {
                        selector = selector.Replace(str, "");
                    }
                }
                else { begin = 0; end = 0; }
            } while (begin > 0 && end > 0);
            return selector;
        }
        public Selector() {  }

        //public event PropertyChangedEventHandler ElementPropertyChanged;
        //public void NotifyPropertyChanged(string propertyName)
        //{
        //    ElementPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}


        public Selector(string json)
        {
            //if (string.IsNullOrEmpty(json)) return; We want to know, if someone sends illegal json
            JArray a = JArray.Parse(json);
            foreach (JObject o in a)
            {
                var item = new SelectorItem(o);
                item.Enabled = true;
                // Items.Add(item);
                Add(item);
            }
            //foreach(var p in Items) p.PropertyChanged += (sender, e) =>
            //{
            //    OnPropertyChanged(e);
            //};

        }
        public virtual IElement[] GetElements(IElement fromElement = null, int maxresults = 1) { return new IElement[] { }; }
        public override string ToString()
        {
            var results = new List<JObject>();
            foreach (var j in Items)
            {
                if (j.Enabled) results.Add(j.json);
            }
            return new JArray(results).ToString();
        }

    }
}
