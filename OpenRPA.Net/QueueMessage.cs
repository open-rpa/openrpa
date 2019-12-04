using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class QueueMessage : SocketCommand, Interfaces.IQueueMessage
    {
        public QueueMessage() : base()
        {
            msg.command = "queuemessage";
            correlationId = Guid.NewGuid().ToString();
        }
        public QueueMessage(string queuename) : base()
        {
            this.queuename = queuename;
            msg.command = "queuemessage";
            correlationId = Guid.NewGuid().ToString();
        }
        public string queuename { get; set; }
        // public string data { get; set; }
        public object data { get; set; }
        public string correlationId { get; set; }
        public string replyto { get; set; }

    }
}
