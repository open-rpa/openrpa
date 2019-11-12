using OpenRPA.NamedPipeWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDServicePlugin
{
    [Serializable]
    public class RPAMessage : PipeMessage
    {
        public RPAMessage() : base()
        {
        }
        public RPAMessage(string command) : base()
        {
            this.command = command;
        }
        public RPAMessage(string command, string windowsusername, Interfaces.entity.TokenUser user, string openrpapath) : base()
        {
            this.command = command;
            this.windowsusername = windowsusername;
            this.user = user;
            this.openrpapath = openrpapath;
        }
        public string command { get; set; }
        public string windowsusername { get; set; }
        public Interfaces.entity.TokenUser user { get; set; }
        public string openrpapath { get; set; }
        
    }
}
