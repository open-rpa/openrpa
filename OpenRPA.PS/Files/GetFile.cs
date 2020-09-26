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
    [Cmdlet(VerbsCommon.Get, "File")]
    public class GetFile : OpenRPACmdlet
    {
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "byFilename")]
        public string Filename { get; set; }
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "byId")]
        public string Id { get; set; }
        protected override async Task ProcessRecordAsync()
        {
            try
            {
                Uri baseUri = new Uri(global.openflowconfig.baseurl);

                var q = "{\"_id\": \"" + Id + "\"}";
                if (!string.IsNullOrEmpty(Filename)) q = "{\"filename\":\"" + Filename + "\"}";
                var rows = await global.webSocketClient.Query<JObject>("files", q, null, 100, 0, "{\"_id\": -1}");

                if (rows.Length == 0) throw new Exception("File not found");
                Filename = rows[0]["filename"].ToString();
                Id = rows[0]["_id"].ToString();

                string Path = this.SessionState.Path.CurrentFileSystemLocation.Path;

                Uri downloadUri = new Uri(baseUri, "/download/" + Id);
                var url = downloadUri.ToString();
                using (var client = new System.Net.WebClient())
                {
                    // client.Headers.Add("Authorization", "jwt " + global.webSocketClient);
                    client.Headers.Add(System.Net.HttpRequestHeader.Authorization, global.webSocketClient.jwt);

                    Filename = System.IO.Path.GetFileName(Filename);
                    await client.DownloadFileTaskAsync(new Uri(url), System.IO.Path.Combine(Path, Filename));
                    WriteObject(new System.IO.FileInfo(System.IO.Path.Combine(Path, Filename)));
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
            }
            Id = null;
            Filename = null;
        }
    }
}
