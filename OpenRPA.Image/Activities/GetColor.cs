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
using System.Runtime.InteropServices;

namespace OpenRPA.Image
{
    using System;
    using System.Runtime.InteropServices;

    [Designer(typeof(GetColorDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetColor), "Resources.toolbox.getcolor.png")]
    [LocalizedToolboxTooltip("activity_getcolor_tooltip" , typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getcolor", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_getcolor_helpurl", typeof(Resources.strings))]
    public class GetColor : CodeActivity
    {
        public GetColor()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
        }
        [RequiredArgument, LocalizedDisplayName("activity_offsetx", typeof(Resources.strings)), LocalizedDescription("activity_offsetx_help", typeof(Resources.strings))]
        public InArgument<int> OffsetX { get; set; } = 5;
        [RequiredArgument, LocalizedDisplayName("activity_offsety", typeof(Resources.strings)), LocalizedDescription("activity_offsety_help", typeof(Resources.strings))]
        public InArgument<int> OffsetY { get; set; } = 5;
        [LocalizedDisplayName("activity_element", typeof(Resources.strings)), LocalizedDescription("activity_element_help", typeof(Resources.strings))]
        public InArgument<IElement> Element { get; set; }
        public OutArgument<System.Drawing.Color> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var el = Element.Get(context);
            // if (el == null) throw new ArgumentException("element cannot be null");
            var x = OffsetX.Get(context);
            var y = OffsetY.Get(context);
            if (el != null) { x += el.Rectangle.X; y += el.Rectangle.Y; }
            Result.Set(context, GetPixelColor(x, y));

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


        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
        static public System.Drawing.Color GetPixelColor(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            var color = System.Drawing.Color.FromArgb((int)(pixel & 0x000000FF),
                         (int)(pixel & 0x0000FF00) >> 8,
                         (int)(pixel & 0x00FF0000) >> 16);
            return color;
        }
    }

}