using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class RobotCommand
    {
        public string command { get; set; }
        public string workflowid { get; set; }
        public string detectorid { get; set; }
        public JObject data { get; set; }
        // public Dictionary<string, object> parameters { get; set; }
    }
}
