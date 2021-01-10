using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class PushMetricsMessage : SocketCommand
    {
        public PushMetricsMessage() : base()
        {
            msg.command = "pushmetrics";
        }
        public string metrics { get; set; }
    }
}
