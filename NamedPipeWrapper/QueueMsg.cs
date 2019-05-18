using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeWrapper
{
    public class QueueMsg<T> where T : PipeMessage
    {
        public QueueMsg(T message)
        {
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            messageid = message.messageid;
        }
        public System.Diagnostics.Stopwatch sw { get; set; }
        public AutoResetEvent autoReset { get; set; }
        public string messageid { get; set; }
        public T result { get; set; }
    }
}
