using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface ICustomWorkflowExtension
    {
        void Initialize(IOpenRPAClient client, IWorkflow workflow, IWorkflowInstance instance);
    }
}
