using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.mq
{
    public class InvokeWorkflowCommand
    {
        public string state { get; set; }
        public string _id { get; set; }
        public string jwt { get; set; }
        public Newtonsoft.Json.Linq.JObject payload { get; set; }
        // public Dictionary<string, object> parameters { get; set; }
    }
}
