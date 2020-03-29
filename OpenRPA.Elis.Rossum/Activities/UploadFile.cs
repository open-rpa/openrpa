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
    [System.ComponentModel.Designer(typeof(UploadFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(UploadFile), "Resources.toolbox.getimage.png")]
    [LocalizedToolboxTooltip("activity_uploadfile_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_uploadfile", typeof(Resources.strings))]
    public class UploadFile : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Key { get; set; }
        [RequiredArgument]
        public InArgument<string> Queue { get; set; }
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var key = Key.Get(context);
            var queue = Queue.Get(context);
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            var res = SimpleRequests.HttpUploadFile(queue + "/upload", filename, key, "content", "image/jpeg");
            var o = JObject.Parse(res);
            var fileurl = o["results"][0]["annotation"].ToString();
            context.SetValue(Result, fileurl);
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