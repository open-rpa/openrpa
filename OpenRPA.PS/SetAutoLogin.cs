using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
namespace OpenRPA.PS
{
    [Cmdlet(VerbsCommon.Set, "AutoLogin")]
    public class SetAutoLogin : Cmdlet
    {
        [Parameter(Mandatory = true)] public string Username { get; set; }
        [Parameter(Mandatory = true)] public string Password { get; set; }
        [Parameter()] public int LogonCount { get; set; }
        protected override void ProcessRecord()
        {
            NativeMethods.LsaPrivateData.SetAutologin(Username, Password, LogonCount);
        }

    }
}
