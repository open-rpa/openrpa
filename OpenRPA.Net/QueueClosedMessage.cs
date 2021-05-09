using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class QueueClosedMessage : SocketCommand, Interfaces.IQueueClosedMessage
    {
        public QueueClosedMessage() : base()
        {
            msg.command = "closequeue";
        }
        public QueueClosedMessage(string queuename) : base()
        {
            this.queuename = queuename;
            msg.command = "closequeue";
        }
        public string queuename { get; set; }
    }
}
