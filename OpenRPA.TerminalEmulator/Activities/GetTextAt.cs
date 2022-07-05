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
    [Designer(typeof(GetTextAtDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetTextAt), "Resources.toolbox.gettextat.png")]
    [LocalizedToolboxTooltip("activity_gettextatat_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_gettextatat", typeof(Resources.strings))]
    public class GetTextAt : CodeActivity
    {
        [LocalizedDisplayName("activity_gettextat_timeout", typeof(Resources.strings)), LocalizedDescription("activity_gettextat_timeout_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Timeout { get; set; }
        [LocalizedDisplayName("activity_gettextat_waitforkeyboard", typeof(Resources.strings)), LocalizedDescription("activity_gettextat_waitforkeyboard_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForKeyboard { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_gettextat_row", typeof(Resources.strings)), LocalizedDescription("activity_gettextat_row_help", typeof(Resources.strings))]
        public InArgument<int> Row { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_gettextat_column", typeof(Resources.strings)), LocalizedDescription("activity_gettextat_column_help", typeof(Resources.strings))]
        public InArgument<int> Column { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_gettextat_length", typeof(Resources.strings)), LocalizedDescription("activity_gettextat_length_help", typeof(Resources.strings))]
        public InArgument<int> Length { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_gettextat_result", typeof(Resources.strings)), LocalizedDescription("activity_gettextat_result_help", typeof(Resources.strings))]
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            int row = Row.Get(context);
            int column = Column.Get(context);
            int length = Length.Get(context);
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
            if (WaitForKeyboard.Get(context))
            {
                session.Terminal.WaitForKeyboardUnlocked(timeout);
            }
            var result = session.Terminal.GetTextAt(column, row, length);
            Result.Set(context, result);
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