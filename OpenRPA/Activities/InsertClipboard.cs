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

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(InsertClipboardDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.insertclipboard.png")]
    [ToolboxTooltip("Insert new content into the users CLipboard buffer")]
    public class InsertClipboard : CodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_ignoreerrors", typeof(Resources.strings)), LocalizedDescription("activity_ignoreerrors_help", typeof(Resources.strings))]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        [RequiredArgument, LocalizedDisplayName("activity_text", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public InArgument<string> Text { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string text = Text.Get(context);
            try
            {
                System.Threading.Thread staThread = new System.Threading.Thread(() =>
                {
                    System.Windows.Clipboard.SetDataObject(text, true);
                });
                staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }
            catch (Exception ex)
            {
                if (IgnoreErrors.Get(context))
                {
                    Log.Debug(ex.Message);
                    return;
                }
                throw;
            }
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public new string DisplayName
        {
            get
            {
                return base.DisplayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}