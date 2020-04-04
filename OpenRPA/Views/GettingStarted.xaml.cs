using CefSharp;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class GettingStarted : System.Windows.Controls.UserControl
    {
        private string url;
        public GettingStarted(string url )
        {
            InitializeComponent();
            DataContext = this;
            this.url = url;
            CefSharp.Wpf.ChromiumWebBrowser browser;
            browser = new CefSharp.Wpf.ChromiumWebBrowser(this.url) { LifeSpanHandler = new BasicLifeSpanHandler() };
            content.Child = browser;
        }
        public bool ShowAgain
        {
            get
            {
                return Config.local.show_getting_started;
            }
            set
            {
                Config.local.show_getting_started = value;
                //NotifyPropertyChanged("ShowAgain");
            }
        }

    }
    public class BasicLifeSpanHandler : ILifeSpanHandler
    {
        public bool OnBeforePopup(IWebBrowser browser, string sourceUrl, string targetUrl, ref int x, ref int y, ref int width, ref int height)
        {
            return true;
        }
        public void OnBeforeClose(IWebBrowser browser)
        {
        }
        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            System.Diagnostics.Process.Start(targetUrl);
            newBrowser = null;
            return true;
        }
        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }
        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            return true;
        }
        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }
    }
}
