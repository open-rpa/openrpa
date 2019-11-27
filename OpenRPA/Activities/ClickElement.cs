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

namespace OpenRPA.Activities
{
    [Designer(typeof(ClickElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.elementclick.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ClickElement : CodeActivity
    {
        public ClickElement()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
            //PostWait = new InArgument<TimeSpan>()
            //{
            //    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromMilliseconds(150)")
            //};
        }
        [RequiredArgument]
        public InArgument<bool> AnimateMouse { get; set; } = false;
        [RequiredArgument]
        //[Editor(typeof(SelectButtonEditor), typeof(PropertyValueEditor))]
        public InArgument<int> Button { get; set; } = (int)Input.MouseButton.Left;
        [RequiredArgument]
        public int OffsetX { get; set; } = 5;
        [RequiredArgument]
        public int OffsetY { get; set; } = 5;
        [RequiredArgument]
        public InArgument<IElement> Element { get; set; }
        [RequiredArgument]
        public InArgument<bool> DoubleClick { get; set; } = false;
        public InArgument<bool> VirtualClick { get; set; } = true;
        public InArgument<TimeSpan> PostWait { get; set; }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetDoubleClickTime();
        protected override void Execute(CodeActivityContext context)
        {
            var el = Element.Get(context);
            if (el == null) throw new ArgumentException("element cannot be null");
            var doubleclick = false;
            if (DoubleClick != null) doubleclick = DoubleClick.Get(context);
            var button = Button.Get(context);
            var virtualClick = false;
            if (VirtualClick != null) virtualClick = VirtualClick.Get(context);
            var _button = (Input.MouseButton)button;
            el.Click(virtualClick, _button, OffsetX, OffsetY, doubleclick);
            TimeSpan postwait = TimeSpan.Zero;
            if (PostWait!=null) { postwait = PostWait.Get(context); }
            if(postwait != TimeSpan.Zero)
            {
                System.Threading.Thread.Sleep(postwait);
            }
            
        }
    }
}