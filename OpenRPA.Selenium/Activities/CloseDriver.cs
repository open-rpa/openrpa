using OpenRPA.Interfaces;
using OpenQA.Selenium;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace OpenRPA.Selenium
{
    [System.ComponentModel.Designer(typeof(CloseDriverDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(CloseDriver), "Resources.toolbox.default.png")]
    [LocalizedDisplayName("activity_closedriver", typeof(Resources.strings))]
    public class CloseDriver : NativeActivity
    {
        public CloseDriver()
        {
            WebDriver = new InArgument<IWebDriver>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IWebDriver>("BrowserDriver")
            };
            KillProcess = true;
        }
        [RequiredArgument]
        [Category("Input")]
        public InArgument<IWebDriver> WebDriver { get; set; }
        
        [Category("Options")]
        [LocalizedDisplayName("activity_CloseDriver_KillDriver", typeof(Resources.strings))]
        public bool KillProcess { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            if (KillProcess)
            {
                var currentSessionId = Process.GetCurrentProcess().SessionId;
                var drivers = new[] { "chromedriver", "msedgedriver", "geckodriver" }.SelectMany(Process.GetProcessesByName);
                foreach (var p in drivers)
                {
                    if (p.SessionId == currentSessionId)
                    {
                        p.Kill();
                    }
                }
            }
            else
            {
                var webDriver = context.GetValue(WebDriver);
                webDriver.Quit();
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