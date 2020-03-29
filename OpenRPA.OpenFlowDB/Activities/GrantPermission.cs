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
    [System.ComponentModel.Designer(typeof(GrantPermissionDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.addentitypermission.png")]
    [LocalizedToolboxTooltip("activity_grantpermission_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_grantpermission", typeof(Resources.strings))]
    public class GrantPermission : CodeActivity
    {
        [RequiredArgument]
        public InArgument<Object> Item { get; set; }
        [RequiredArgument]
        public OutArgument<JObject> Result { get; set; }
        public InArgument<bool> Read { get; set; }
        public InArgument<bool> Update { get; set; }
        public InArgument<bool> Delete { get; set; }
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
            var read = Read.Get(context);
            var update = Update.Get(context);
            var delete = Delete.Get(context);
            JArray _acl = (JArray)result.GetValue("_acl");
            //var acl = _acl?.Value<entity.ace[]>().ToList();

            List<OpenRPA.Interfaces.entity.ace> acl = null;
            if(_acl!=null) { acl =  _acl.ToObject<List<OpenRPA.Interfaces.entity.ace>>();  } else { acl = new List<OpenRPA.Interfaces.entity.ace>(); }
            
            OpenRPA.Interfaces.entity.ace ace = acl.Where(x => x._id == _id).FirstOrDefault();
            if (ace == null)
            {
                ace = new OpenRPA.Interfaces.entity.ace();
                ace.unsetBit(1);
                ace.unsetBit(2);
                ace.unsetBit(3);
                ace.unsetBit(4);
                ace.unsetBit(5);
                ace.unsetBit(6);
                ace._id = _id; ace.name = name;
                ace.deny = false;
                acl.Add(ace);
            }
            //ace.setBit(1); // create
            if (Read.Expression != null) { if (read) ace.setBit(2); else ace.unsetBit(2); }
            if (Update.Expression != null) { if (update) ace.setBit(3); else ace.unsetBit(3); }
            if (Delete.Expression != null) { if (delete) ace.setBit(4); else ace.unsetBit(4); }
            //if (delete) ace.setBit(5); else ace.unsetBit(5); //invoke
            result["_acl"] = JArray.FromObject(acl);

            context.SetValue(Result, result);
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