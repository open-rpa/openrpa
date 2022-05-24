using FlaUI.Core.AutomationElements;
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

namespace OpenRPA.TerminalEmulator
{
    [System.ComponentModel.Designer(typeof(TerminalSessionDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(TerminalSession), "Resources.toolbox.terminalsession.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_terminalsession_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_terminalsession", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_terminalsession_helpurl", typeof(Resources.strings))]
    public class TerminalSession : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        [LocalizedDisplayName("activity_terminalsession_timeout", typeof(Resources.strings)), LocalizedDescription("activity_terminalsession_timeout_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Timeout { get; set; }
        [RequiredArgument]
        [LocalizedDisplayName("activity_terminalsession_hostname", typeof(Resources.strings)), LocalizedDescription("activity_terminalsession_hostname_help", typeof(Resources.strings))]
        public InArgument<string> Hostname { get; set; }
        [RequiredArgument]
        [LocalizedDisplayName("activity_terminalsession_termtype", typeof(Resources.strings)), LocalizedDescription("activity_terminalsession_termtype_help", typeof(Resources.strings))]
        public InArgument<string> TermType { get; set; }
        [RequiredArgument]
        [LocalizedDisplayName("activity_terminalsession_port", typeof(Resources.strings)), LocalizedDescription("activity_terminalsession_port_help", typeof(Resources.strings))]
        public InArgument<int> Port { get; set; }
        [LocalizedDisplayName("activity_terminalsession_hideui", typeof(Resources.strings)), LocalizedDescription("activity_terminalsession_hideui_help", typeof(Resources.strings))]
        public InArgument<bool> HideUI { get; set; }
        [Browsable(false)]
        public InArgument<bool> UseSSL { get; set; }
        [Browsable(false)]
        public ActivityAction<Interfaces.VT.ITerminalSession> Body { get; set; }
        private readonly Variable<Interfaces.VT.ITerminalSession> _elements = new Variable<Interfaces.VT.ITerminalSession>("_elements");
        protected override void StartLoop(NativeActivityContext context)
        {
            Interfaces.VT.ITerminalSession session = null;
            var timeout = Timeout.Get(context);
            if (Timeout == null || Timeout.Expression == null) timeout = TimeSpan.FromSeconds(3);
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            GenericTools.RunUI(() =>
            {
                session = new TerminalRecorder() { WorkflowInstanceId = WorkflowInstanceId };
                RunPlugin.Sessions.Add(session);
                context.SetValue(_elements, session);
                session.Config = new termOpen3270Config();
                session.Config.Hostname = Hostname.Get(context);
                session.Config.TermType = TermType.Get(context);
                session.Config.Port = Port.Get(context);
                if(!HideUI.Get(context)) session.Show();
                session.Connect();
            });
            var sw = new Stopwatch();
            sw.Start();
            while (!session.Terminal.IsConnected && sw.Elapsed < timeout)
            {
                System.Threading.Thread.Sleep(50);
            }
            Log.Output(string.Format("Connected to {0} in {1:mm\\:ss\\.fff}", session.Config.Hostname, sw.Elapsed));
            if (!session.Terminal.IsConnected)
            {
                throw new Exception("Timeout connecting to " + Hostname.Get(context) + ":" + Port.Get(context));
            }
            session.Refresh();
            context.ScheduleAction(Body, session, OnBodyComplete);
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            var session = _elements.Get(context);
            if (session == null) return;
            session.Disconnect();
            GenericTools.RunUI(session.Close);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            //Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            Type t = typeof(TerminalSession);
            var wfdesigner = global.OpenRPAClient.CurrentDesigner;
            WFHelper.DynamicAssemblyMonitor(wfdesigner.WorkflowDesigner, t.Assembly.GetName().Name, t.Assembly, true);
            var fef = new TerminalSession();
            // fef.Variables = new System.Collections.ObjectModel.Collection<Variable>();
            var aa = new ActivityAction<Interfaces.VT.ITerminalSession>();
            var da = new DelegateInArgument<Interfaces.VT.ITerminalSession>();
            da.Name = "_session";
            aa.Handler = new System.Activities.Statements.Sequence();
            fef.Body = aa;
            aa.Argument = da;
            return fef;
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