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
    [Designer(typeof(SendKeyDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(SendKey), "Resources.toolbox.settext.png")]
    [LocalizedToolboxTooltip("activity_sendkey_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_sendkey", typeof(Resources.strings))]
    public class SendKey : CodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_sendkey_key", typeof(Resources.strings)), LocalizedDescription("activity_sendkey_key_help", typeof(Resources.strings))]
        public InArgument<string> Key { get; set; }
        [LocalizedDisplayName("activity_sendkey_timeout", typeof(Resources.strings)), LocalizedDescription("activity_sendkey_timeout_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Timeout { get; set; }
        [LocalizedDisplayName("activity_sendkey_waitforkeyboard", typeof(Resources.strings)), LocalizedDescription("activity_sendkey_waitforkeyboard_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForKeyboard { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string key = Key.Get(context);
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
            GenericTools.RunUI(session.Refresh);
            if (WaitForKeyboard.Get(context))
            {
                session.Terminal.WaitForKeyboardUnlocked(timeout);
            }
            System.Windows.Input.Key _key;
            if (Enum.TryParse(key, true, out _key))
            {
                session.Terminal.SendKey(_key);
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