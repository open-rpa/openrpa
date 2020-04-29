using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(MoveMailItemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.newemail.png")]
    [LocalizedToolboxTooltip("activity_movemailitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_movemailitem", typeof(Resources.strings))]
    public class MoveMailItem : CodeActivity
    {
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<email> Mail { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Folder { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            Microsoft.Office.Interop.Outlook.Application outlookApplication = new Microsoft.Office.Interop.Outlook.Application();
            Microsoft.Office.Interop.Outlook.MailItem email = (Microsoft.Office.Interop.Outlook.MailItem)outlookApplication.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);

            var mail = Mail.Get(context);
            var folder = Folder.Get(context);
            mail.Move(folder);

        }
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