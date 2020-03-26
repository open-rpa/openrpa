using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRPA.NamedPipeWrapper
{
    [Serializable]
    public class PipeMessage
    {
        private static Random rnd = new Random();
        private static int messagecounter = rnd.Next(100, 9000);
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
        // public string error { get; set; }
        public object error { get; set; }
    }
}
