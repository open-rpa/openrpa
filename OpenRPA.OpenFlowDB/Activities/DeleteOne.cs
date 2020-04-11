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
    [Designer(typeof(DeleteOneDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.deleteentity.png")]
    [LocalizedToolboxTooltip("activity_deleteone_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_deleteone", typeof(Resources.strings))]
    public class DeleteOne : AsyncTaskCodeActivity
    {
        [RequiredArgument]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        [RequiredArgument, OverloadGroup("Item")]
        public InArgument<object> Item { get; set; }
        [RequiredArgument, OverloadGroup("Id")]
        public InArgument<string> _id { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var ignoreErrors = IgnoreErrors.Get(context);
            var collection = Collection.Get(context);
            if (string.IsNullOrEmpty(collection)) collection = "entities";
            JObject result = null;
            var o = Item.Get(context);
            string id = null;
            if(o != null)
            {
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
                var tempid = result.GetValue("_id");
                if (tempid != null) id = tempid.ToString();
                if (string.IsNullOrEmpty(id)) throw new Exception("object has no _id field");
            }
            else
            {
                id = _id.Get(context);
            }
            await global.webSocketClient.DeleteOne(collection, id);
            System.Windows.Forms.Application.DoEvents();
            return true;
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
