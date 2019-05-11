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
        public ObservableCollection<SelectorItem> Properties { get; set; }
        public Selector() { }
        public Selector(string json)
        {
            //if (string.IsNullOrEmpty(json)) return; We want to know, if someone sends illegal json
            JArray a = JArray.Parse(json);
            foreach (JObject o in a)
            {
                var item = new SelectorItem(o);
                item.Enabled = true;
                Items.Add(item);
            }

        }
        public JObject json
        {
            get
            {
                var elements = new List<JObject>();
                foreach (var j in Properties)
                {
                    if (j.Enabled) elements.Add(j.json);
                }
                return new JObject(elements);
            }
        }
        public void SelectorChanged(object sender, PropertyChangedEventArgs e)
        {
            ElementChanged?.Invoke();
        }
        public event Action ElementChanged;
        public virtual IElement[] GetElements(IElement fromElement = null) { return new IElement[] { }; }
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
