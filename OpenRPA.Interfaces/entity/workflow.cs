using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.entity
{
    public class workflow : apibase, IWorkflow
    {
        public string queue { get; set; }
        public string Xaml { get; set; }
        public string projectid { get; set; }
        public bool Serializable { get; set; }
        public string projectandname { get; set; }
        public List<workflowparameter> Parameters { get; set; }
        IWorkflowInstance IWorkflow.CreateInstance(Dictionary<string, object> Parameters, string queuename, string correlationId, idleOrComplete idleOrComplete, VisualTrackingHandler VisualTracking)
        {
            throw new NotImplementedException();
        }
    }
}
