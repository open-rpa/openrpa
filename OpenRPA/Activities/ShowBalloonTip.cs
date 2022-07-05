using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Data;

namespace OpenRPA.Activities
{
    [Designer(typeof(ShowNotificationDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.showballontip.png")]
    [LocalizedToolboxTooltip("activity_showballoontip_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_showballoontip", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_showballoontip_helpurl", typeof(Resources.strings))]
    public class ShowBalloonTip : CodeActivity
    {
        [RequiredArgument, Category("Input")]
        public InArgument<string> Message { get; set; }
        [Category("Input")]
        public InArgument<string> Title { get; set; }
        [Category("Input")]
        public InArgument<TimeSpan> Duration { get; set; }
        [Editor(typeof(SelectNotificationTypeEditor), typeof(ExtendedPropertyValueEditor)), Category("Misc")]
        public InArgument<string> NotificationType { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var message = Message.Get(context);
            var title = Title.Get(context);
            var duration = Duration.Get(context);
            var notificationtype = NotificationType.Get(context);
            var icontype = System.Windows.Forms.ToolTipIcon.None;
            if (notificationtype == "Information") icontype = System.Windows.Forms.ToolTipIcon.Info;
            if (notificationtype == "Warning") icontype = System.Windows.Forms.ToolTipIcon.Warning;
            if (notificationtype == "Error") icontype = System.Windows.Forms.ToolTipIcon.Error;
            // if (duration.TotalMilliseconds < 100) duration = TimeSpan.FromSeconds(5);
            App.notifyIcon.ShowBalloonTip((int)duration.TotalMilliseconds, title, message, icontype);
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
        class SelectNotificationTypeEditor : CustomSelectEditor
        {
            public override DataTable options
            {
                get
                {
                    DataTable lst = new DataTable();
                    lst.Columns.Add("ID", typeof(string));
                    lst.Columns.Add("TEXT", typeof(string));
                    lst.Rows.Add("Information", "Information");
                    lst.Rows.Add("Warning", "Warning");
                    lst.Rows.Add("Error", "Error");
                    lst.Rows.Add("None", "None");
                    return lst;
                }
            }
        }
    }

}