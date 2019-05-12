using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.Selector
{
    public partial class SelectorItem : ObservableObject
    {
        public IElement Element { get { return GetProperty<IElement>(); } set { SetProperty(value); } }
        public bool Enabled { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public bool canDisable { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        //public string Selector { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string Selector
        {
            get
            {
                if (Properties == null) return null;
                var v = Properties.Where(x => x.Name == "Selector").FirstOrDefault();
                if (v == null) return null;
                return v.Value;
            }
        }
        public ObservableCollection<SelectorItemProperty> Properties { get; set; }
        public SelectorItem() { Enabled = true; }
        public SelectorItem(JObject o)
        {
            Properties = new ObservableCollection<SelectorItemProperty>();
            foreach (var k in o)
            {
                Properties.Add(new SelectorItemProperty(k.Key, k.Value.ToString()));
            }
            foreach (var p in Properties)
            {
                p.Enabled = true;
                p.canDisable = true;
                p.PropertyChanged += (sender, e) =>
                {
                    OnPropertyChanged("Displayname");
                    OnPropertyChanged("json");
                };
            };
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "Displayname" || e.PropertyName == "json") return;
                OnPropertyChanged("Displayname");
                OnPropertyChanged("json");
            };
           Enabled = true;
            canDisable = string.IsNullOrEmpty(Selector);
        }
        public JObject json
        {
            get
            {
                var elements = new List<JProperty>();
                foreach (var j in Properties)
                {
                    if (j.Enabled) elements.Add(j.json);
                }
                return new JObject(elements);
            }
        }
        public string Displayname
        {
            get
            {
                return json.ToString().Replace(Environment.NewLine, " ").Trim();
            }
        }
        public override string ToString()
        {
            return Displayname;
        }

    }
}
