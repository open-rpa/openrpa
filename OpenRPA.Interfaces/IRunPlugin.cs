using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IRunPlugin : INotifyPropertyChanged, IPlugin
    {
        bool onWorkflowStarting(ref IWorkflowInstance e, bool resumed);
        bool onWorkflowResumeBookmark(ref IWorkflowInstance e, string bookmarkName, object value);
        void onWorkflowCompleted(ref IWorkflowInstance e);
        void onWorkflowAborted(ref IWorkflowInstance e);
        void onWorkflowIdle(ref IWorkflowInstance e);
    }
}
