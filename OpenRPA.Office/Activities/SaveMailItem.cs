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
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
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
            if(!string.IsNullOrEmpty(folder))
            {
                filename = System.IO.Path.Combine(folder, RemoveInvalidChars(email.Subject));
                switch(astype)
                {
                    case "4":
                        filename += ".doc"; break;
                    case "5":
                        filename += ".html"; break;
                    case "8":
                        filename += ".ics"; break;
                    case "10":
                        filename += ".mht"; break;
                    case "3":
                        filename += ".msg"; break;
                    case "9":
                        filename += ".msg"; break;
                    case "1":
                        filename += ".rtf"; break;
                    case "2":
                        filename += ".oft"; break;
                    case "0":
                        filename += ".txt"; break;
                    case "7":
                        filename += ".vcs"; break;
                    case "6":
                        filename += ".vcf"; break;
                }
            }
            email.mailItem.SaveAs(filename, astype); // olMSGUnicode / msg : 9    olMSG / msg : 3
            if (!string.IsNullOrEmpty(folder))
            {
                Filename.Set(context, filename);
            }
            
        }
    }
}
