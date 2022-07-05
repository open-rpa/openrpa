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
    [Designer(typeof(GetTextDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetText), "Resources.toolbox.gettext.png")]
    [LocalizedToolboxTooltip("activity_gettext_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_gettext", typeof(Resources.strings))]
    public class GetText : CodeActivity
    {
        [LocalizedDisplayName("activity_gettext_timeout", typeof(Resources.strings)), LocalizedDescription("activity_gettext_timeout_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Timeout { get; set; }
        [LocalizedDisplayName("activity_gettext_waitforkeyboard", typeof(Resources.strings)), LocalizedDescription("activity_gettext_waitforkeyboard_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForKeyboard { get; set; }
        [RequiredArgument, OverloadGroup("By Field"), LocalizedDisplayName("activity_gettext_field", typeof(Resources.strings)), LocalizedDescription("activity_gettext_field_help", typeof(Resources.strings))]
        public InArgument<int> Field { get; set; }
        [RequiredArgument, OverloadGroup("By String"), LocalizedDisplayName("activity_gettext_string", typeof(Resources.strings)), LocalizedDescription("activity_gettext_string_help", typeof(Resources.strings))]
        public InArgument<int> String { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_gettext_result", typeof(Resources.strings)), LocalizedDescription("activity_gettext_result_help", typeof(Resources.strings))]
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            int field = Field.Get(context);
            int _string = String.Get(context);
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
                if(field > -1) _f = session.Terminal.GetField(field);
                if (_string > -1) _f = session.Terminal.GetString(_string);
                if(_f == null)
                {
                    System.Threading.Thread.Sleep(250);
                    session.Refresh();
                }
                if (sw.Elapsed > timeout) throw new Exception("Failed locating field #" + field);
            } while (_f == null || (_f.Location.Column == 0 && _f.Location.Row == 0));
            if (_f == null) throw new ArgumentException("Field not found");
            if (WaitForKeyboard.Get(context))
            {
                session.Terminal.WaitForKeyboardUnlocked(timeout);
            }
            Result.Set(context, _f.Text);
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