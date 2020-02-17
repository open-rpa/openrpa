using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.entity
{
    public class openflowworkflow : apibase
    {
        public openflowworkflow()
        {
            _type = "workflow";
        }
        public string queue { get; set; }
        public bool rpa { get; set; }
        public bool web { get; set; }
    }
}
