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
    [System.ComponentModel.Designer(typeof(OpenURLDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(OpenURL), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [LocalizedToolboxTooltip("activity_openurl_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_openurl", typeof(Resources.strings))]
    public class OpenURL : NativeActivity
    {
        [RequiredArgument]
        public InArgument<string> Url { get; set; }
        public InArgument<string> Browser { get; set; }
        public InArgument<bool> NewTab { get; set; }
        public OpenURL()
        {
        }
        protected override void Execute(NativeActivityContext context)
        {
            var url = Url.Get(context);
            var browser = Browser.Get(context);
            var timeout = TimeSpan.FromSeconds(3);
            var newtab = NewTab.Get(context);
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            if (!string.IsNullOrEmpty(url))
            {
                NMHook.enumtabs();
                var tab = NMHook.FindTabByURL(browser, url);
                if (tab != null)
                {
                    if (!tab.highlighted || !tab.selected)
                    {
                        var _tab = NMHook.selecttab(browser, tab.id);
                    }
                }
            }
            NMHook.openurl(browser, url, newtab);
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