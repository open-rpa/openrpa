using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class InsertOneMessage<T> : SocketCommand
    {
        public InsertOneMessage() : base()
        {
            msg.command = "insertone";
        }
        public T item { get; set; }
        public string collectionname { get; set; }
        public T result { get; set; }

    }
}
