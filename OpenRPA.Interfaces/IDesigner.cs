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
        System.Activities.Argument GetArgument(string Name, bool add, Type type);
        System.Activities.DynamicActivityProperty GetArgumentOf<T>(string Name, bool add);
        System.Activities.Variable GetVariable(string Name, Type type);
        System.Activities.Variable<T> GetVariableOf<T>(string Name);
        IWorkflow Workflow { get; set; }
        void forceHasChanged(bool value);
        System.Collections.ObjectModel.KeyedCollection<string, System.Activities.DynamicActivityProperty> GetParameters();
        List<ModelItem> GetWorkflowActivities();
        Task<bool> SaveAsync();
    }
}
