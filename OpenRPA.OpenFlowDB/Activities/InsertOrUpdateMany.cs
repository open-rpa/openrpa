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
    [System.ComponentModel.Designer(typeof(InsertOrUpdateManyDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.insertorupdatemany.png")]
    [LocalizedToolboxTooltip("activity_insertorupdatemany_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_insertorupdatemany", typeof(Resources.strings))]
    public class InsertOrUpdateMany : AsyncTaskCodeActivity<JObject[]>
    {
        [RequiredArgument]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        public InArgument<string> Uniqueness { get; set; }
        public InArgument<string> Type { get; set; }
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        [RequiredArgument]
        public InArgument<object[]> Items { get; set; }
        public InArgument<bool> SkipResult { get; set; }
        public InArgument<string> EncryptFields { get; set; }
        protected async override Task<JObject[]> ExecuteAsync(AsyncCodeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            var instance = global.OpenRPAClient.GetWorkflowInstanceByInstanceId(WorkflowInstanceId);
            string traceId = instance?.TraceId; string spanId = instance?.SpanId;
            var ignoreErrors = IgnoreErrors.Get(context);
            var encrypt = EncryptFields.Get(context);
            if (encrypt == null) encrypt = "";
            var collection = Collection.Get(context);
            if (string.IsNullOrEmpty(collection)) collection = "entities";
            var type = Type.Get(context);
            var uniqueness = Uniqueness.Get(context);
            // JObject[] result = null;
            var results = new List<JObject>();
            var items = Items.Get(context);
            foreach(var o in items)
            {
                JObject result = null;
                if (o.GetType() != typeof(JObject))
                {
                    var t = Task.Factory.StartNew(() =>
                    {
                        result = JObject.FromObject(o);
                    });
                    t.Wait();
                }
                else
                {
                    result = (JObject)o;
                }

                if (!string.IsNullOrEmpty(encrypt))
                {
                    result["_encrypt"] = encrypt;
                }
                var name = result.GetValue("name", StringComparison.OrdinalIgnoreCase)?.Value<string>();
                result["name"] = name;
                if (!string.IsNullOrEmpty(type))
                {
                    result["_type"] = type;
                }
                results.Add(result);
            }

            var _result = await global.webSocketClient.InsertOrUpdateMany(collection, 1, false, uniqueness, SkipResult.Get(context), results.ToArray(), traceId, spanId);
            System.Windows.Forms.Application.DoEvents();
            return _result;
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
