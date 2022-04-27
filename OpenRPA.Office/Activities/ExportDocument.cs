using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
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
    [System.ComponentModel.Designer(typeof(ExportDocumentDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.ExportDocument.png")]
    [LocalizedToolboxTooltip("activity_exportdocument_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_exportdocument", typeof(Resources.strings))]
    public class ExportDocument : CodeActivity
    {
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> ReadPassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> WritePassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<bool> RemoveReadPassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<bool> RemoveWritePassword { get; set; }




        // [RequiredArgument]
        [Category("Input")]
        [OverloadGroup("asworkbook")]
        [LocalizedDisplayName("activity_exportdocument_document", typeof(Resources.strings)), LocalizedDescription("activity_exportdocument_document_help", typeof(Resources.strings))]
        public InOutArgument<Microsoft.Office.Interop.Word.Document> Document { get; set; }
        // [RequiredArgument]
        [Category("Input")]
        [OverloadGroup("asfilename")]
        [LocalizedDisplayName("activity_exportdocument_filename", typeof(Resources.strings)), LocalizedDescription("activity_exportdocument_filename_help", typeof(Resources.strings))]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        [Category("Input")]
        [LocalizedDisplayName("activity_exportdocument_savechanges", typeof(Resources.strings)), LocalizedDescription("activity_exportdocument_savechanges_help", typeof(Resources.strings))]
        public InArgument<bool> SaveChanges { get; set; } = true;
        [RequiredArgument]
        [Category("Misc")]
        [Editor(typeof(XlFixedFormatTypeEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> Type { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var readPassword = ReadPassword.Get(context);
            if (string.IsNullOrEmpty(readPassword)) readPassword = null;
            var writePassword = WritePassword.Get(context);
            if (string.IsNullOrEmpty(writePassword)) writePassword = null;
            var removeReadPassword = RemoveReadPassword.Get(context);
            var removeWritePassword = RemoveWritePassword.Get(context);

            var formattype = Type.Get(context);
            var document = Document.Get(context);
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            var saveChanges = SaveChanges.Get(context);

            Application activeObject = null;
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
            object missing = System.Type.Missing;
            object ofilename = filename;
            if (document == null) document = activeObject.Documents.Open(ref ofilename, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);
            if (document != null)
            {
                if (string.IsNullOrEmpty(filename)) filename = document.FullName;
                officewrap.application.DisplayAlerts = false;
                if (!string.IsNullOrEmpty(readPassword)) { document.Password = readPassword; saveChanges = true; }
                if (removeReadPassword) { document.Password = ""; saveChanges = true; }
                if (!string.IsNullOrEmpty(writePassword)) { document.WritePassword = writePassword; saveChanges = true; }
                if (removeWritePassword) { document.WritePassword = ""; saveChanges = true; }
                var ext = System.IO.Path.GetExtension(filename);
                if (ext.ToLower() != ".xps" && formattype == "1")
                {
                    filename = System.IO.Path.ChangeExtension(filename, "xps");
                }
                else if (ext.ToLower() != ".pdf" && formattype == "0")
                {
                    filename = System.IO.Path.ChangeExtension(filename, "pdf");
                }
                if (System.IO.File.Exists(filename))
                {
                    System.IO.File.Delete(filename);
                }
                if (formattype == "1")
                {
                    document.ExportAsFixedFormat(filename, WdExportFormat.wdExportFormatXPS);
                }
                else
                {
                    document.ExportAsFixedFormat(filename, WdExportFormat.wdExportFormatPDF);
                }
                document.Close(saveChanges);
                officewrap.application.DisplayAlerts = true;
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