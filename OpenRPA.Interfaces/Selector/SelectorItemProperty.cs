using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.Selector
{
    public class SelectorItemProperty : ObservableObject
    {
        public SelectorItemProperty(string name, string value)
        {
            Name = name;
            Value = value;
            Enabled = (name != "ControlType");
            canDisable = true;
        }
        public string Name { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string Value { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public bool Enabled { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public bool canDisable { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public JProperty json
        {
            get
            {
                if (!Enabled) return null;
                var e1 = Value.JParse();
                var e2 = Value;
                return new JProperty(Name, Value);
                //return new JProperty(Name, Value.JParse());
            }
        }
        public override string ToString()
        {
            return new JProperty(Name, Value).ToString();
        }
    }
}
