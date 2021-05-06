using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class EnsureNoderedInstanceMessage : SocketCommand
    {
        public EnsureNoderedInstanceMessage() : base()
        {
            msg.command = "ensurenoderedinstance";
        }
        public string _id { get; set; }

    }
    class DeleteNoderedInstanceMessage : SocketCommand
    {
        public DeleteNoderedInstanceMessage() : base()
        {
            msg.command = "deletenoderedinstance";
        }
        public string _id { get; set; }

    }
    class RestartNoderedInstanceMessage : SocketCommand
    {
        public RestartNoderedInstanceMessage() : base()
        {
            msg.command = "restartnoderedinstance";
        }
        public string _id { get; set; }

    }

}
