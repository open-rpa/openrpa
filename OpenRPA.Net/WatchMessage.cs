using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class WatchMessage : SocketCommand
    {
        public WatchMessage() : base()
        {
            msg.command = "watch";
        }
        public string id { get; set; }
        public string collectionname { get; set; }
        public Newtonsoft.Json.Linq.JArray aggregates { get; set; }
    }
}
