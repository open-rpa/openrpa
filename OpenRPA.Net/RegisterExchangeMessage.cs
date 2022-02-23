using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class RegisterExchangeMessage : SocketCommand
    {
        public RegisterExchangeMessage() : base()
        {
            msg.command = "registerexchange";
        }
        public RegisterExchangeMessage(string exchangename, string algorithm) : base()
        {
            this.exchangename = exchangename;
            msg.command = "registerexchange";
            queuename = "";
            this.algorithm = algorithm;
            addqueue = false;
        }
        public string exchangename { get; set; }
        public string algorithm { get; set; }
        public string routingkey { get; set; }
        public bool addqueue { get; set; }        
        public string queuename { get; set; }
    }
}
