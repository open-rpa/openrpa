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
    [System.ComponentModel.Designer(typeof(ReplyMailItemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.replymailitem.png")]
    [LocalizedToolboxTooltip("activity_replymailitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_replymailitem", typeof(Resources.strings))]
    public class ReplyMailItem : CodeActivity
    {
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<email> Email { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Misc")]
        [Editor(typeof(SelectNewEmailOptionsEditor), typeof(System.Activities.Presentation.PropertyEditing.ExtendedPropertyValueEditor))]
        public InArgument<string> UIAction { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Misc")]
        [Editor(typeof(SelectReplyEmailOptionsEditor), typeof(System.Activities.Presentation.PropertyEditing.ExtendedPropertyValueEditor))]
        public InArgument<string> ReplyAction { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string> To { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string> CC { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string> BCC { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Subject { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        [OverloadGroup("TextMessage")]
        public InArgument<string> Body { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        [OverloadGroup("HTMLMessage")]
        public InArgument<string> HTMLBody { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string[]> Attachments { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<email> EMail { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            Microsoft.Office.Interop.Outlook.Application outlookApplication = new Microsoft.Office.Interop.Outlook.Application();
            // Microsoft.Office.Interop.Outlook.MailItem email = (Microsoft.Office.Interop.Outlook.MailItem)outlookApplication.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
            var fromemail = Email.Get(context);
            

            var to = To.Get(context);
            var cc = CC.Get(context);
            var bcc = BCC.Get(context);
            var subject = Subject.Get(context);
            string body = (Body != null ?Body.Get(context) : null);
            string htmlbody = (HTMLBody != null ? HTMLBody.Get(context) : null);
            if (!string.IsNullOrEmpty(htmlbody)) body = htmlbody;
            var attachments = Attachments.Get(context);
            var uiaction = UIAction.Get(context);
            var replyaction = ReplyAction.Get(context);
            Microsoft.Office.Interop.Outlook.MailItem email;
            if(replyaction == "forward")
            {
                email = fromemail.mailItem.Forward();
            } else if (replyaction == "reply")
            {
                email = fromemail.mailItem.Reply();
            } else
            {
                email = fromemail.mailItem.ReplyAll();
            }
            email.BodyFormat = Microsoft.Office.Interop.Outlook.OlBodyFormat.olFormatRichText;
            if(!string.IsNullOrEmpty(to)) email.To = to;
            if (!string.IsNullOrEmpty(subject)) email.Subject = subject;
            email.HTMLBody = body + Environment.NewLine + email.HTMLBody;
            if (!string.IsNullOrEmpty(cc)) email.CC = cc;
            if (!string.IsNullOrEmpty(bcc)) email.BCC = bcc;

            if (attachments != null && attachments.Count() > 0)
            {
                foreach (var attachment in attachments)
                {
                    if (!string.IsNullOrEmpty(attachment))
                    {
                        email.Attachments.Add(attachment, Microsoft.Office.Interop.Outlook.OlAttachmentType.olByValue, 100000, Type.Missing);
                    }
                }
            }
            if(uiaction == "Show")
            {
                email.Display(true);
            }
            //else if(uiaction == "SendVisable")
            //{
            //    email.Display(true);
            //    email.Send();
            //}
            else
            {
                email.Send();
            }
            if(EMail!=null)
            {
                EMail.Set(context, new email(email));
            }
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
    class SelectReplyEmailOptionsEditor : CustomSelectEditor
    {
        public override DataTable options
        {
            get
            {
                DataTable lst = new DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("reply", "Reply");
                lst.Rows.Add("replyall", "Reply all");
                lst.Rows.Add("forward", "Forward");
                return lst;
            }
        }
    }
}