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
    [System.ComponentModel.Designer(typeof(DetectorDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.detector.png")]
    [ToolboxTooltip(Text = "Puts workflow in idle mode, waiting on selected detector to trigger")]
    public class Detector : NativeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_detector", typeof(Resources.strings)), LocalizedDescription("activity_detector_help", typeof(Resources.strings))]
        public string detector { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark("detector_" + detector, new BookmarkCallback(OnBookmarkCallback));
        }
        void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            // keep bookmark, we want to support being triggerede multiple times, so bookmark needs to be keep incase workflow is restarted
            // context.RemoveBookmark(bookmark.Name);
        }
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public new string DisplayName
        {
            get
            {
                return base.DisplayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}