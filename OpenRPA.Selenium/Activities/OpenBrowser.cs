using OpenRPA.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace OpenRPA.Selenium
{
    [System.ComponentModel.Designer(typeof(OpenBrowserDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(OpenBrowser), "Resources.toolbox.default.png")]
    [LocalizedDisplayName("activity_openbrowser", typeof(Resources.strings))]
    public class OpenBrowser : NativeActivity
    {
        public OpenBrowser()
        {
            Body = new ActivityAction<IWebDriver>
            {
                Argument = new DelegateInArgument<IWebDriver>("BrowserDriver"),
            };

            Arguments = new InArgument<string[]>
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<string[]>("{ \"--remote-debugging-port=9222\" }")
            };
            Browser = Browsers.Chrome;
            ProfileMode = ProfileModes.Default;
            OpenOneTab = true;
            Body.Handler = new CloseDriver();
        }
        public enum Browsers
        {
            Chrome,
            Firefox,
            MicrosoftEdge
        }
        public enum ProfileModes
        {
            Default,
            New,
            Custom
        }

        [Browsable(false)]
        public ActivityAction<IWebDriver> Body { get; set; }

        [Category("Input")]
        public Browsers Browser { get; set; }

        [Category("Options")]
        [LocalizedDisplayName("activity_ProfileMode", typeof(Resources.strings))]
        public ProfileModes ProfileMode { get; set; }

        [Category("Options")]
        [LocalizedDisplayName("activity_ProfilePath", typeof(Resources.strings))]
        public InArgument<string> ProfilePath { get; set; }
        [Category("Options")]
        public InArgument<string[]> Arguments { get; set; }
        [Category("Options")]
        public InArgument<int> DebuggerAddress { get; set; }
        [Category("Options")]
        public InArgument<string> DriverPath { get; set; }
        [Category("Options")]
        [LocalizedDisplayName("activity_OpenOneTab", typeof(Resources.strings))]
        public bool OpenOneTab { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            var profilePath = context.GetValue(ProfilePath);
            var port = context.GetValue(DebuggerAddress);
            var driverPath = context.GetValue(DriverPath);

            var arguments = Arguments.Get(context) == null ? new List<string>() : Arguments.Get(context).ToList();
            arguments.Add("--start-maximized");
            arguments.Add("--disable-build-check");

            IWebDriver BrowserDriver = CreateWebDriver(Browser, profilePath, port, driverPath, arguments);

            if (OpenOneTab)
            {
                CloseExtraTabs(BrowserDriver);
            }

            context.ScheduleAction(Body, BrowserDriver, null, null);
        }

        private IWebDriver CreateWebDriver(Browsers browser, string profilePath, int port, string driverPath, List<string> arguments)
        {
            IWebDriver webDriver;
            if (browser == Browsers.Chrome)
            {
                var options = ConfigureChromeOptions(profilePath, port, arguments);
                var driverService = string.IsNullOrEmpty(driverPath) ? ChromeDriverService.CreateDefaultService() : ChromeDriverService.CreateDefaultService(driverPath);
                driverService.HideCommandPromptWindow = true;
                webDriver = new ChromeDriver(driverService, options);
            }
            else if (browser == Browsers.MicrosoftEdge)
            {
                var options = ConfigureEdgeOptions(profilePath, port, arguments);
                var driverService = string.IsNullOrEmpty(driverPath) ? EdgeDriverService.CreateDefaultService() : EdgeDriverService.CreateDefaultService(driverPath);
                driverService.HideCommandPromptWindow = true;
                webDriver = new EdgeDriver(driverService, options);
            }
            else
            {
                var driverService = string.IsNullOrEmpty(driverPath) ? FirefoxDriverService.CreateDefaultService() : FirefoxDriverService.CreateDefaultService(driverPath);
                driverService.HideCommandPromptWindow = true;
                if (port > 0)
                {
                    driverService.BrowserCommunicationPort = port;
                    driverService.ConnectToRunningBrowser = true;
                    webDriver = new FirefoxDriver(driverService);
                }
                else
                {
                    var options = ConfigureFirefoxOptions(profilePath, arguments);
                    webDriver = new FirefoxDriver(driverService, options);
                }
            }
            return webDriver;
        }

        private ChromeOptions ConfigureChromeOptions(string profilePath, int port, List<string> arguments)
        {
            var options = new ChromeOptions();
            if (port > 0)
            {
                options.DebuggerAddress = $"127.0.0.1:{port}";
                return options;
            }
            options.AddExcludedArguments(new List<string> { "enable-automation" });
            options.AddAdditionalChromeOption("useAutomationExtension", false);
            if (ProfileMode == ProfileModes.Default)
            {
                var userProfilePath = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data";
                options.AddArgument("--user-data-dir=" + Environment.ExpandEnvironmentVariables(userProfilePath));
            }
            else if (ProfileMode == ProfileModes.Custom && !string.IsNullOrEmpty(profilePath))
            {
                options.AddArgument("--user-data-dir=" + profilePath);
            }
            options.AddArguments(arguments);
            return options;
        }

        private EdgeOptions ConfigureEdgeOptions(string profilePath, int port, List<string> arguments)
        {
            var options = new EdgeOptions();
            if (port > 0)
            {
                options.DebuggerAddress = $"127.0.0.1:{port}";
                return options;
            }
            options.AddExcludedArguments(new List<string> { "enable-automation" });
            options.AddAdditionalEdgeOption("useAutomationExtension", false);
            if (ProfileMode == ProfileModes.Default)
            {
                var userProfilePath = @"%USERPROFILE%\AppData\Local\Microsoft\Edge\User Data";
                options.AddArgument("--user-data-dir=" + Environment.ExpandEnvironmentVariables(userProfilePath));
            }
            else if (ProfileMode == ProfileModes.Custom && !string.IsNullOrEmpty(profilePath))
            {
                options.AddArgument("--user-data-dir=" + profilePath);
            }
            options.AddArguments(arguments);
            return options;
        }

        private FirefoxOptions ConfigureFirefoxOptions(string profilePath, List<string> arguments)
        {
            var options = new FirefoxOptions();
            if (ProfileMode == ProfileModes.Default)
            {
                var profilesDir = Environment.ExpandEnvironmentVariables("%APPDATA%\\Mozilla\\Firefox\\Profiles");
                var defaultProfilePath = Directory.GetDirectories(profilesDir).First(dir => dir.EndsWith(".default-release"));
                options.Profile = new FirefoxProfile(defaultProfilePath);
            }
            else if (ProfileMode == ProfileModes.Custom && !string.IsNullOrEmpty(profilePath))
            {
                options.Profile = new FirefoxProfile(profilePath);
            }
            options.AddArguments(arguments);
            return options;
        }

        private void CloseExtraTabs(IWebDriver browserDriver)
        {
            var windowHandles = browserDriver.WindowHandles;
            if (windowHandles != null && windowHandles.Count() > 1)
            {
                var newestTabCreated = windowHandles.FirstOrDefault();
                foreach (var tab in windowHandles)
                {
                    if (!tab.Equals(newestTabCreated))
                    {
                        browserDriver.SwitchTo().Window(tab);
                        browserDriver.Close();
                    }
                }
                browserDriver.SwitchTo().Window(newestTabCreated);
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            base.CacheMetadata(metadata);
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