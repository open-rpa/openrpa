using OpenRPA.Interfaces;
using OpenQA.Selenium;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace OpenRPA.Selenium.Activities
{
    [System.ComponentModel.Designer(typeof(ElementActionsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.default.png")]
    [LocalizedDisplayName("activity_elementactions", typeof(Resources.strings))]
    [LocalizedToolboxTooltip("activity_elementactions_tooltip", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_elementactions_helpurl", typeof(Resources.strings))]
    public class ElementActions : NativeActivity
    {
        public ElementActions()
        {
            Element = new InArgument<IWebElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IWebElement>("item")
            };
            Action = "Click";
        }

        [RequiredArgument, Category("Input")]
        public InArgument<IWebElement> Element { get; set; }
        [RequiredArgument, Category("Input"), Editor(typeof(SelectActionEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> Action { get; set; }
        [Category("Input")]
        public InArgument<string> Argument { get; set; }
        [Category("Output")]
        public OutArgument<string> Result { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            var element = Element.Get(context);
            if (element == null)
            {
                throw new Exception("Element is null");
            }
            try
            {
                var action = Action.Get(context);
                var argument = Argument.Get(context);
                var result = string.Empty;
                switch (action)
                {
                    case "Click": element.Click(); break;
                    case "SendKeys": element.SendKeys(argument); break;
                    case "Submit": element.Submit(); break;
                    case "Clear": element.Clear(); break;
                    case "Text": result = element.Text; break;
                    case "TagName": result = element.TagName; break;
                    case "PropertyValue": result = element.PropertyValue(argument)?.ToString(); break;
                    case "GetAttribute": result = element.GetAttribute(argument); break;
                    case "GetCssValue": result = element.GetCssValue(argument); break;
                }
                context.SetValue(Result, result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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

        class SelectActionEditor : CustomSelectEditor
        {
            public override DataTable options
            {
                get
                {
                    var lst = new DataTable();
                    lst.Columns.Add("ID", typeof(string));
                    lst.Columns.Add("TEXT", typeof(string));
                    lst.Rows.Add("Click", "Click");
                    lst.Rows.Add("SendKeys", "SendKeys");
                    lst.Rows.Add("Submit", "Submit");
                    lst.Rows.Add("Clear", "Clear");
                    lst.Rows.Add("Text", "Text");
                    lst.Rows.Add("TagName", "TagName");
                    lst.Rows.Add("PropertyValue", "PropertyValue");
                    lst.Rows.Add("GetAttribute", "GetAttribute");
                    lst.Rows.Add("GetCssValue", "GetCssValue");
                    return lst;
                }
            }
        }

    }
}