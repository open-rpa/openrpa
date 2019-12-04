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
using Newtonsoft.Json.Linq;

namespace OpenRPA.OpenFlowDB
{
    [System.ComponentModel.Designer(typeof(RemovePermissionDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.addentitypermission.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class RemovePermission : CodeActivity
    {
        [RequiredArgument]
        public InArgument<Object> Item { get; set; }
        [RequiredArgument]
        public OutArgument<JObject> Result { get; set; }
        [RequiredArgument]
        public InArgument<string> EntityId { get; set; }
        public InArgument<string> Name { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
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

            var _id = EntityId.Get(context);
            var name = Name.Get(context);
            JArray _acl = (JArray)result.GetValue("_acl");
            List<OpenRPA.Interfaces.entity.ace> acl = null;
            if(_acl!=null) { acl =  _acl.ToObject<List<OpenRPA.Interfaces.entity.ace>>();  } else { acl = new List<OpenRPA.Interfaces.entity.ace>(); }
            
            var aces = acl.Where(x => x._id == _id).ToList();
            foreach(var ace in aces)
            {
                acl.Remove(ace);
            }
            result["_acl"] = JArray.FromObject(acl);

            context.SetValue(Result, result);
        }
    }
}