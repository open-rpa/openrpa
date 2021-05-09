using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class CloseQueueMessage : SocketCommand
    {
        public CloseQueueMessage() : base()
        {
            msg.command = "closequeue";
        }
        public CloseQueueMessage(string queuename) : base()
        {
            this.queuename = queuename;
            msg.command = "closequeue";
        }
        public string queuename { get; set; }
    }
}
