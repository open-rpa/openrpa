using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class BaseMessage
    {
        public string id { get; set; }
        public string replyto { get; set; }
        public string command { get; set; }
        public string data { get; set; }
        public void reply(string command)
        {
            this.command = command;
            replyto = id;
            id = Guid.NewGuid().ToString();
        }
        public void reply()
        {
            replyto = id;
            id = Guid.NewGuid().ToString();
        }
    }
}
