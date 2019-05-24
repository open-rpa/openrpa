using FlaUI.Core.AutomationElements.Infrastructure;
using Newtonsoft.Json.Linq;
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
    [System.Drawing.ToolboxBitmap(typeof(Detector), "Resources.toolbox.detector.png")]
    //[designer.ToolboxTooltip(Text = "Puts workflow in idle mode, waiting on selected detector to trigger")]
    public class Detector : NativeActivity
    {
        [RequiredArgument]
        public string detector { get; set; }

        //private AutoResetEvent syncEvent = null;
        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark("detector_" + detector, new BookmarkCallback(OnBookmarkCallback));
        }

        void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            context.RemoveBookmark(bookmark.Name);
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