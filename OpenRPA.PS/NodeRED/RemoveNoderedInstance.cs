using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace OpenRPA.PS
{
    [Cmdlet(VerbsCommon.Remove, "NoderedInstance")]
    public class RemoveNoderedInstance : OpenRPACmdlet
    {
        [Parameter(Position = 0, ValueFromPipeline = true)] public string _id { get; set; }
        protected override async Task ProcessRecordAsync()
        {
            try
            {
                string traceId = ""; string spanId = "";
                if (string.IsNullOrEmpty(_id)) { _id = global.webSocketClient.user._id; }
                await global.webSocketClient.DeleteNoderedInstance(_id, "", "");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
            }


        }
    }
}
