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
    [Cmdlet(VerbsCommon.Add, "File")]
    public class AddFile : OpenRPACmdlet
    {
        [Parameter(Position = 1, Mandatory = true)]
        public string Filename { get; set; }
        public string VirtualPath { get; set; }
        protected override async Task ProcessRecordAsync()
        {
            try
            {
                string traceId = ""; string spanId = "";
                string Path = this.SessionState.Path.CurrentFileSystemLocation.Path;
                string id = await global.webSocketClient.UploadFile(System.IO.Path.Combine(Path, Filename), VirtualPath, null, traceId, spanId);
                var entities = await global.webSocketClient.Query<JObject>("files", "{\"_id\": \"" + id + "\"}", null, 1, 0, null, null, traceId, spanId);
                var entity = entities[0].Value<JObject>("metadata");
                entity["__pscollection"] = "files";
                var obj = entity.toPSObject();
                obj.TypeNames.Insert(0, "OpenRPA.PS.Entity");
                WriteObject(obj);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
            }
            Filename = null;
        }
    }
}
