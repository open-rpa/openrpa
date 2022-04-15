using FlaUI.Core.AutomationElements.Infrastructure;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(BreakDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.break.png")]
    [LocalizedToolboxTooltip("activity_break_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_break", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_break_helpurl", typeof(Resources.strings))]
    public class Break : NativeActivity
    {
        protected override void Execute(NativeActivityContext context)
        {
            Bookmark bookmark = (Bookmark)context.Properties.Find("BreakBookmark");
            if (bookmark != null)
            {
                context.ResumeBookmark(bookmark, context.CreateBookmark());
            }
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
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
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}