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
    [LocalizedToolboxTooltip("activity_insertclipboard_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_insertclipboard", typeof(Resources.strings))]
    public class InsertClipboard : CodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_ignoreerrors", typeof(Resources.strings)), LocalizedDescription("activity_ignoreerrors_help", typeof(Resources.strings))]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        [RequiredArgument, OverloadGroup("Text"), LocalizedDisplayName("activity_text", typeof(Resources.strings)), LocalizedDescription("activity_text_help", typeof(Resources.strings))]
        public InArgument<string> Text { get; set; }
        [RequiredArgument, OverloadGroup("BitmapImage"), LocalizedDisplayName("activity_image", typeof(Resources.strings)), LocalizedDescription("activity_image_help", typeof(Resources.strings))]
        public InArgument<System.Drawing.Bitmap> Image { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string text = Text.Get(context);
            System.Drawing.Bitmap image = Image.Get(context);
            try
            {
                System.Threading.Thread staThread = new System.Threading.Thread(() =>
                {
                    if (!string.IsNullOrEmpty(text)) System.Windows.Clipboard.SetDataObject(text, true);
                    if (image != null) System.Windows.Clipboard.SetDataObject(image, true);
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