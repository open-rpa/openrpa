using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class QueryMessage<T> : SocketCommand
    {
        public QueryMessage() : base()
        {
            msg.command = "query";
        }
        public JObject query { get; set; }
        public string projection { get; set; }
        public string queryas { get; set; }
        public int top { get; set; }
        public int skip { get; set; }
        public string orderby { get; set; }
        public string collectionname { get; set; }
        public T[] result { get; set; }

    }
}
