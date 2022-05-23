using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OpenRPA.TerminalEmulator
{
    [Designer(typeof(WaitForTextDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(WaitForText), "Resources.toolbox.settext.png")]
    [LocalizedToolboxTooltip("activity_waitfortext_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_waitfortext", typeof(Resources.strings))]
    public class WaitForText : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        [LocalizedDisplayName("activity_waitfortext_timeout", typeof(Resources.strings)), LocalizedDescription("activity_waitfortext_timeout_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Timeout { get; set; }
        [LocalizedDisplayName("activity_waitfortext_found", typeof(Resources.strings)), LocalizedDescription("activity_waitfortext_found_help", typeof(Resources.strings))]
        public InArgument<bool> Found { get; set; }
        [LocalizedDisplayName("activity_waitfortext_throw", typeof(Resources.strings)), LocalizedDescription("activity_waitfortext_throw_help", typeof(Resources.strings))]
        public InArgument<bool> Throw { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_waitfortext_text", typeof(Resources.strings)), LocalizedDescription("activity_waitfortext_text_help", typeof(Resources.strings))]
        public InArgument<string> Text { get; set; }
        [Browsable(false)]
        public ActivityAction<bool> Body { get; set; }
        protected override void StartLoop(NativeActivityContext context)
        {
            string text = Text.Get(context);
            var timeout = Timeout.Get(context);
            var _throw = Throw.Get(context);
            if (Timeout == null || Timeout.Expression == null) timeout = TimeSpan.FromSeconds(3);
            var vars = context.DataContext.GetProperties();
            Interfaces.VT.ITerminalSession session = null;
            foreach (dynamic v in vars) {
                var val = v.GetValue(context.DataContext);
                if(val is Interfaces.VT.ITerminalSession _session)
                {
                    session = val;
                }
            }
            if (session == null) throw new ArgumentException("Failed locating terminal session");
            if (session.Terminal == null) throw new ArgumentException("Terminal Sessoin not initialized");
            if (!session.Terminal.IsConnected) throw new ArgumentException("Terminal Sessoin not connected");
            var sw = new Stopwatch();
            sw.Start();
            var result = session.Terminal.WaitForText(text, timeout);
            if (result && Body != null)
            {
                context.ScheduleAction(Body, result, OnBodyComplete);
            }
            else if (_throw)
            {
                throw new ArgumentException("Timeout waiting for text");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {

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
        public Activity Create(System.Windows.DependencyObject target)
        {
            Type t = typeof(WaitForText);
            var wfdesigner = global.OpenRPAClient.CurrentDesigner;
            WFHelper.DynamicAssemblyMonitor(wfdesigner.WorkflowDesigner, t.Assembly.GetName().Name, t.Assembly, true);
            var fef = new WaitForText();
            var aa = new ActivityAction<bool>();
            var da = new DelegateInArgument<bool>();
            da.Name = "item";
            aa.Handler = new System.Activities.Statements.Sequence();
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
    }
}