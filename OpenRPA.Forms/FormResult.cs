using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Forms
{
    public class FormResult
    {
        public IDictionary<string, object> Model { get; set; }
        public string Action { get; set; }
        public object ActionParameter { get; set; }
    }
}
