using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Views
{

    public class variable
    {
        public object obj { get; set; }
        public string name { get; set; }
        public string value { get; set; }
        public string details { get; set; }
        public Type type { get; set; }
        public string typename { get; set; }

        public int Level { get; set; }
        public bool IsExpanded { get; set; } = false;
        public bool HasChildren { get; set; } = true;
    }

}
