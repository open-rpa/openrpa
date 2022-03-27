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
    [System.Drawing.ToolboxBitmap(typeof(OpenURL), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [LocalizedToolboxTooltip("activity_gettab_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_gettab", typeof(Resources.strings))]
    public class GetTab : NativeActivity
    {
        public InArgument<string> Browser { get; set; }
        [RequiredArgument, OverloadGroup("Result")]
        public OutArgument<NativeMessagingMessageTab> Result { get; set; }
        [RequiredArgument, OverloadGroup("Results")]
        public OutArgument<NativeMessagingMessageTab[]> Results { get; set; }
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
                if(Result != null && !Result.GetIsEmpty())
                {
                    Result.Set(context, NMHook.CurrentEdgeTab);
                } else
                {
                    Results.Set(context,NMHook.tabs.Where(x => x.browser == browser).ToArray());
                }
            }
            else if (browser == "ff")
            {
                if (Result != null && !Result.GetIsEmpty())
                {
                    Result.Set(context, NMHook.CurrentFFTab);
                }
                else
                {
                    Results.Set(context, NMHook.tabs.Where(x => x.browser == browser).ToArray());
                }
            }
            else
            {
                if (Result != null && !Result.GetIsEmpty())
                {
                    Result.Set(context, NMHook.CurrentChromeTab);
                }
                else
                {
                    Results.Set(context, NMHook.tabs.Where(x => x.browser == browser).ToArray());
                }
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