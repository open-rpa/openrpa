using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class CreateWorkflowInstanceMessage : SocketCommand
    {
        public CreateWorkflowInstanceMessage() : base()
        {
            msg.command = "createworkflowinstance";
        }
        public string correlationId { get; set; }
        public string newinstanceid { get; set; }
        public string state { get; set; }
        public string queue { get; set; }
        public string workflowid { get; set; }
        public string resultqueue { get; set; }
        public string targetid { get; set; }
        public string parentid { get; set; }
        public object payload { get; set; }
        public bool initialrun { get; set; }

    }
}
