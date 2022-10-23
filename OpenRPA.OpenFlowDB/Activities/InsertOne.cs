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
    [System.ComponentModel.Designer(typeof(InsertOneDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.insertone.png")]
    [LocalizedToolboxTooltip("activity_insertone_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_insertone", typeof(Resources.strings))]
    public class InsertOne : AsyncTaskCodeActivity<JObject>
    {
        [RequiredArgument]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        public InArgument<string> Type { get; set; }
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        [RequiredArgument]
        public InArgument<Object> Item { get; set; }
        public InArgument<string> EncryptFields { get; set; }
        protected async override Task<JObject> ExecuteAsync(AsyncCodeActivityContext context)
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
            JObject result = null;
            var o = Item.Get(context);
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
            var id = result.GetValue("_id");
            if (id != null)
            {
                var _id = id.ToString();
                result = await global.webSocketClient.UpdateOne(collection, 1, false, result, traceId, spanId);
            }
            else
            {
                result = await global.webSocketClient.InsertOne(collection, 1, false, result, traceId, spanId);
            }
            System.Windows.Forms.Application.DoEvents();
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
