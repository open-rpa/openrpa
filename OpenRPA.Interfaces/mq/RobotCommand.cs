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
        public string nodeId { get; set; }        
        public string detectorid { get; set; }
        public bool killexisting { get; set; }
        public bool killallexisting { get; set; }
        public Newtonsoft.Json.Linq.JObject data { get; set; }
        // public Dictionary<string, object> parameters { get; set; }
    }
    public class RobotOutputCommand
    {
        public string command { get; set; }
        public string workflowid { get; set; }
        public string flowid { get; set; }
        public string detectorid { get; set; }
        public bool killexisting { get; set; }
        public bool killallexisting { get; set; }
        public int level { get; set; }
        public string data { get; set; }
    }
}
