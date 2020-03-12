using OpenRPA.NamedPipeWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.NM.pipe
{
    public class queuemsg<T> where T : PipeMessage
    {
        public queuemsg(T message)
        {
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            messageid = message.messageid;
            Received = false;
        }
        public System.Diagnostics.Stopwatch sw { get; set; }
        public AutoResetEvent autoReset { get; set; }
        public bool Received { get; set; }
        public string messageid { get; set; }
        public T result { get; set; }
    }
}
