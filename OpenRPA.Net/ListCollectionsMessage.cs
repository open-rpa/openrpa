using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class ListCollectionsMessage : SocketCommand
    {
        public ListCollectionsMessage() : base()
        {
            msg.command = "listcollections";
            includehist = false;
        }
        public bool includehist { get; set; }
        public Collection[] result { get; set; }
    }
    public class Collection : Interfaces.ICollection
    {
        public string name { get; set; }
        public string type { get; set; }
    }
}
