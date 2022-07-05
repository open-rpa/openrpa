using OpenRPA.Image.PrintCapture;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace OpenRPA.Image
{
    [Designer(typeof(PrintWindowDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [ToolboxBitmap(typeof(GetImage), "Resources.toolbox.printwindow.png")]
    [LocalizedToolboxTooltip("activity_printwindow_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_printwindow", typeof(Resources.strings))]
    public sealed class PrintWindow : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> WindowName { get; set; }

        [RequiredArgument]
        public OutArgument<ImageElement> Result { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            string windowName = context.GetValue(WindowName);

            var pch = new PrintCaptureHelper();
            if (!pch.Init(windowName))
            {
                throw new ArgumentException("Window is not found");
            }

            pch.Capture();
            Bitmap image = System.Drawing.Image.FromHbitmap(pch.BitmapPtr);
            var rect = new Rectangle(pch.WindowRect.Top, pch.WindowRect.Left, pch.WindowRect.Width, pch.WindowRect.Height);
            pch.Cleanup();

            context.SetValue(Result, new ImageElement(rect, image));
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
