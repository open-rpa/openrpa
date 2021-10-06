using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    [System.ComponentModel.Designer(typeof(GetTabDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetTab), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [LocalizedToolboxTooltip("activity_gettab_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_gettab", typeof(Resources.strings))]
    public class GetTab : NativeActivity
    {
        public InArgument<string> Browser { get; set; }
        [RequiredArgument]
        public OutArgument<NativeMessagingMessageTab> Result { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            if (!NMHook.connected)
            {
                Result.Set(context, null);
                return;
            }
            var browser = Browser.Get(context);
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            if (browser == "edge")
            {
                Result.Set(context, NMHook.CurrentEdgeTab);
            }
            else if (browser == "ff")
            {
                Result.Set(context, NMHook.CurrentFFTab);
            }
            else
            {
                Result.Set(context, NMHook.CurrentChromeTab);
            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
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