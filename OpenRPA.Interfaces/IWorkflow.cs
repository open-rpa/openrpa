using Newtonsoft.Json;
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
    public interface IWorkflow : IProjectableBase
    {
        [JsonIgnore]
        long current_version { get; set; }
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
        bool save_output { get; set; }
        bool send_output { get; set; }
        string queue { get; set; }
        string Xaml { get; set; }
        string culture { get; set; }        
        [JsonIgnore]
        string RelativeFilename { get; }
        string FilePath { get; }
        string Filename { get; set; }
        bool Serializable { get; set; }
        bool background { get; set; }
        string ProjectAndName { get; set; }
        List<workflowparameter> Parameters { get; set; }
        IWorkflowInstance CreateInstance(Dictionary<string, object> Parameters, string queuename, string correlationId, idleOrComplete idleOrComplete, VisualTrackingHandler VisualTracking);
        string UniqueFilename();
        Task Delete(bool skipOnline = false);
        Task Save(bool skipOnline = false);
        Task UpdateImagePermissions();
        Task Update(IWorkflow item, bool skipOnline = false);
        void ParseParameters();
        void NotifyUIState();
        [JsonIgnore]
        string State { get; }

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
