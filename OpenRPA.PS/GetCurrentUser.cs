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
        protected override async Task ProcessRecordAsync()
        {
            WriteObject(global.webSocketClient.user);
        }
    }
}
