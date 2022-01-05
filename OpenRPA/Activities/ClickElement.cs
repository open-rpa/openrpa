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
    [LocalizedToolboxTooltip("activity_clickelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_clickelement", typeof(Resources.strings))]
    public class ClickElement : CodeActivity
    {
        public ClickElement()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
            VirtualClick = Config.local.use_virtual_click;
            AnimateMouse = Config.local.use_animate_mouse;
            PostWait = Config.local.use_postwait;
        }
        [RequiredArgument, LocalizedDisplayName("activity_animatemouse", typeof(Resources.strings)), LocalizedDescription("activity_animatemouse_help", typeof(Resources.strings))]
        public InArgument<bool> AnimateMouse { get; set; } = false;
        [RequiredArgument, LocalizedDisplayName("activity_mousebutton", typeof(Resources.strings)), LocalizedDescription("activity_mousebutton_help", typeof(Resources.strings))]
        //[Editor(typeof(SelectButtonEditor), typeof(PropertyValueEditor))]
        public InArgument<int> Button { get; set; } = (int)Input.MouseButton.Left;
        [RequiredArgument, LocalizedDisplayName("activity_offsetx", typeof(Resources.strings)), LocalizedDescription("activity_offsetx_help", typeof(Resources.strings))]
        public InArgument<int> OffsetX { get; set; } = 5;
        [RequiredArgument, LocalizedDisplayName("activity_offsety", typeof(Resources.strings)), LocalizedDescription("activity_offsety_help", typeof(Resources.strings))]
        public InArgument<int> OffsetY { get; set; } = 5;
        [RequiredArgument, LocalizedDisplayName("activity_element", typeof(Resources.strings)), LocalizedDescription("activity_element_help", typeof(Resources.strings))]
        public InArgument<IElement> Element { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_doubleclick", typeof(Resources.strings)), LocalizedDescription("activity_doubleclick_help", typeof(Resources.strings))]
        public InArgument<bool> DoubleClick { get; set; } = false;
        [LocalizedDisplayName("activity_virtualclick", typeof(Resources.strings)), LocalizedDescription("activity_virtualclick_help", typeof(Resources.strings))]
        public InArgument<bool> VirtualClick { get; set; } = true;
        [LocalizedDisplayName("activity_postwait", typeof(Resources.strings)), LocalizedDescription("activity_postwait_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> PostWait { get; set; }
        [Editor(typeof(KeyModifiersOptionsEditor), typeof(System.Activities.Presentation.PropertyEditing.ExtendedPropertyValueEditor))]
        [LocalizedDisplayName("activity_keymodifiers", typeof(Resources.strings)), LocalizedDescription("activity_keymodifiers_help", typeof(Resources.strings))]
        public InArgument<string> KeyModifiers { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var el = Element.Get(context);
            if (el == null) throw new ArgumentException("element cannot be null");
            var doubleclick = false;
            if (DoubleClick != null) doubleclick = DoubleClick.Get(context);
            var button = Button.Get(context);
            var virtualClick = false;
            if (VirtualClick != null) virtualClick = VirtualClick.Get(context);
            var animatemouse = false;
            if (AnimateMouse != null) animatemouse = AnimateMouse.Get(context);
            var keymodifiers = "";
            if (KeyModifiers != null) keymodifiers = KeyModifiers.Get(context);

            var disposes = new List<IDisposable>();
            var keys = TypeText.GetKeys(keymodifiers);
            foreach (var vk in keys) disposes.Add(FlaUI.Core.Input.Keyboard.Pressing(vk));

            var _button = (Input.MouseButton)button;
            el.Click(virtualClick, _button, OffsetX.Get(context), OffsetY.Get(context), doubleclick, animatemouse);
            disposes.ForEach(x => { x.Dispose(); });
            TimeSpan postwait = TimeSpan.Zero;
            if (PostWait != null) { postwait = PostWait.Get(context); }
            if (postwait != TimeSpan.Zero)
            {
                System.Threading.Thread.Sleep(postwait);
                // FlaUI.Core.Input.Wait.UntilInputIsProcessed();
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
    class KeyModifiersOptionsEditor : CustomSelectEditor
    {
        public override System.Data.DataTable options
        {
            get
            {
                var lst = new System.Data.DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("{LCONTROL}", "Left Control");
                lst.Rows.Add("{LMENU}", "Left Menu\\Alt");
                lst.Rows.Add("{LSHIFT}", "Left Shift");
                return lst;
            }
        }

    }
}