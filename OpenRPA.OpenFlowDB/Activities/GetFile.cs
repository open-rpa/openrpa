using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace OpenRPA.OpenFlowDB
{
    [System.ComponentModel.Designer(typeof(GetFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.getfile.png")]
    [LocalizedToolboxTooltip("activity_getfile_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getfile", typeof(Resources.strings))]
    public class GetFile : AsyncTaskCodeActivity
    {
        [RequiredArgument,OverloadGroupAttribute("Filename")]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument,OverloadGroupAttribute("Id")]
        public InArgument<string> _id { get; set; }
        [RequiredArgument]
        public InArgument<string> LocalPath { get; set; }
        [RequiredArgument]
        public InArgument<bool> IgnorePath { get; set; } = false;
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            var instance = global.OpenRPAClient.GetWorkflowInstanceByInstanceId(WorkflowInstanceId);
            string traceId = instance?.TraceId; string spanId = instance?.SpanId;
            var filename = Filename.Get(context);
            var id = _id.Get(context);
            var filepath = LocalPath.Get(context);
            var ignorepath = IgnorePath.Get(context);
            // await global.webSocketClient.DownloadFileAndSave(filename, id, filepath, ignorepath);

            Uri baseUri = new Uri(global.openflowconfig.baseurl);
            filepath = Environment.ExpandEnvironmentVariables(filepath);

            var q = "{\"_id\": \"" + id + "\"}";
            if(!string.IsNullOrEmpty(filename)) q = "{\"filename\":\"" + filename + "\"}";
            var rows = await global.webSocketClient.Query<JObject>("files", q, null, 100, 0, "{\"_id\": -1}", traceId: traceId, spanId: spanId);

            if (rows.Length == 0) throw new Exception("File not found");
            filename = rows[0]["filename"].ToString();
            id = rows[0]["_id"].ToString();

            Uri downloadUri = new Uri(baseUri, "/download/" + id);
            var url = downloadUri.ToString();

            // if(string.IsNullOrEmpty(filename)) filename = "temp."
            // if (System.IO.File.Exists(filepath) && !overwrite) return 42;
            using (var client = new System.Net.WebClient())
            {
                // client.Headers.Add("Authorization", "jwt " + global.webSocketClient);
                client.Headers.Add(System.Net.HttpRequestHeader.Authorization, global.webSocketClient.jwt);
                if (!string.IsNullOrEmpty(traceId)) client.Headers.Add("x-trace-id", traceId);
                if (!string.IsNullOrEmpty(spanId)) client.Headers.Add("x-span-id", spanId);

                if (ignorepath) filename = System.IO.Path.GetFileName(filename);
                await client.DownloadFileTaskAsync(new Uri(url), System.IO.Path.Combine(filepath, filename));
            }
            return 42;
        }
        protected override void AfterExecute(AsyncCodeActivityContext context, object result)
        {
        }
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}