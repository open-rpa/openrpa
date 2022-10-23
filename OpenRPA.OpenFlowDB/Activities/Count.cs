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
using System.Data;

namespace OpenRPA.OpenFlowDB
{
    [System.ComponentModel.Designer(typeof(CountDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.count.png")]
    [LocalizedToolboxTooltip("activity_count_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_count", typeof(Resources.strings))]
    public class Count : AsyncTaskCodeActivity<int>
    {
        public InArgument<string> QueryString { get; set; }
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        protected async override Task<int> ExecuteAsync(AsyncCodeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            var instance = global.OpenRPAClient.GetWorkflowInstanceByInstanceId(WorkflowInstanceId);
            string traceId = instance?.TraceId; string spanId = instance?.SpanId;
            var collection = Collection.Get(context);
            var querystring = QueryString.Get(context);
            if (string.IsNullOrEmpty(collection)) collection = "entities";
            int result = -1;
            result = await global.webSocketClient.Count(collection, querystring, null, traceId, spanId);
            System.Windows.Forms.Application.DoEvents();
            return result;
        }
        protected override int PostExecute(AsyncCodeActivityContext context, int result)
        {
            return result;
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
