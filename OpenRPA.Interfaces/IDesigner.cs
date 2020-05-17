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
        System.Activities.Argument GetArgument(string Name, bool add, Type type);
        System.Activities.DynamicActivityProperty GetArgumentOf<T>(string Name, bool add);
        System.Activities.Variable GetVariable(string Name, Type type);
        System.Activities.Variable<T> GetVariableOf<T>(string Name);
        IWorkflow Workflow { get; set; }
    }
}
