using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IWorkflowInstance : INotifyPropertyChanged
    {
        [Newtonsoft.Json.JsonProperty(propertyName: "parentspanid")]
        string ParentSpanId { get; set; }
        [Newtonsoft.Json.JsonProperty(propertyName: "spanid")]
        string SpanId { get; set; }
        IWorkflow Workflow { get; set; }
        Dictionary<string, object> Parameters { get; set; }
        Dictionary<string, object> Bookmarks { get; set; }
        string _id { get; set; }
        string correlationId { get; set; }
        string queuename { get; set; }
        string InstanceId { get; set; }
        string WorkflowId { get; set; }
        string caller { get; set; }
        string xml { get; set; }
        string owner { get; set; }
        string ownerid { get; set; }
        string host { get; set; }
        string fqdn { get; set; }
        string errormessage { get; set; }
        string errorsource { get; set; }
        bool isCompleted { get; set; }
        bool hasError { get; set; }
        string state { get; set; }
        Exception Exception { get; set; }
        System.Diagnostics.Stopwatch runWatch { get; set; }
        Dictionary<string, WorkflowInstanceValueType> Variables { get; set; }
        void ResumeBookmark(string bookmarkName, object value);
        void Run();
        void Abort(string Reason);
    }
    public class WorkflowInstanceValueType
    {
        public WorkflowInstanceValueType(Type type, object value) { this.type = type; this.value = value; }
        public Type type { get; set; }
        public object value { get; set; }
    }

}
