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
using FlaUI.Core.AutomationElements;

namespace OpenRPA.Windows
{
    [Designer(typeof(CloseWindowDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(CloseWindowDesigner), "Resources.toolbox.closewindow.png")]
    [LocalizedToolboxTooltip("activity_closewindow_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_closewindow", typeof(Resources.strings))]
    public class CloseWindow : CodeActivity
    {
        public CloseWindow()
        {
            Element = new InArgument<UIElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<UIElement>("item")
            };
        }
        [RequiredArgument, LocalizedDisplayName("activity_element", typeof(Resources.strings)), LocalizedDescription("activity_element_help", typeof(Resources.strings))]
        public InArgument<UIElement> Element { get; set; }
        public InArgument<bool> IgnoreErrors { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var ignore = IgnoreErrors.Get(context);
            var el = Element.Get(context);
            try
            {
                if (el == null) throw new ArgumentException("element cannot be null");
                if (el.ProcessId == System.Diagnostics.Process.GetCurrentProcess().Id) return;
                var window = el.RawElement.AsWindow();
                window.Close();
            }
            catch (Exception)
            {
                if(!ignore) throw;
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