using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
namespace OpenRPA.PS
{
    [Cmdlet(VerbsCommon.Get, "CurrentUser")]
    public class GetCurrentUser : OpenRPACmdlet
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task ProcessRecordAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            WriteObject(global.webSocketClient.user);
        }
    }
}
