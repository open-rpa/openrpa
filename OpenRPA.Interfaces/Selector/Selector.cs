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
