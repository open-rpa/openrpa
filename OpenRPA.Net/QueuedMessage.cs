using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class QueuedMessage
    {
        public Message msg { get; set; }
        public Message reply { get; set; }
        public AutoResetEvent autoReset { get; set; }
        public QueuedMessage(Message msg)
        {
            this.msg = msg;
        }
    }
}
