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
using System.Threading;
using Newtonsoft.Json.Linq;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(SaveMailItemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.commentout.png")]
    [LocalizedToolboxTooltip("activity_savemailitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_savemailitem", typeof(Resources.strings))]
    public sealed class SaveMailItem : CodeActivity
    {
        public SaveMailItem()
        {
            Type = "9";
        }
        [RequiredArgument]
        [Category("Input")]
        public InArgument<email> EMail { get; set; }
        [RequiredArgument]
        [Category("Misc")]
        [Editor(typeof(olSaveAsTypeEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> Type { get; set; }
        [Category("Output")]
        public InOutArgument<string> Filename { get; set; }
        [RequiredArgument,Category("Input")]
        public InArgument<string> Folder { get; set; }
        public string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(System.IO.Path.GetInvalidFileNameChars()));
        }
        protected override void Execute(CodeActivityContext context)
        {
            var email = EMail.Get(context);
            var filename = Filename.Get(context);
            var folder = Folder.Get(context);
            var astype = Type.Get(context);
            if (string.IsNullOrEmpty(filename)) filename = email.Subject;
            if (!string.IsNullOrEmpty(folder))
            {
                filename = System.IO.Path.Combine(folder, RemoveInvalidChars(filename));
                string fileextension = ".bin";
                switch (astype)
                {
                    case "4":
                        fileextension = ".doc"; break;
                    case "5":
                        fileextension = ".html"; break;
                    case "8":
                        fileextension = ".ics"; break;
                    case "10":
                        fileextension = ".mht"; break;
                    case "3":
                        fileextension = ".msg"; break;
                    case "9":
                        fileextension = ".msg"; break;
                    case "1":
                        fileextension = ".rtf"; break;
                    case "2":
                        fileextension = ".oft"; break;
                    case "0":
                        fileextension = ".txt"; break;
                    case "7":
                        fileextension = ".vcs"; break;
                    case "6":
                        fileextension = ".vcf"; break;
                }
                if (System.IO.Path.GetExtension(filename.ToLower()) != fileextension) filename = filename += fileextension;
            }
            email.mailItem.SaveAs(filename, astype); // olMSGUnicode / msg : 9    olMSG / msg : 3
            if (!string.IsNullOrEmpty(folder))
            {
                Filename.Set(context, filename);
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
}
