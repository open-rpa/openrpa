using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class RegisterQueueMessage : SocketCommand
    {
        public RegisterQueueMessage() : base()
        {
            msg.command = "registerqueue";
        }
        public RegisterQueueMessage(string queuename) : base()
        {
            this.queuename = queuename;
            msg.command = "registerqueue";
        }
        public string queuename { get; set; }
    }
}
