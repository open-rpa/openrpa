using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.entity
{
    public class snippet : apibase
    {
        public snippet() : base() { _type = "snippet"; }
        public string category { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string xaml { get { return GetProperty<string>(); } set { SetProperty(value); } }
    }
}
