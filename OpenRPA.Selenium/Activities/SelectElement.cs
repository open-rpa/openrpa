using OpenRPA.Interfaces;
using OpenQA.Selenium;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace OpenRPA.Selenium
{
    [System.ComponentModel.Designer(typeof(SelectElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(SelectElement), "Resources.toolbox.default.png")]
    [LocalizedDisplayName("activity_selectelement", typeof(Resources.strings))]
    public class SelectElement : NativeActivity
    {
        public SelectElement()
        {
            Element = new InArgument<IWebElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IWebElement>("item")
            };
            ChooseBy = "Value";
        }

        [RequiredArgument, Category("Input")]
        public InArgument<IWebElement> Element { get; set; }

        [Editor(typeof(SelectByEditor), typeof(ExtendedPropertyValueEditor))]
        [RequiredArgument, Category("Input")]
        public InArgument<string> ChooseBy { get; set; }

        [RequiredArgument, Category("Input")]
        public InArgument<string> Value { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            var element = Element.Get(context);
            if (element == null)
            {
                throw new Exception("Element is null");
            }

            try
            {
                var chooseBy = ChooseBy.Get(context);
                var value = Value.Get(context);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(element);
                switch (chooseBy)
                {
                    case "Value": selectElement.SelectByValue(value); break;
                    case "Text": selectElement.SelectByText(value); break;
                    case "Index":
                        if (int.TryParse(value, out int index))
                        {
                            selectElement.SelectByIndex(index);
                        } break;
                    default: break;
                }
            }
            catch (Exception ex) 
            {
                throw new Exception($"Error selecting element: {ex.Message}", ex);
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

        class SelectByEditor : CustomSelectEditor
        {
            public override DataTable options
            {

                get
                {
                    var lst = new DataTable();
                    lst.Columns.Add("ID", typeof(string));
                    lst.Columns.Add("TEXT", typeof(string));
                    lst.Rows.Add("Value", "Value");
                    lst.Rows.Add("Text", "Text");
                    lst.Rows.Add("Index", "Index");
                    return lst;
                }
            }
        }
    }
}