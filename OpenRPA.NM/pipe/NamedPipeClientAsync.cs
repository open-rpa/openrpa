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
    public partial class NamedPipeClientAsync<T> : NamedPipeClient<T> where T : PipeMessage
    {
        public NamedPipeClientAsync(string pipeName) : base(pipeName)
        {
        }

        async public Task<T> MessageAsync(T message, bool throwError)
        {
            T result = default(T);
            if (pipe == null || !pipe.isConnected) return result;

            var queue = new queuemsg<T>(message);
            replyqueue.Add(queue);
            Log.Debug("ASYNC Send and queue message " + message.messageid);
            using (queue.autoReset = new AutoResetEvent(false))
            {
                pipe.PushMessage(message);
                await queue.autoReset.WaitOneAsync();
                queue.sw.Stop();
            }
            Log.Debug("ASYNC received reply for " + message.messageid + " " + string.Format("Time elapsed: {0:mm\\:ss\\.fff}", queue.sw.Elapsed));
            replyqueue.Remove(queue);
            result = queue.result;
            if (!string.IsNullOrEmpty(result.error) && throwError) throw new NamedPipeException(result.error);
            return result;
        }

    }
}
