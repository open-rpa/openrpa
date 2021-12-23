using System;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IDesigner
    {
        bool HasChanged { get; set; }
        System.Activities.Presentation.WorkflowDesigner WorkflowDesigner { get; }
        System.Activities.Argument GetArgument(string Name, bool add, Type type);
        System.Activities.DynamicActivityProperty GetArgumentOf<T>(string Name, bool add);
        System.Activities.Variable GetVariable(string Name, Type type);
        System.Activities.Variable<T> GetVariableOf<T>(string Name);
        IWorkflow Workflow { get; set; }
        bool VisualTracking { get; set; }
        bool SlowMotion { get; set; }
        bool IsSelected { get; set; }
        void forceHasChanged(bool value);
        System.Collections.ObjectModel.KeyedCollection<string, System.Activities.DynamicActivityProperty> GetParameters();
        List<ModelItem> GetWorkflowActivities();
        Task<bool> SaveAsync();
        IDictionary<System.Activities.Debugger.SourceLocation, System.Activities.Presentation.Debug.BreakpointTypes> BreakpointLocations { get; set; }
        void OnVisualTracking(IWorkflowInstance Instance, string ActivityId, string ChildActivityId, string State);
        void IdleOrComplete(IWorkflowInstance instance, EventArgs e);
        void Run(bool VisualTracking, bool SlowMotion, IWorkflowInstance instance);
        ModelItem AddRecordingActivity(System.Activities.Activity a, IPlugin plugin);
        ModelItem AddActivity(System.Activities.Activity a);
    }
}
