using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Word;
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(SetParagraphDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.setparagraph.png")]
    [LocalizedToolboxTooltip("activity_setparagraph_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_setparagraph", typeof(Resources.strings))]
    public class SetParagraph : CodeActivity
    {
        [Category("Input")]
        [LocalizedDisplayName("activity_setparagraph_filename", typeof(Resources.strings)), LocalizedDescription("activity_setparagraph_filename_help", typeof(Resources.strings))]
        public InArgument<string> Filename { get; set; }
        [Category("Input")]
        [LocalizedDisplayName("activity_setparagraph_index", typeof(Resources.strings)), LocalizedDescription("activity_setparagraph_index_help", typeof(Resources.strings))]
        public InArgument<int> Index { get; set; }
        [Category("Input")]
        [LocalizedDisplayName("activity_setparagraph_text", typeof(Resources.strings)), LocalizedDescription("activity_setparagraph_text_help", typeof(Resources.strings))]
        public InArgument<string> Text { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            Application activeObject = null;
            object missing = Type.Missing;
            Document document = null;
            try
            {
                activeObject = (Application)Marshal.GetActiveObject("Word.Application");
                foreach (Document current in activeObject.Documents)
                {
                    if (current.FullName == filename)
                    {
                        document = current;
                        break;
                    }
                }
            }
            catch
            {
                activeObject = null;
            }
            finally
            {
                if(activeObject==null) activeObject = (Application)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("000209FF-0000-0000-C000-000000000046")));
                activeObject.Visible = true;
            }
            object ofilename = filename;
            if(document==null) document = activeObject.Documents.Open(ref ofilename, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);
            var p = document.Content.Paragraphs;
            int index = Index.Get(context);
            if (Index.Expression == null)
            {
                var r = document.Range(0, document.Paragraphs.Last.Range.End);
                r.Text = Text.Get(context); //  + "\r";
            }
            else
            {
                if (p.Count < index) throw new Exception("filename only contains " + p.Count + " Paragraphs");
                p[index].Range.Text = Text.Get(context);
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
