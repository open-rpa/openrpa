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
using System.Activities.Hosting;

namespace OpenRPA.OpenFlowDB
{
    [System.ComponentModel.Designer(typeof(DeleteFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.deletefile.png")]
    [LocalizedToolboxTooltip("activity_deletefile_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_deletefile", typeof(Resources.strings))]
    public class DeleteFile : AsyncTaskCodeActivity
    {
        public InArgument<bool> IgnoreErrors { get; set; }
        [RequiredArgument,OverloadGroupAttribute("Filename")]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument,OverloadGroupAttribute("Id")]
        public InArgument<string> _id { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            var instance = global.OpenRPAClient.GetWorkflowInstanceByInstanceId(WorkflowInstanceId);
            string traceId = instance?.TraceId; string spanId = instance?.SpanId;
            var filename = Filename.Get(context);
            var id = _id.Get(context);
            var ignoreerrors = IgnoreErrors.Get(context);

            var q = "{\"_id\": \"" + id + "\"}";
            if(!string.IsNullOrEmpty(filename)) q = "{\"filename\":\"" + filename + "\"}";
            // await global.webSocketClient.DeleteOne("files", q);
            var rows = await global.webSocketClient.Query<JObject>("fs.files", q);
            if (rows.Length == 0)
            {
                if(!ignoreerrors) throw new Exception("File not found");
                return 42;
            }
            foreach(var row in rows)
            {
                var _id = row["_id"].ToString();
                await global.webSocketClient.DeleteOne("fs.files", _id, traceId, spanId);
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