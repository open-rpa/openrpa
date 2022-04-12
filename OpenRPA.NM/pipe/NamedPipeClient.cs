using OpenRPA.NamedPipeWrapper;
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
        public static object lockobj = new object();
        public event Action<T> ServerMessage;
        public event Action Disconnected;
        public event Action Connected;
        public event Action<Exception> Error;
        public NamedPipeClient(string pipeName)
        {
            if (System.Threading.Monitor.TryEnter(lockobj, 1000))
            {
                try
                {
                    replyqueue = new List<queuemsg<T>>();
                }
                finally
                {
                    System.Threading.Monitor.Exit(lockobj);
                }
            }
            pipe = new NamedPipeWrapper.NamedPipeClient<T>(pipeName);
            pipe.AutoReconnect = true;
            pipe.Disconnected += (sender) => { Disconnected?.Invoke(); };
            pipe.Connected += (sender) => { Connected?.Invoke(); };
            pipe.Error += (e) => { Error?.Invoke(e); };
            pipe.ServerMessage += (sender, message) =>
            {
                queuemsg<T> queue = null;
                if (System.Threading.Monitor.TryEnter(lockobj, 1000))
                {
                    try
                    {
                        queue = replyqueue.Where(x => x != null && x.messageid == message.messageid).FirstOrDefault();
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(lockobj);
                    }
                }

                
                if (queue != null)
                {
                    // Log.Information("received reply for " + message.messageid + " " + string.Format("Time elapsed: {0:mm\\:ss\\.fff}", queue.sw.Elapsed));
                    if (queue.Received) return;
                    queue.result = message;
                    queue.Received = true;
                    if (queue.autoReset != null) queue.autoReset.Set();
                    return;
                }
                else
                {
                    // Log.Information("received reply for unknown message id: " + message.messageid);
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
        public T Message(T message, TimeSpan timeout)
        {
            T result = default(T);
            if (pipe == null || !pipe.isConnected) return result;

            var queue = new queuemsg<T>(message);
            if (System.Threading.Monitor.TryEnter(lockobj, 1000))
            {
                try
                {
                    replyqueue.Add(queue);
                }
                finally
                {
                    System.Threading.Monitor.Exit(lockobj);
                }
            }
            // Log.Information("Send and queue message " + message.messageid);
            using (queue.autoReset = new AutoResetEvent(false))
            {
                pipe.PushMessage(message);
                queue.autoReset.WaitOne(timeout);
                queue.sw.Stop();
            }
            // Log.Debug("received reply for " + message.messageid + " " + string.Format("Time elapsed: {0:mm\\:ss\\.fff}", queue.sw.Elapsed));
            if (System.Threading.Monitor.TryEnter(lockobj, 1000))
            {
                try
                {
                    replyqueue.Remove(queue);
                }
                finally
                {
                    System.Threading.Monitor.Exit(lockobj);
                }
            }
           
            result = queue.result;
            if (result != null && result.error != null)
            {
                string s = result.error.ToString().Trim();
                if (!string.IsNullOrEmpty(s) && s != "{}")
                {
                    Log.Error(result.error.ToString());
                    throw new NamedPipeException(result.error.ToString());
                }
            }
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
