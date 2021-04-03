using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class WatchEventMessage<T> : SocketCommand
    {
        public WatchEventMessage() : base()
        {
            msg.command = "watchevent";
        }
        public string id { get; set; }
        public T result { get; set; }
    }
    class WatchEventMessage : SocketCommand
    {
        public WatchEventMessage() : base()
        {
            msg.command = "watchevent";
        }
        public string id { get; set; }
        public Newtonsoft.Json.Linq.JObject result { get; set; }
    }
}
