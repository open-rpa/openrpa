using OpenRPA.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace OpenRPA.Selenium
{
    [System.ComponentModel.Designer(typeof(SwitchToDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(SwitchTo), "Resources.toolbox.default.png")]
    [LocalizedDisplayName("activity_SwitchTo", typeof(Resources.strings))]
    public class SwitchTo : NativeActivity
    {
        [RequiredArgument, Category("Input/Output")]
        public InArgument<IWebDriver> WebDriver { get; set; }

        [RequiredArgument, Category("Input")]
        [Editor(typeof(SelectSwitchToEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> Action { get; set; }
        [Category("Input")]
        public InArgument<object> Argument { get; set; }

        [Category("Output")]
        public InArgument<IWebElement> Element { get; set; }
        [Category("Output")]
        public InArgument<IAlert> Alert { get; set; }

        public SwitchTo()
        {
            WebDriver = new InArgument<IWebDriver>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IWebDriver>("BrowserDriver")
            };
            Action = "Frame";
        }

        protected override void Execute(NativeActivityContext context)
        {
            var webDriver = context.GetValue(WebDriver);
            var action = Action.Get(context);
            var argument = Argument.Get(context);

            switch (action)
            {
                case "Frame":
                    if (argument is string) { webDriver.SwitchTo().Frame(argument.ToString()); }
                    else if (argument is int) { webDriver.SwitchTo().Frame((int)argument); }
                    else if (argument is IWebElement) { webDriver.SwitchTo().Frame((IWebElement)argument); };
                    break;
                case "ParentFrame":
                    webDriver.SwitchTo().ParentFrame(); 
                    break;
                case "Window": 
                    webDriver.SwitchTo().Window(argument.ToString()); 
                    break;
                case "NewWindow":
                    var windowType = (WindowType)Enum.Parse(typeof(WindowType), argument.ToString());
                    webDriver.SwitchTo().NewWindow(windowType); 
                    break;
                case "DefaultContent": 
                    webDriver.SwitchTo().DefaultContent();
                    break;
                case "ActiveElement": 
                    context.SetValue(Element, webDriver.SwitchTo().ActiveElement()); 
                    break;
                case "Alert": 
                    context.SetValue(Element, webDriver.SwitchTo().Alert()); 
                    break;
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
        class SelectSwitchToEditor : CustomSelectEditor
        {
            public override DataTable options
            {

                get
                {
                    var lst = new DataTable();
                    lst.Columns.Add("ID", typeof(string));
                    lst.Columns.Add("TEXT", typeof(string));
                    lst.Rows.Add("Frame", "Frame");
                    lst.Rows.Add("ParentFrame", "ParentFrame");
                    lst.Rows.Add("Window", "Window");
                    lst.Rows.Add("NewWindow", "NewWindow");
                    lst.Rows.Add("DefaultContent", "DefaultContent");
                    lst.Rows.Add("ActiveElement", "ActiveElement");
                    lst.Rows.Add("Alert", "Alert");
                    return lst;
                }
            }
        }
    }
}