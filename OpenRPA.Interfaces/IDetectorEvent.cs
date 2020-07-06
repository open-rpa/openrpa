using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IDetectorEvent
    {
        IElement element { get; set; }
        string host { get; set; }
        string fqdn { get; set; }
        // TokenUser user { get; set; }
    }
}
