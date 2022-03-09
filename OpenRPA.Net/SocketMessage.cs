using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class SocketMessage : BaseMessage
    {
        public int priority { get; set; } = 2;
        public SocketMessage() { }
        public SocketMessage(Message msg, int priority)
        {
            id = msg.id;
            replyto = msg.replyto;
            command = msg.command;
            data = msg.data;
            count = 1;
            index = 0;
            this.priority = priority;
        }
        public SocketMessage(Message msg, string data, int count, int index, int priority)
        {
            id = msg.id;
            replyto = msg.replyto;
            command = msg.command;
            this.data = data;
            this.count = count;
            this.index = index;
            this.priority = priority;
        }
        public int count { get; set; }
        public int index { get; set; }
        public void Send(WebSocketClient ws)
        {
            ws.PushMessage(this);
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(id)) return base.ToString();
            if (string.IsNullOrEmpty(command)) return id;
            return id + ":" + command;

        }
        [Newtonsoft.Json.Serialization.OnError]
        internal void OnError(System.Runtime.Serialization.StreamingContext context, Newtonsoft.Json.Serialization.ErrorContext errorContext)
        {
            errorContext.Handled = true;
        }
    }
}
