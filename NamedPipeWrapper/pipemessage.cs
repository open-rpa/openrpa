using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NamedPipeWrapper
{
    [Serializable]
    public class PipeMessage
    {
        private static int messagecounter = 0;
        public PipeMessage()
        {
            ++messagecounter;
            messageid = messagecounter.ToString();
        }
        public PipeMessage(PipeMessage message)
        {
            messageid = message.messageid;
        }
        public string messageid { get; set; }
        public string error { get; set; }
    }
}
