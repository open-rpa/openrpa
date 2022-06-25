using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    class InsertOrUpdateMany<T> : SocketCommand
    {
        public InsertOrUpdateMany() : base()
        {
            msg.command = "insertorupdatemany";
        }
        // w: 1 - Requests acknowledgment that the write operation has propagated
        // w: 0 - Requests no acknowledgment of the write operation
        // w: 2 would require acknowledgment from the primary and one of the secondaries
        // w: 3 would require acknowledgment from the primary and both secondaries
        public int w { get; set; }
        // true, requests acknowledgment that the mongod instances have written to the on-disk journal
        public bool j { get; set; }
        public T[] items { get; set; }
        public string collectionname { get; set; }
        public string uniqeness { get; set; }        
        public T[] results { get; set; }
        public bool skipresults { get; set; }
    }
}
