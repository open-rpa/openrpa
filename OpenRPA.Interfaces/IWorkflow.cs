using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public delegate void idleOrComplete(IWorkflowInstance sender, EventArgs e);
    public delegate void VisualTrackingHandler(IWorkflowInstance Instance, string ActivityId, string ChildActivityId, string State);
    public interface IWorkflow : INotifyPropertyChanged
    {
        string _id { get; set; }
        string name { get; set; }
        string queue { get; set; }
        string Xaml { get; set; }
        string projectid { get; set; }
        bool Serializable { get; set; }
        List<workflowparameter> Parameters { get; set; }
        IWorkflowInstance CreateInstance(Dictionary<string, object> Parameters, string queuename, string correlationId, idleOrComplete idleOrComplete, VisualTrackingHandler VisualTracking);
    }
    public enum workflowparameterdirection
    {
        @in = 0,
        @out = 1,
        inout = 2,
    }
    public class workflowparameter
    {
        public string name { get; set; }
        public string type { get; set; }
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public workflowparameterdirection direction { get; set; }
    }

}
