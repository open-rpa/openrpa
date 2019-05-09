using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class UpdateOneMessage<T> : SocketCommand
    {
        public UpdateOneMessage() : base()
        {
            msg.command = "updateone";
        }
        public T item { get; set; }
        public string collectionname { get; set; }
        public T result { get; set; }

    }
}
