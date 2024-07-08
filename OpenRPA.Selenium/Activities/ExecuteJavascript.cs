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
    [System.ComponentModel.Designer(typeof(ExecuteJavascriptDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ExecuteJavascript), "Resources.toolbox.default.png")]
    [LocalizedDisplayName("activity_executejavascript", typeof(Resources.strings))]
    public class ExecuteJavascript : NativeActivity
    {
        public ExecuteJavascript()
        {
            WebDriver = new InArgument<IWebDriver>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IWebDriver>("BrowserDriver")
            };
            Element = new InArgument<IWebElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IWebElement>("item")
            };
            Executetype = "Executor";
        }

        [Category("Input")]
        public InArgument<IWebElement> Element { get; set; }

        [RequiredArgument, Category("Input")]
        public InArgument<IWebDriver> WebDriver { get; set; }

        [Editor(typeof(SelectByEditor), typeof(ExtendedPropertyValueEditor))]
        [RequiredArgument, Category("Input")]
        public InArgument<string> Executetype { get; set; }

        [RequiredArgument, Category("Input")]
        public InArgument<string> Script { get; set; }

        [Category("Output")]
        public OutArgument<object> Result { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            var webDriver = context.GetValue(WebDriver);
            if (webDriver == null)
            {
                throw new Exception("webDriver is null");
            }

            try
            {
                var element = Element.Get(context);
                var executeType = Executetype.Get(context);
                var script = Script.Get(context);
                IJavaScriptExecutor js = (IJavaScriptExecutor)webDriver;
                object executorResult;
                switch (executeType)
                {
                    case "Executor":
                        executorResult = element == null ? js.ExecuteScript(script) : js.ExecuteScript(script, element);
                        Result.Set(context, executorResult); break;
                    case "ExecuteAsync":
                        executorResult = element == null ? js.ExecuteAsyncScript(script) : js.ExecuteAsyncScript(script, element);
                        Result.Set(context, executorResult); break;
                    default: break;
                }
            }
            catch (Exception ex) 
            {
                throw new Exception($"Error execute element: {ex.Message}", ex);
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
                    lst.Rows.Add("Executor", "Executor");
                    lst.Rows.Add("ExecuteAsync", "ExecuteAsync");
                    return lst;
                }
            }
        }
    }
}