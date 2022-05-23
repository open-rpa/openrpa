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
    [Designer(typeof(SetTextDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(SetText), "Resources.toolbox.settext.png")]
    [LocalizedToolboxTooltip("activity_settext_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_settext", typeof(Resources.strings))]
    public class SetText : CodeActivity
    {
        [LocalizedDisplayName("activity_settext_timeout", typeof(Resources.strings)), LocalizedDescription("activity_settext_timeout_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Timeout { get; set; }
        [LocalizedDisplayName("activity_settext_waitforkeyboard", typeof(Resources.strings)), LocalizedDescription("activity_settext_waitforkeyboard_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForKeyboard { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_settext_field", typeof(Resources.strings)), LocalizedDescription("activity_settext_field_help", typeof(Resources.strings))]
        public InArgument<int> Field { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_settext_text", typeof(Resources.strings)), LocalizedDescription("activity_settext_text_help", typeof(Resources.strings))]
        public InArgument<string> Text { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            int field = Field.Get(context);
            string text = Text.Get(context);
            var timeout = Timeout.Get(context);
            if (Timeout == null || Timeout.Expression == null) timeout = TimeSpan.FromSeconds(3);
            var vars = context.DataContext.GetProperties();
            Interfaces.VT.ITerminalSession session = null;
            foreach (dynamic v in vars)
            {
                var val = v.GetValue(context.DataContext);
                if (val is Interfaces.VT.ITerminalSession _session)
                {
                    session = val;
                }
            }
            if (session == null) throw new ArgumentException("Failed locating terminal session");
            if (session.Terminal == null) throw new ArgumentException("Terminal Sessoin not initialized");
            if (!session.Terminal.IsConnected) throw new ArgumentException("Terminal Sessoin not connected");
            Interfaces.VT.IField _f = null;
            var sw = new Stopwatch();
            sw.Start();

            do
            {
                _f = session.Terminal.GetField(field);
                if (sw.Elapsed > timeout) throw new Exception("Failed locating field #" + field);
            } while (_f == null || (_f.Location.Column == 0 && _f.Location.Row == 0));
            if (_f == null) throw new ArgumentException("Field not found");
            GenericTools.RunUI(session.Refresh);
            if (WaitForKeyboard.Get(context))
            {
                session.Terminal.WaitForKeyboardUnlocked(timeout);
            }
            session.Terminal.SendText(field, text);
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