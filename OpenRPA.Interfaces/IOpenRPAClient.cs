using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public delegate void SignedinEventHandler(entity.TokenUser user);
    public delegate void DisconnectedEventHandler();
    public interface IOpenRPAClient
    {
        event SignedinEventHandler Signedin;
        event DisconnectedEventHandler Disconnected;
    }
}
