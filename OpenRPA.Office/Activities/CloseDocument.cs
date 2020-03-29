using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Word;
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(CloseDocumentDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.closeworkbook.png")]
    [LocalizedToolboxTooltip("activity_closedocument_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_closedocument", typeof(Resources.strings))]
    public class CloseDocument : CodeActivity
    {
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<bool> SaveChanges { get; set; } = true;
        protected override void Execute(CodeActivityContext context)
        {
            var saveChanges = SaveChanges.Get(context);
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            Application activeObject = null;
            object missing = Type.Missing;
            Document document = null;
            try
            {
                activeObject = (Application)Marshal.GetActiveObject("Word.Application");
                if (!string.IsNullOrEmpty(filename))
                {
                    foreach (Document current in activeObject.Documents)
                    {
                        if (current.FullName == filename)
                        {
                            document = current;
                            document.Close(saveChanges);
                            break;
                        }
                    }
                }
            }
            catch
            {
                activeObject = null;
            }
            finally
            {
                if (activeObject == null) activeObject = (Application)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("000209FF-0000-0000-C000-000000000046")));
                activeObject.Visible = true;
            }
            int doccount = 0;
            foreach (Document current in activeObject.Documents)
            {
                if (current.FullName == filename)
                {
                    doccount++;
                    break;
                }
            }
            if(doccount==0 || string.IsNullOrEmpty(filename))
            {
                activeObject.Quit(saveChanges);
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