using OpenRPA.Interfaces;
using Microsoft.CodeAnalysis.FlowAnalysis;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Activities;
using System.ComponentModel;
using System.Linq;

namespace OpenRPA.Selenium
{
    [System.ComponentModel.Designer(typeof(OpenUrlDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(OpenUrl), "Resources.toolbox.default.png")]
    [LocalizedDisplayName("activity_openurl", typeof(Resources.strings))]
    public class OpenUrl : NativeActivity
    {
        [RequiredArgument, Category("Input")]
        public InArgument<IWebDriver> WebDriver { get; set; }

        [RequiredArgument, Category("Input")]
        public InArgument<string> Url { get; set; }
        [Category("Options")]
        public bool WaitForReady { get; set; }

        public OpenUrl()
        {
            WebDriver = new InArgument<IWebDriver>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IWebDriver>("BrowserDriver")
            };
            WaitForReady = false;
        }

        protected override void Execute(NativeActivityContext context)
        {
            var webDriver= context.GetValue(WebDriver);
            var windowHandles = webDriver.WindowHandles;
            if(windowHandles != null && windowHandles.Count() > 1)
            {
                var newestTabCreated = windowHandles.First();
                webDriver.SwitchTo().Window(newestTabCreated);
            }

            var url = context.GetValue(Url);
            webDriver.Navigate().GoToUrl(url);
            if (WaitForReady)
            {
                var wait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(30));
                wait.Until((x) =>
                {
                    return ((IJavaScriptExecutor)webDriver).ExecuteScript("return document.readyState").Equals(obj: "complete");
                });
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