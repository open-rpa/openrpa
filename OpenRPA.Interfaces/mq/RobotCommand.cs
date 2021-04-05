using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.mq
{
    public class RobotCommand
    {
        public string command { get; set; }
        public string workflowid { get; set; }
        public string flowid { get; set; }
        public string detectorid { get; set; }
        public bool killexisting { get; set; }
        public Newtonsoft.Json.Linq.JObject data { get; set; }
        // public Dictionary<string, object> parameters { get; set; }
    }
}
