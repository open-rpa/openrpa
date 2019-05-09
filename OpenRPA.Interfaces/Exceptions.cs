using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    [Serializable()]
    public class ElementNotFoundException : Exception
    {
        public ElementNotFoundException(string message)
            : base(message)
        {
        }
        protected ElementNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

    }
}
