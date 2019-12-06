using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.entity
{
    public class apirole : apibase
    {
        public Rolemember[] members { get; set; }
        public bool rparole { get; set; }
        public override string ToString()
        {
            if(members!=null)
            {
                return name  + "[" + members.Count() + "]";
            }
            return name;
        }

    }
}
