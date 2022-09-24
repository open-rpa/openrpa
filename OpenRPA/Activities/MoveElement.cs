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
    [Designer(typeof(MoveElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.moveelement.png")]
    [LocalizedToolboxTooltip("activity_moveelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_moveelement", typeof(Resources.strings))]
    public class MoveElement : CodeActivity
    {
        public MoveElement()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
        }
        [RequiredArgument, LocalizedDisplayName("activity_element", typeof(Resources.strings)), LocalizedDescription("activity_element_help", typeof(Resources.strings))]
        public InArgument<IElement> Element { get; set; }

        [RequiredArgument, LocalizedDisplayName("activity_screen", typeof(Resources.strings)), LocalizedDescription("activity_screen_help", typeof(Resources.strings))]
        public InArgument<int> Screen { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_x", typeof(Resources.strings)), LocalizedDescription("activity_x_help", typeof(Resources.strings))]
        public InArgument<int> X { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_y", typeof(Resources.strings)), LocalizedDescription("activity_y_help", typeof(Resources.strings))]
        public InArgument<int> Y { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_width", typeof(Resources.strings)), LocalizedDescription("activity_width_help", typeof(Resources.strings))]
        public InArgument<int> Width { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_height", typeof(Resources.strings)), LocalizedDescription("activity_height_help", typeof(Resources.strings))]
        public InArgument<int> Height { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_animatemove", typeof(Resources.strings)), LocalizedDescription("activity_animatemove_help", typeof(Resources.strings))]
        public InArgument<bool> AnimateMove { get; set; } = false;

        [LocalizedDisplayName("activity_windowstate", typeof(Resources.strings)), LocalizedDescription("activity_windowstate_help", typeof(Resources.strings)),
            Editor(typeof(SelectWindowStateEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> WindowState { get; set; }


        protected override void Execute(CodeActivityContext context)
        {
            var el = Element.Get(context);
            if (el == null) throw new ArgumentException("element cannot be null");
            var screen = Screen.Get(context);
            var x = X.Get(context);
            var y = Y.Get(context);
            var width = Width.Get(context);
            var height = Height.Get(context);
            var animatemove = AnimateMove.Get(context);
            var windowstate = WindowState.Get(context);
            if (width <= 30 || height <= 10)
            {
            }
            else
            {
                var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                if (screen < 0)
                {
                    Log.Warning("screen cannot be below 0, using screen 0");
                    screen = 0;
                }
                if (screen >= allScreens.Count)
                {
                    Log.Warning("screen " + screen + " does not exists, using " + (allScreens.Count - 1) + " instead");
                    screen = allScreens.Count - 1;
                }
                x += allScreens[screen].WorkingArea.X;
                y += allScreens[screen].WorkingArea.Y;
                if (el.RawElement is FlaUI.Core.AutomationElements.AutomationElement fla)
                {
                    var _window = ((UIElement)el).GetWindow< FlaUI.Core.AutomationElements.Window>();
                    var window = new UIElement(_window);
                    if (_window.Properties.NativeWindowHandle.IsSupported)
                    {
                        GenericTools.Restore(_window.Properties.NativeWindowHandle.Value);
                    }
                    if (animatemove) window.MoveWindowTo(x, y, width, height);
                    if (!animatemove)
                    {
                        window.SetWindowSize(width, height);
                        window.SetWindowPosition(x, y);
                    }
                    if (!string.IsNullOrEmpty(windowstate) && _window.Properties.NativeWindowHandle.IsSupported)
                    {
                        switch (windowstate)
                        {
                            case "Normal": GenericTools.Restore(_window.Properties.NativeWindowHandle.Value); break;
                            case "Minimized": GenericTools.Minimize(_window.Properties.NativeWindowHandle.Value); break;
                            case "Maximized": GenericTools.Maximized(_window.Properties.NativeWindowHandle.Value); break;
                        }
                    }
                }
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
    class SelectWindowStateEditor : CustomSelectEditor
    {
        public override System.Data.DataTable options
        {
            get
            {
                var lst = new System.Data.DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("", "Nothing");
                lst.Rows.Add("Normal", "Normal");
                lst.Rows.Add("Minimized", "Minimized");
                lst.Rows.Add("Maximized", "Maximized");
                return lst;
            }
        }
    }
}