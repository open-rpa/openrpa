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
    [System.ComponentModel.Designer(typeof(NewMailItemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.newemail.png")]
    [LocalizedToolboxTooltip("activity_newmailitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_newmailitem", typeof(Resources.strings))]
    public class NewMailItem : CodeActivity
    {
        [RequiredArgument]
        [System.ComponentModel.Category("Misc")]
        [Editor(typeof(SelectNewEmailOptionsEditor), typeof(System.Activities.Presentation.PropertyEditing.ExtendedPropertyValueEditor))]
        public InArgument<string> UIAction { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<string> To { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string> CC { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string> BCC { get; set; }
        [RequiredArgument]
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
            Microsoft.Office.Interop.Outlook.MailItem email = (Microsoft.Office.Interop.Outlook.MailItem)outlookApplication.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);

            var to = To.Get(context);
            var cc = CC.Get(context);
            var bcc = BCC.Get(context);
            var subject = Subject.Get(context);
            string body = (Body != null ?Body.Get(context) : null);
            string htmlbody = (HTMLBody != null ? HTMLBody.Get(context) : null);
            if (!string.IsNullOrEmpty(htmlbody)) body = htmlbody;
            var attachments = Attachments.Get(context);
            var uiaction = UIAction.Get(context);

            email.BodyFormat = Microsoft.Office.Interop.Outlook.OlBodyFormat.olFormatRichText;
            email.To = to;
            email.Subject = subject;
            email.HTMLBody = body;
            email.CC = cc;
            email.BCC = bcc;

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
    class SelectNewEmailOptionsEditor : CustomSelectEditor
    {
        public override DataTable options
        {
            get
            {
                DataTable lst = new DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("SendHidden", "Send");
                //lst.Rows.Add("SendVisable", "Show and send");
                lst.Rows.Add("Show", "Show and wait");
                return lst;
            }
        }
    }
    class olSaveAsTypeEditor : CustomSelectEditor
    {
        public override DataTable options
        {
            get
            {
                DataTable lst = new DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("4", "Microsoft Office Word format (.doc)");
                lst.Rows.Add("5", "HTML format (.html)");
                lst.Rows.Add("8", "iCal format (.ics)");
                lst.Rows.Add("10", "MIME HTML format (.mht)");
                lst.Rows.Add("3", "Outlook message format (.msg)");
                lst.Rows.Add("9", "Outlook Unicode message format (.msg)");
                lst.Rows.Add("1", "Rich Text format (.rtf)");
                lst.Rows.Add("2", "Microsoft Outlook template (.oft)");
                lst.Rows.Add("0", "Text format (.txt)");
                lst.Rows.Add("7", "VCal format (.vcs)");
                lst.Rows.Add("6", "VCard format (.vcf)");
                return lst;
            }
        }
    }
}