using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NamedPipeWrapper
{
    [Serializable()]
    public class NamedPipeException : Exception
    {
        public NamedPipeException(string message)
            : base(message)
        {
        }
        protected NamedPipeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

    }
}
