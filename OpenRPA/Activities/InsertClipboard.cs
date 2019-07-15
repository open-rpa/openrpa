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
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class InsertClipboard : CodeActivity
    {
        [RequiredArgument]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        [RequiredArgument]
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
    }
}