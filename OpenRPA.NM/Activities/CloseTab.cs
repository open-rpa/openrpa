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
    [System.ComponentModel.Designer(typeof(CloseTabDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(CloseTab), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [LocalizedToolboxTooltip("activity_closetab_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_closetab", typeof(Resources.strings))]
    public class CloseTab : NativeActivity
    {
        public InArgument<string> Browser { get; set; }
        public InArgument<bool> CloseAll { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var closeall = CloseAll.Get(context);
            var browser = Browser.Get(context);
            var timeout = TimeSpan.FromSeconds(3);
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            NMHook.enumtabs();
            if (closeall)
            {
                foreach(var tab in NMHook.tabs.Where(x=> x.browser == browser).ToList())
                {
                    NMHook.closetab(browser, tab.id);
                }
            } else
            {
                if (browser == "chrome")
                {
                    if (NMHook.CurrentChromeTab != null)
                    {
                        NMHook.closetab(browser, NMHook.CurrentChromeTab.id);
                    }
                    else { Log.Warning("No active tab found for " + browser); }
                }
                if (browser == "ff")
                {
                    if (NMHook.CurrentFFTab != null)
                    {
                        NMHook.closetab(browser, NMHook.CurrentFFTab.id);
                    }
                    else { Log.Warning("No active tab found for " + browser); }
                }
                if (browser == "edge")
                {
                    if (NMHook.CurrentEdgeTab != null)
                    {
                        NMHook.closetab(browser, NMHook.CurrentEdgeTab.id);
                    }
                    else { Log.Warning("No active tab found for " + browser); }
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