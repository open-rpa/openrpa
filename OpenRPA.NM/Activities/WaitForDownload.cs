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

namespace OpenRPA.NM
{
    [System.ComponentModel.Designer(typeof(WaitForDownloadDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(WaitForDownload), "Resources.toolbox.waitfordownload.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_waitfordownload_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_waitfordownload", typeof(Resources.strings))]
    public class WaitForDownload : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        [System.ComponentModel.Browsable(false)]
        public ActivityAction Body { get; set; }
        public OutArgument<DetectorEvent> Event { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }
        private System.Timers.Timer timer = null;
        private IWorkflowInstance wfApp = null;
        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark("DownloadDetectorPlugin", new BookmarkCallback(OnBookmarkCallback));
            var timeout = Timeout.Get(context);
            if(timeout.TotalMilliseconds> 0)
            {
                wfApp = Plugin.client.WorkflowInstances.Where(x => x.InstanceId == context.WorkflowInstanceId.ToString()).FirstOrDefault();
                if (wfApp == null) throw new Exception("Fail locating current workflow instance, to create timeout activity");
                timer = new System.Timers.Timer(timeout.TotalMilliseconds);
                timer.Elapsed += (sender, e) =>
                {
                    try
                    {
                        wfApp.ResumeBookmark("DownloadDetectorPlugin", null, false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                };
                timer.Start();
            }
            context.ScheduleAction(Body, null, null);
        }
        void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            Event.Set(context, obj);
            try
            {
                if(timer!=null)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public Activity Create(DependencyObject target)
        {
            var fef = new WaitForDownload();
            var aa = new ActivityAction();
            fef.Body = aa;
            return fef;
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