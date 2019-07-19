using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.entity
{
    public class metadata : apibase
    {
        public string filename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string path { get { return GetProperty<string>(); } set { SetProperty(value); } }
    }
}
