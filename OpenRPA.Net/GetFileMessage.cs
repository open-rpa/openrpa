using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class GetFileMessage : SocketCommand
    {
        public GetFileMessage() : base()
        {
            msg.command = "getfile";
        }
        public string filename { get; set; }
        public string file { get; set; }
        public string mimeType { get; set; }
        public Interfaces.entity.metadata metadata { get; set; }
        public string id { get; set; }

    }

}
