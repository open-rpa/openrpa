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
    [System.ComponentModel.Designer(typeof(WaitForFileStatusDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(WaitForFileStatus), "Resources.toolbox.waitforfilestatus.png")]
    [LocalizedToolboxTooltip("activity_waitforfilestatus_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_waitforfilestatus", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_waitforfilestatus_helpurl", typeof(Resources.strings))]
    public class WaitForFileStatus : CodeActivity
    {
        [RequiredArgument]
        public InArgument<TimeSpan> Timeout { get; set; }
        [RequiredArgument]
        public InArgument<string> Key { get; set; }
        [RequiredArgument]
        public InArgument<string> Fileurl { get; set; }
        [RequiredArgument]
        public InArgument<string> Status { get; set; } = "exported";
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var timeout = Timeout.Get(context);
            if (Timeout == null || Timeout.Expression == null) timeout = TimeSpan.FromSeconds(10);
            var key = Key.Get(context);
            var fileurl = Fileurl.Get(context);
            var desiredstatus = Status.Get(context).ToLower();
            string status = "";
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                var res = SimpleRequests.GET(fileurl, key);
                var o = JObject.Parse(res);
                status = o["status"].ToString();
                if (status != desiredstatus) System.Threading.Thread.Sleep(1000);
            } while (status != desiredstatus && sw.Elapsed < timeout);
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