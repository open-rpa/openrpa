using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
namespace OpenRPA.PS
{
    [Cmdlet(VerbsCommon.Remove, "AutoLogin")]
    public class RemoveAutologin : Cmdlet
    {
        protected override void ProcessRecord()
        {
            NativeMethods.LsaPrivateData.RemoveAutologin();
        }

    }
}
