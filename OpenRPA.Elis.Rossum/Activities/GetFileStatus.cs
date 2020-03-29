using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Elis.Rossum
{
    [System.ComponentModel.Designer(typeof(GetFileStatusDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetFileStatus), "Resources.toolbox.getimage.png")]
    [LocalizedToolboxTooltip("activity_getfilestatus_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getfilestatus", typeof(Resources.strings))]
    public class GetFileStatus : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Key { get; set; }
        [RequiredArgument]
        public InArgument<string> Fileurl { get; set; }
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var key = Key.Get(context);
            var fileurl = Fileurl.Get(context);
            var res = SimpleRequests.GET(fileurl, key);
            var o = JObject.Parse(res);
            string status = o["status"].ToString();
            context.SetValue(Result, status);
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