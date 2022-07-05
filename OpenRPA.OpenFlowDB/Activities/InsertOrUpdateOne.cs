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
    [System.ComponentModel.Designer(typeof(InsertOrUpdateOneDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.insertorupdateone.png")]
    [LocalizedToolboxTooltip("activity_insertorupdateone_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_insertorupdateone", typeof(Resources.strings))]
    public class InsertOrUpdateOne : AsyncTaskCodeActivity<JObject>
    {
        [RequiredArgument]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        [Browsable(false)] // ups, spelling error
        public InArgument<string> Uniqeness { get; set; }
        public InArgument<string> Uniqueness { get; set; }
        public InArgument<string> Type { get; set; }
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        [RequiredArgument]
        public InArgument<Object> Item { get; set; }
        public InArgument<string> EncryptFields { get; set; }
        protected async override Task<JObject> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var ignoreErrors = IgnoreErrors.Get(context);
            var encrypt = EncryptFields.Get(context);
            if (encrypt == null) encrypt = "";
            var collection = Collection.Get(context);
            if (string.IsNullOrEmpty(collection)) collection = "entities";
            var type = Type.Get(context);
            var uniqueness = Uniqueness.Get(context);
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
            result = await global.webSocketClient.InsertOrUpdateOne(collection, 1, false, uniqueness, result);
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
