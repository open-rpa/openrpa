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

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(DeleteOneDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.deleteentity.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class DeleteOne : AsyncTaskCodeActivity<bool>
    {
        [RequiredArgument]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        [RequiredArgument, OverloadGroupAttribute("Item")]
        public InArgument<Object> Item { get; set; }
        [RequiredArgument, OverloadGroupAttribute("Id")]
        public InArgument<string> _id { get; set; }
        protected async override Task<bool> ExecuteAsync(AsyncCodeActivityContext context)
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
    }
}
