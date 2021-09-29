using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class DeleteManyMessage : SocketCommand
    {
        public DeleteManyMessage() : base()
        {
            msg.command = "deletemany";
        }
        public string[] ids { get; set; }
        public string query { get; set; }
        public int affectedrows { get; set; }
        public string collectionname { get; set; }
    }

}
