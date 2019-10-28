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
        public Interfaces.IMessage msg { get; set; }
        public Interfaces.IMessage reply { get; set; }
        public AutoResetEvent autoReset { get; set; }
        public QueuedMessage(Interfaces.IMessage msg)
        {
            this.msg = msg;
        }
    }
}
