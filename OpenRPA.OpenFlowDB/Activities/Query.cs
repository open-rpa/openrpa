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
    [System.ComponentModel.Designer(typeof(QueryDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.query.png")]
    [LocalizedToolboxTooltip("activity_query_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_query", typeof(Resources.strings))]
    public class Query : AsyncTaskCodeActivity<JObject[]>
    {
        [Browsable(false)]
        public InArgument<bool> IgnoreErrors { get; set; }
        public InArgument<string> QueryString { get; set; }
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        public InArgument<string> Projection { get; set; }
        public InArgument<string> Orderby { get; set; }
        public InArgument<int> Top { get; set; }
        public InArgument<int> Skip { get; set; }
        public OutArgument<DataTable> DataTable { get; set; }
        protected async override Task<JObject[]> ExecuteAsync(AsyncCodeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            var instance = global.OpenRPAClient.GetWorkflowInstanceByInstanceId(WorkflowInstanceId);
            string traceId = instance?.TraceId; string spanId = instance?.SpanId;
            var collection = Collection.Get(context);
            var querystring = QueryString.Get(context);
            var projection = Projection.Get(context);
            var top = Top.Get(context);
            var skip = Skip.Get(context);
            if (top < 1) top = 100;
            if (skip < 0) skip = 0;
            var orderby = Orderby.Get(context);
            if (string.IsNullOrEmpty(collection)) collection = "entities";
            JObject[] result = null;
            result = await global.webSocketClient.Query<JObject>(collection, querystring, projection, top, skip, orderby, traceId, spanId);
            System.Windows.Forms.Application.DoEvents();
            return result;
        }
        protected override JObject[] PostExecute(AsyncCodeActivityContext context, JObject[] result)
        {
            if (DataTable != null && DataTable.Expression != null)
            {
                var jarray = new JArray(result);
                DataTable dt = jarray.ToDataTable();
                DataTable.Set(context, dt);
            }
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
