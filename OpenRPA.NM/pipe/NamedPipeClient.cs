using NamedPipeWrapper;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.NM.pipe
{
    public partial class NamedPipeClient<T> where T : PipeMessage
    {
        public NamedPipeWrapper.NamedPipeClient<T> pipe = null;
        public List<queuemsg<T>> replyqueue = null;
        public event Action<T> ServerMessage;
        public event Action Disconnected;
        public event Action Connected;
        public event Action<Exception> Error;
        public NamedPipeClient(string pipeName)
        {
            replyqueue = new List<queuemsg<T>>();
            pipe = new NamedPipeWrapper.NamedPipeClient<T>(pipeName);
            pipe.AutoReconnect = true;
            pipe.Disconnected += (sender) => { Disconnected?.Invoke(); };
            pipe.Connected += (sender) => { Connected?.Invoke(); };
            pipe.Error += (e) => { Error?.Invoke(e); };
            pipe.ServerMessage += (sender, message) => {

                var queue = replyqueue.Where(x => x.messageid == message.messageid).FirstOrDefault();
                if (queue != null)
                {
                    queue.result = message;
                    queue.autoReset.Set();
                    return;
                }
                ServerMessage?.Invoke(message);
            };
        }

        public bool isConnected { get { return pipe.isConnected; } }
        public void Start() { pipe.Start(); }
        public void Stop() { pipe.Stop(); }
        public void PushMessage(T message)
        {
            pipe.PushMessage(message);
        }
        public T Message(T message, bool throwError)
        {
            T result = default(T);
            if (pipe == null || !pipe.isConnected) return result;

            var queue = new queuemsg<T>(message);
            replyqueue.Add(queue);
            Log.Verbose("Send and queue message " + message.messageid);
            using (queue.autoReset = new AutoResetEvent(false))
            {
                pipe.PushMessage(message);
                queue.autoReset.WaitOne();
                queue.sw.Stop();
            }
            Log.Verbose("received reply for " + message.messageid + " " + string.Format("Time elapsed: {0:mm\\:ss\\.fff}", queue.sw.Elapsed));
            replyqueue.Remove(queue);
            result = queue.result;
            if (!string.IsNullOrEmpty(result.error)) throw new NamedPipeException(result.error);
            return result;
        }
        [Serializable()]
        public class NamedPipeException : Exception
        {
            public NamedPipeException(string message)
                : base(message)
            {
            }
            protected NamedPipeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
            {
            }

        }
    }
}
