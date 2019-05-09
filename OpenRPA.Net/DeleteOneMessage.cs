using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class DeleteOneMessage : SocketCommand
    {
        public DeleteOneMessage() : base()
        {
            msg.command = "deleteone";
        }
        public string _id { get; set; }
        public string collectionname { get; set; }
    }

}
