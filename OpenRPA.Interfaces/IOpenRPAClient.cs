using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public delegate void StatusEventHandler(string message);
    public delegate void SignedinEventHandler(entity.TokenUser user);
    public delegate void DisconnectedEventHandler();
    public delegate void ConnectedEventHandler();
    public delegate void ReadyForActionEventHandler();
    public interface IOpenRPAClient
    {
        event StatusEventHandler Status;
        event SignedinEventHandler Signedin;
        event ConnectedEventHandler Connected;
        event DisconnectedEventHandler Disconnected;
        event ReadyForActionEventHandler ReadyForAction;
        System.Collections.ObjectModel.ObservableCollection<IWorkitemQueue> WorkItemQueues { get; set; }
        bool isReadyForAction { get; set; }
        bool isRunningInChildSession { get; }
        IMainWindow Window { get; set; }
        IDesigner CurrentDesigner { get; }
        IDesigner[] Designers { get; }
        IDesigner GetWorkflowDesignerByIDOrRelativeFilename(string IDOrRelativeFilename);
        IWorkflow GetWorkflowByIDOrRelativeFilename(string IDOrRelativeFilename);
        IWorkflowInstance GetWorkflowInstanceByInstanceId(string InstanceId);
        List<IWorkflowInstance> WorkflowInstances { get; }
        void ParseCommandLineArgs(IList<string> args);

    }
}
