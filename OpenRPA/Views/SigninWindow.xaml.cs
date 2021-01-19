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
    public partial class SigninWindow : Window
    {
        private string url;
        private bool forceLogin;
        public SigninWindow(string url, bool forceLogin )
        {
            InitializeComponent();
            this.url = url;
            this.forceLogin = forceLogin;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            CefSharp.Wpf.ChromiumWebBrowser browser;
            if (forceLogin)
            {
                browser = new CefSharp.Wpf.ChromiumWebBrowser(this.url + "/PassiveSignout");
            }
            else
            {
                browser = new CefSharp.Wpf.ChromiumWebBrowser(this.url + "/Login");

            }
            browser.LoadingStateChanged += Browser_LoadingStateChanged;
            content.Child = browser;
        }
        public string getjwt(string url, string cookies)
        {
            try
            {
                var wc = new System.Net.WebClient();
                wc.Headers.Add("Cookie: " + cookies);
                wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                //byte[] result = wc.UploadData("<URL>", "POST", System.Text.Encoding.UTF8.GetBytes(postData));
                var result = wc.DownloadString(new Uri(url + "/jwtlong"));
                if (string.IsNullOrEmpty(result)) return "";
                var j = JObject.Parse(result);
                var jwt = j.Value<string>("jwt");
                Log.Verbose("********************");
                Log.Verbose(j.ToString());
                Log.Verbose("********************");
                return jwt;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return "";
            }
        }
        private string _jwt = null;
        public string jwt
        {
            get
            {
                return _jwt;
            }
        }
        private void Browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            var CookieManager = CefSharp.Cef.GetGlobalCookieManager();

            //cookieManager = Browser.GetCookieManager();

            if (!e.IsLoading)
            {
                CookieManager.VisitAllCookiesAsync().ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        string cookies = "";
                        var browsercookies = t.Result;
                        foreach (var cookie in browsercookies)
                        {
                            System.Diagnostics.Debug.WriteLine("CookieName: " + cookie.Name);
                            if(cookie.Domain == new Uri(this.url).DnsSafeHost) cookies += cookie.Name + "=" + cookie.Value + "; ";
                        }
                        if (!string.IsNullOrEmpty(cookies))
                        {
                            // System.Diagnostics.Debug.WriteLine(getjwt(this.url, cookies));
                            _jwt = getjwt(this.url, cookies);
                            if(!string.IsNullOrEmpty(_jwt))
                            {
                                GenericTools.RunUI(() => { DialogResult = true; });
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No Cookies found");
                    }
                });

            }
        }

        //public string jwt
        //{
        //    get
        //    {
        //        try
        //        {
        //            var cookies = FullWebBrowserCookie.GetCookieInternal(new Uri(url), false);
        //            WebClient wc = new WebClient();
        //            wc.Headers.Add("Cookie: " + cookies);
        //            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
        //            //byte[] result = wc.UploadData("<URL>", "POST", System.Text.Encoding.UTF8.GetBytes(postData));
        //            var result = wc.DownloadString(new Uri(url + "/jwtlong"));
        //            if (string.IsNullOrEmpty(result)) return "";
        //            var j = JObject.Parse(result);
        //            var jwt = j.Value<string>("jwt");
        //            return jwt;
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error(ex.ToString());
        //            return "";
        //        }
        //    }
        //}
        //private void WebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        //{
        //    myBrowser.Width = myBrowser.ActualWidth + 16;
        //    myBrowser.Height = myBrowser.ActualHeight + 16;
        //    if (!string.IsNullOrEmpty(jwt))
        //    {
        //        DialogResult = true;
        //    }
        //}
        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if(forceLogin) {
        //        myBrowser.Navigate(new Uri(url + "/PassiveSignout"));
        //    } else
        //    {
        //        myBrowser.Navigate(new Uri(url + "/Login"));
        //    }
        //}
    }

}
