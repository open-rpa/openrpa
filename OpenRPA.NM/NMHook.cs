using OpenRPA.NamedPipeWrapper;
using Newtonsoft.Json;
using OpenRPA.Interfaces;
using static OpenRPA.Interfaces.RegUtil;
using OpenRPA.NM.pipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;

namespace OpenRPA.NM
{
    public class NMHook
    {
        public static event Action<NativeMessagingMessage> onMessage;
        public static event Action<string> onDisconnected;
        public static event Action<string> Connected;
        private static NamedPipeClientAsync<NativeMessagingMessage> chromepipe = null;
        private static NamedPipeClientAsync<NativeMessagingMessage> ffpipe = null;
        private static NamedPipeClientAsync<NativeMessagingMessage> edgepipe = null;
        public const string PIPE_NAME = "openrpa_nativebridge";
        public static bool chromeconnected
        {
            get
            {
                if (chromepipe == null) return false;
                return chromepipe.isConnected;
            }
        }
        public static bool ffconnected
        {
            get
            {
                if (ffpipe == null) return false;
                return ffpipe.isConnected;
            }
        }
        public static bool edgeconnected
        {
            get
            {
                if (edgepipe == null) return false;
                return edgepipe.isConnected;
            }
        }
        public static bool connected
        {
            get
            {
                if (chromeconnected || ffconnected || edgeconnected) return true;
                return false;
            }
        }
        public static void checkForPipes(bool chrome, bool ff, bool edge)
        {
            //registreChromeNativeMessagingHost(false);
            //registreffNativeMessagingHost(false);
            if (chromepipe == null && chrome)
            {
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                chromepipe = new NamedPipeClientAsync<NativeMessagingMessage>(SessionId + "_" + PIPE_NAME + "_chrome");
                chromepipe.ServerMessage += Client_OnReceivedMessage;
                chromepipe.Disconnected += () => { onDisconnected?.Invoke("chrome"); };
                chromepipe.Connected += () => {  Connected?.Invoke("chrome"); Task.Run(()=> enumwindowandtabs());  };
                chromepipe.Error += (e) => { Log.Debug(e.ToString()); };
                chromepipe.Start();
            }
            if (ffpipe == null && ff)
            {
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                ffpipe = new NamedPipeClientAsync<NativeMessagingMessage>(SessionId + "_" + PIPE_NAME + "_ff");
                ffpipe.ServerMessage += Client_OnReceivedMessage;
                ffpipe.Disconnected += () => { onDisconnected?.Invoke("ff"); };
                ffpipe.Connected += () => { Connected?.Invoke("ff"); Task.Run(() => enumwindowandtabs()); };
                ffpipe.Error += (e) => { Log.Debug(e.ToString()); };
                ffpipe.Start();
            }
            if (edgepipe == null && chrome)
            {
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                edgepipe = new NamedPipeClientAsync<NativeMessagingMessage>(SessionId + "_" + PIPE_NAME + "_edge");
                edgepipe.ServerMessage += Client_OnReceivedMessage;
                edgepipe.Disconnected += () => { onDisconnected?.Invoke("edge"); };
                edgepipe.Connected += () => { Connected?.Invoke("edge"); Task.Run(() => enumwindowandtabs()); };
                edgepipe.Error += (e) => { Log.Debug(e.ToString()); };
                edgepipe.Start();
            }
        }
        public static object ExecuteScript(string browser, int frameid, int tabid, string script, TimeSpan timeout)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("executescript", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            NativeMessagingMessage result = null;
            NativeMessagingMessageTab tab = null;
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            if (tabid > -1) tab = FindTabById(browser, tabid);
            if(tab==null)
            {
                if (browser == "chrome") tab = CurrentChromeTab;
                if (browser == "ff") tab = CurrentFFTab;
                if (browser == "edge") tab = CurrentEdgeTab;
            } 
            message.tab = tab;
            if (tab != null) { message.windowId = tab.windowId; message.tabid = tab.id; }
            message.browser = browser; message.frameId = frameid;
            message.script = script;
            result = sendMessageResult(message, false, timeout);
            if(result!=null)
            {
                return result.result;
            }
            return null;
        }
        private static List<NativeMessagingMessageWindow> windows = new List<NativeMessagingMessageWindow>();
        private static List<NativeMessagingMessageTab> tabs = new List<NativeMessagingMessageTab>();
        public static NativeMessagingMessageWindow CurrentChromeWindow
        {
            get
            {
                var win = windows.Where(x => x.browser == "chrome" && x.focused).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "chrome" && x.id == 1).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "chrome").FirstOrDefault();
                return win;
            }
        }
        public static NativeMessagingMessageWindow CurrentFFWindow
        {
            get
            {
                var win = windows.Where(x => x.browser == "ff" && x.focused).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "ff" && x.id == 1).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "ff").FirstOrDefault();
                return win;
            }
        }
        public static NativeMessagingMessageWindow CurrentEdgeWindow
        {
            get
            {
                var win = windows.Where(x => x.browser == "edge" && x.focused).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "edge" && x.id == 1).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "edge").FirstOrDefault();
                return win;
            }
        }
        public static NativeMessagingMessageTab CurrentChromeTab
        {
            get
            {
                var win = CurrentChromeWindow;
                if (win == null) return null;
                return FindTabByWindowId("chrome", win.id, true);
            }
        }
        public static NativeMessagingMessageTab CurrentFFTab
        {
            get
            {
                var win = CurrentFFWindow;
                if (win == null) return null;
                return FindTabByWindowId("ff", win.id, true);
            }
        }
        public static NativeMessagingMessageTab CurrentEdgeTab
        {
            get
            {
                var win = CurrentEdgeWindow;
                if (win == null) return null;
                return FindTabByWindowId("edge", win.id, true);
            }
        }
        public static NativeMessagingMessageTab CurrentTab // this is stupid ... 
        {
            get
            {
                NativeMessagingMessageTab CurrentTab = CurrentChromeTab;
                if (CurrentTab != null) return CurrentTab;
                CurrentTab = CurrentEdgeTab;
                if (CurrentTab != null) return CurrentTab;
                CurrentTab = CurrentFFTab;
                return CurrentTab;
            }
        }
        public static NativeMessagingMessageTab GetCurrentTab(string browser)
        {
            NativeMessagingMessageTab CurrentTab = null;
            if (browser == "chrome") CurrentTab = CurrentChromeTab;
            if (browser == "edge") CurrentTab = CurrentEdgeTab;
            if (browser == "ff") CurrentTab = CurrentFFTab;
            return CurrentTab;
        }
        private static void windowcreated(NativeMessagingMessage msg)
        {
            windowremoved(msg);
            var win = new NativeMessagingMessageWindow(msg);
            windows.Add(win);
        }
        private static void windowremoved(NativeMessagingMessage msg)
        {
            var win = windows.Where(x => x.id == msg.windowId).FirstOrDefault();
            if (win != null) windows.Remove(win);
        }
        private static void windowfocus(NativeMessagingMessage msg)
        {
            var win = windows.Where(x => x.id == msg.windowId).FirstOrDefault();
            if (win != null)
            {
                windows.ForEach(x => x.focused = false && x.browser == msg.browser);
                Log.Debug("Selected " + msg.browser + " windows " + win.id);
                win.focused = true;
            }
        }
        private static void tabcreated(NativeMessagingMessage msg)
        {
            var tab = FindTabById(msg.browser, msg.tab.id);
            if (tab != null)
            {
                tabupdated(msg);
                return;
            }
            msg.tab.browser = msg.browser;
            lock (tabs) tabs.Add(msg.tab);
        }
        private static void tabupdated(NativeMessagingMessage msg)
        {
            var tab = FindTabById(msg.browser, msg.tab.id);
            if (tab == null)
            {
                tabcreated(msg);
                return;
            }
            tab.browser = msg.browser;
            tab.active = msg.tab.active;
            tab.audible = msg.tab.audible;
            tab.autoDiscardable = msg.tab.autoDiscardable;
            tab.discarded = msg.tab.discarded;
            tab.favIconUrl = msg.tab.favIconUrl;
            tab.height = msg.tab.height;
            tab.highlighted = msg.tab.highlighted;
            tab.incognito = msg.tab.incognito;
            tab.index = msg.tab.index;
            tab.pinned = msg.tab.pinned;
            tab.selected = msg.tab.selected;
            tab.status = msg.tab.status;
            Console.WriteLine(tab.status);
            tab.title = msg.tab.title;
            tab.url = msg.tab.url;
            tab.width = msg.tab.width;
            tab.windowId = msg.tab.windowId;
        }
        private static void tabremoved(NativeMessagingMessage msg)
        {
            var tab = FindTabById(msg.browser, msg.tabid);
            if (tab != null) lock (tabs) tabs.Remove(tab);
        }
        private static void tabactivated(NativeMessagingMessage msg)
        {
            lock(tabs)
            {
                foreach (var tab in tabs.Where(x => x.browser == msg.browser && x.windowId == msg.windowId))
                {
                    tab.highlighted = (tab.id == msg.tabid);
                    tab.selected = (tab.id == msg.tabid);
                    if (tab.highlighted)
                    {
                        Log.Debug("Selected " + msg.browser + " tab " + msg.tabid + " (" + tab.title + ")");
                    }
                }
            }
        }
        private static void downloadcomplete(NativeMessagingMessage msg)
        {
            var json = msg.data;
            var download = JsonConvert.DeserializeObject<Download>(json);
            foreach(var p in Plugins.detectorPlugins)
            {
                if(p is DownloadDetectorPlugin plugin)
                {
                    plugin.RaiseDetector(download);
                }
            }
            var e = new DetectorEvent(download);
            foreach (var wi in Plugin.client.WorkflowInstances.ToList())
            {
                if (wi.isCompleted) continue;
                if (wi.Bookmarks != null)
                {
                    foreach (var b in wi.Bookmarks)
                    {
                        if (b.Key == "DownloadDetectorPlugin")
                        {
                            wi.ResumeBookmark(b.Key, e);
                        }
                    }
                }
            }
        }
        private static void Client_OnReceivedMessage(NativeMessagingMessage message)
        {
            try
            {
                NativeMessagingMessage msg;
                try
                {
                    msg = message;
                    //msg = JsonConvert.DeserializeObject<NativeMessagingMessage>(e.Message);
                    if (string.IsNullOrEmpty(message.functionName) || message.functionName == "ping") return;
                }
                catch (Exception)
                {
                    return;
                }
                if(msg.functionName != "mousemove")
                {
                    Log.Verbose("[nmhook][resc][" + msg.browser + "]" + msg.functionName + " for tab " + msg.tabid + " - " + msg.messageid);
                    //Log.Output("[nmhook][resc][" + msg.browser + "]" + msg.functionName + " for tab " + msg.tabid + " - " + msg.messageid + " (" + msg.uix + "," + msg.uiy + "," + msg.uiwidth + "," + msg.uiheight + ")");
                }
                if (PluginConfig.compensate_for_old_addon)
                {
                    msg.uix -= 7;
                    msg.uiy += 7;
                }
                if (msg.functionName == "windowcreated") windowcreated(msg);
                if (msg.functionName == "windowremoved") windowremoved(msg);
                if (msg.functionName == "windowfocus") windowfocus(msg);
                if (msg.functionName == "tabcreated") tabcreated(msg);
                if (msg.functionName == "tabremoved") tabremoved(msg);
                if (msg.functionName == "tabupdated") tabupdated(msg);
                if (msg.functionName == "tabactivated") tabactivated(msg);
                if (msg.functionName == "downloadcomplete") downloadcomplete(msg);
                Task.Run(() => { onMessage?.Invoke(msg); });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        async public static Task<NativeMessagingMessage> sendMessageResultAsync(NativeMessagingMessage message, bool throwError)
        {
            NativeMessagingMessage result = null;
            if (message.browser == "ff")
            {
                if (ffconnected)
                {
                    result = await ffpipe.MessageAsync(message, throwError);
                }
            }
            else
            {
                if (chromeconnected)
                {
                    result = await chromepipe.MessageAsync(message, throwError);
                }
            }
            return result;
        }
        public static NativeMessagingMessage sendMessageResult(NativeMessagingMessage message, bool throwError, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (message.browser == "ff")
            {
                if (ffconnected)
                {
                    // Log.Debug("Send and queue message " + message.functionName);
                    result = ffpipe.Message(message, throwError, timeout);
                }
            }
            else if (message.browser == "edge")
            {
                if (edgeconnected)
                {
                    // Log.Debug("Send and queue message " + message.functionName);
                    result = edgepipe.Message(message, throwError, timeout);
                }
            }
            else
            {
                if (chromeconnected)
                {
                    // Log.Debug("Send and queue message " + message.functionName);
                    result = chromepipe.Message(message, throwError, timeout);
                }
            }
            return result;
        }
        public static void enumwindows()
        {
            windows.Clear();
            NativeMessagingMessage message = new NativeMessagingMessage("enumwindows", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            if (chromeconnected)
            {
                var result = sendMessageChromeResult(message, true, TimeSpan.FromSeconds(3));
                if(result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "windowcreated") windowcreated(msg);
                    }
            }
            if (ffconnected)
            {
                var result = sendMessageFFResult(message, true, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "windowcreated") windowcreated(msg);
                    }
            }
            if (edgeconnected)
            {
                var result = sendMessageEdgeResult(message, true, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "windowcreated") windowcreated(msg);
                    }
            }
        }
        public static void enumtabs()
        {
            lock(tabs) tabs.Clear();
            NativeMessagingMessage message = new NativeMessagingMessage("enumtabs", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);

            if (chromeconnected)
            {
                var result = sendMessageChromeResult(message, true, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "tabcreated") tabcreated(msg);
                    }
            }
            if (ffconnected)
            {
                var result = sendMessageFFResult(message, true, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "tabcreated") tabcreated(msg);
                    }
            }
            if (edgeconnected)
            {
                var result = sendMessageEdgeResult(message, true, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "tabcreated") tabcreated(msg);
                    }
            }
        }
        public static void enumwindowandtabs()
        {
            enumwindows();
            enumtabs();
        }
        public static void CloseTab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            NativeMessagingMessage result = null;
            message.browser = tab.browser; message.tabid = tab.id; message.tab = tab;
            message.windowId = tab.windowId;
            if (connected)
            {
                result = sendMessageResult(message, true, PluginConfig.protocol_timeout);
            }
        }
        //public static void HighlightTab(NativeMessagingMessageTab tab)
        //{
        //    if (!tab.highlighted)
        //    {
        //        tab.highlighted = true;
        //        UpdateTab(tab);
        //    }
        //}
        public static void openurl(string browser, string url, bool newtab)
        {
            if (browser == "chrome")
            {
                int tabcount = 0;
                if(chromeconnected) lock(tabs) tabcount = tabs.Where(x => x.browser == "chrome").Count();
                if (!chromeconnected || tabcount == 0)
                {
                    System.Diagnostics.Process.Start("chrome.exe", url);
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    do
                    {
                        System.Threading.Thread.Sleep(500);
                        Console.WriteLine("pending chrome addon to connect");
                    } while (sw.Elapsed < TimeSpan.FromSeconds(20) && !chromeconnected);
                }
                else
                {
                    chromeopenurl(url, newtab);
                }
            }
            else if (browser == "edge")
            {
                int tabcount = 0;
                if (chromeconnected) lock (tabs) tabcount = tabs.Where(x => x.browser == "edge").Count();
                if (!edgeconnected || tabcount == 0)
                {
                    System.Diagnostics.Process.Start("msedge.exe", url);
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    do
                    {
                        System.Threading.Thread.Sleep(500);
                        Console.WriteLine("pending edge addon to connect");
                    } while (sw.Elapsed < TimeSpan.FromSeconds(20) && !edgeconnected);
                }
                else
                {
                    edgeopenurl(url, newtab);
                }
            }
            else
            {
                int tabcount = 0;
                if (chromeconnected) lock (tabs) tabcount = tabs.Where(x => x.browser == "ff").Count();
                if (!ffconnected || tabcount == 0)
                {
                    System.Diagnostics.Process.Start("firefox.exe", url);
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    do
                    {
                        System.Threading.Thread.Sleep(500);
                        Console.WriteLine("pending ff addon to connect");
                    } while (sw.Elapsed < TimeSpan.FromSeconds(20) && !ffconnected);

                }
                else
                {
                    ffopenurl(url, newtab);
                }

            }
        }
        internal static void ffopenurl(string url, bool forceNew)
        {
            if (ffconnected)
            {
                NativeMessagingMessage message = new NativeMessagingMessage("openurl", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { data = url };
                message.xPath = forceNew.ToString().ToLower();
                var result = ffpipe.Message(message, true, TimeSpan.FromSeconds(2));
                if (result != null && result.tab != null) WaitForTab(result.tab.id, result.browser, TimeSpan.FromSeconds(5));
            }
        }
        internal static void chromeopenurl(string url, bool forceNew)
        {
            if (chromeconnected)
            {
                NativeMessagingMessage message = new NativeMessagingMessage("openurl", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { data = url };
                message.xPath = forceNew.ToString().ToLower();
                var result = chromepipe.Message(message, true, TimeSpan.FromSeconds(2));
                if(result!=null && result.tab != null) WaitForTab(result.tab.id, result.browser, TimeSpan.FromSeconds(5));

                //NativeMessagingMessage result = null;
                //NativeMessagingMessage message = new NativeMessagingMessage("openurl") { data = url };
                //enumtabs();
                //var tab = tabs.Where(x => x.url == url && x.highlighted == true && x.browser == "chrome").FirstOrDefault();
                //if (tab == null)
                //{
                //    tab = tabs.Where(x => x.url == url && x.browser == "chrome").FirstOrDefault();
                //}
                //if (tab == null)
                //{
                //    tab = tabs.Where(x => x.highlighted == true && x.browser == "chrome").FirstOrDefault();
                //}
                //if (tab != null && !forceNew)
                //{
                //    //if (tab.highlighted && tab.url == url) return;
                //    message.functionName = "updatetab";
                //    message.data = url;
                //    tab.highlighted = true;
                //    message.tab = tab;
                //    result = chromepipe.Message(message, true, TimeSpan.FromSeconds(2));
                //    if(result!=null && result.tab != null) WaitForTab(result.tab.id, result.browser, TimeSpan.FromSeconds(5));
                //    return;
                //}
                //result = chromepipe.Message(message, true, TimeSpan.FromSeconds(2));
                //if (result == null) throw new Exception("Failed loading url " + url + " in chrome");
                //WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
                //return;
            }
        }
        internal static void edgeopenurl(string url, bool forceNew)
        {
            if (edgeconnected)
            {
                NativeMessagingMessage message = new NativeMessagingMessage("openurl", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { data = url };
                message.xPath = forceNew.ToString().ToLower();
                var result = edgepipe.Message(message, true, TimeSpan.FromSeconds(2));
                if (result != null && result.tab != null) WaitForTab(result.tab.id, result.browser, TimeSpan.FromSeconds(5));
            }
        }
        public static NMElement[] getElement(int tabid, long frameId, string browser, string xPath, TimeSpan timeout)
        {
            var results = new List<NMElement>();
            var getelement = new NativeMessagingMessage("getelement", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            getelement.browser = browser;
            getelement.tabid = tabid;
            getelement.xPath = xPath;
            getelement.frameId = frameId;
            NativeMessagingMessage result = null;
            try
            {
                result = NMHook.sendMessageResult(getelement, true, timeout);
            }
            catch (Exception)
            {
            }
            if (result != null && result.result != null && result.results == null)
            {
                result.results = new NativeMessagingMessage[] { result };
            }
            if (result != null && result.results != null && result.results.Count() > 0)
            {
                foreach (var res in result.results)
                {
                    if (res.result != null)
                    {
                        //var html = new HtmlElement(getelement.xPath, getelement.cssPath, res.tabid, res.frameId, res.result);
                        res.tab = FindTabById(res.browser, res.tabid);
                        var html = new NMElement(res);
                        results.Add(html);
                    }
                }
                //result = result.results[0];
            }
            return results.ToArray();
        }
        public static void WaitForTab(int tabid, string browser, TimeSpan timeout, DateTime? lastready = null)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            enumtabs();
            var tab = FindTabById(browser, tabid);
            do
            {
                if (tab != null)
                {
                    // Log.Debug("WaitForTab: " + tabid + " " + tab.status);
                }
                else
                {
                    // Log.Debug("WaitForTab, failed locating tab: " + tabid);
                    enumtabs();
                }
                System.Threading.Thread.Sleep(500);
                tab = FindTabById(browser, tabid);
            } while (tab != null && tab.status != "ready" && tab.status != "complete" && sw.Elapsed < timeout);
            return;
        }
        public static NativeMessagingMessage sendMessageChromeResult(NativeMessagingMessage message, bool throwError, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (chromeconnected)
            {
                result = chromepipe.Message(message, throwError, timeout);
            }
            return result;
        }
        public static NativeMessagingMessage sendMessageFFResult(NativeMessagingMessage message, bool throwError, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (ffconnected)
            {
                result = ffpipe.Message(message, throwError, timeout);
            }
            return result;
        }
        public static NativeMessagingMessage sendMessageEdgeResult(NativeMessagingMessage message, bool throwError, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (edgeconnected)
            {
                result = edgepipe.Message(message, throwError, timeout);
            }
            return result;
        }
        public static void registreChromeNativeMessagingHost(bool localMachine)
        {
            try
            {
                if (localMachine)
                {
                    if (!hklmExists(@"SOFTWARE\Google")) return;
                    if (!hklmExists(@"SOFTWARE\Google\Chrome")) return;
                    if (!hklmExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                    if (!hklmExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.openrpa.msg")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.openrpa.msg");
                }
                else
                {
                    if (!hkcuExists(@"SOFTWARE\Google")) return;
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome")) return;
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.openrpa.msg")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.openrpa.msg");
                }
                var basepath = Interfaces.Extensions.PluginsDirectory;
                var filename = System.IO.Path.Combine(basepath, "chromemanifest.json");
                if (!System.IO.File.Exists(filename)) return;
                string json = System.IO.File.ReadAllText(filename);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);
                jsonObj["path"] = System.IO.Path.Combine(basepath, "OpenRPA.NativeMessagingHost.exe");
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                try
                {
                    System.IO.File.WriteAllText(filename, output);
                }
                catch (Exception)
                {
                }
                Microsoft.Win32.RegistryKey Chrome = null;
                if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.openrpa.msg", true);
                if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.openrpa.msg", true);
                Chrome.SetValue("", filename);


                //if (localMachine)
                //{
                //    if (!hklmExists(@"SOFTWARE\Policies")) return;
                //    if (!hklmExists(@"SOFTWARE\Policies\Google")) hklmCreate(@"SOFTWARE\Policies\Google");
                //    if (!hklmExists(@"SOFTWARE\Policies\Google\Chrome")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                //    if (!hklmExists(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist")) hklmCreate(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist");
                //    if (!hklmExists(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist")) hklmCreate(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist");

                //    Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist", true);
                //    var names = rk.GetSubKeyNames();
                //    string id = null;
                //    foreach (var name in names)
                //    {
                //        var value = rk.GetValue(name);
                //        if (value != null && value.ToString() == "hpnihnhlcnfejboocnckgchjdofeaphe") id = name;
                //    }
                //    if (string.IsNullOrEmpty(id))
                //    {
                //        rk.SetValue((names.Length + 1).ToString(), "hpnihnhlcnfejboocnckgchjdofeaphe");
                //    }
                //    rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", true);
                //    names = rk.GetSubKeyNames();
                //    id = null;
                //    foreach (var name in names)
                //    {
                //        var value = rk.GetValue(name);
                //        if (value != null && value.ToString() == "hpnihnhlcnfejboocnckgchjdofeaphe") id = name;
                //    }
                //    if (string.IsNullOrEmpty(id))
                //    {
                //        rk.SetValue((names.Length + 1).ToString(), "hpnihnhlcnfejboocnckgchjdofeaphe");
                //    }

                //}
                //else
                //{
                //    if (!hkcuExists(@"SOFTWARE\Policies")) return;
                //    if (!hkcuExists(@"SOFTWARE\Policies\Google")) hkcuCreate(@"SOFTWARE\Policies\Google");
                //    if (!hkcuExists(@"SOFTWARE\Policies\Google\Chrome")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                //    if (!hkcuExists(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist")) hkcuCreate(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist");
                //    if (!hkcuExists(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist")) hkcuCreate(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist");

                //    Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist", true);
                //    var names = rk.GetSubKeyNames();
                //    string id = null;
                //    foreach (var name in names)
                //    {
                //        var value = rk.GetValue(name);
                //        if (value != null && value.ToString() == "hpnihnhlcnfejboocnckgchjdofeaphe") id = name;
                //    }
                //    if (string.IsNullOrEmpty(id))
                //    {
                //        rk.SetValue((names.Length + 1).ToString(), "hpnihnhlcnfejboocnckgchjdofeaphe");
                //    }
                //    rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", true);
                //    names = rk.GetSubKeyNames();
                //    id = null;
                //    foreach (var name in names)
                //    {
                //        var value = rk.GetValue(name);
                //        if (value != null && value.ToString() == "hpnihnhlcnfejboocnckgchjdofeaphe") id = name;
                //    }
                //    if (string.IsNullOrEmpty(id))
                //    {
                //        rk.SetValue((names.Length + 1).ToString(), "hpnihnhlcnfejboocnckgchjdofeaphe");
                //    }

                //}

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static void registreffNativeMessagingHost(bool localMachine)
        {
            try
            {
                if (localMachine)
                {
                    if (!hklmExists(@"Software\Mozilla")) return;
                    if (!hklmExists(@"SOFTWARE\Mozilla\NativeMessagingHosts")) hklmCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts");
                    if (!hklmExists(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.openrpa.msg")) hklmCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.openrpa.msg");
                }
                else
                {
                    if (!hkcuExists(@"SOFTWARE\Mozilla")) return;
                    if (!hkcuExists(@"SOFTWARE\Mozilla\NativeMessagingHosts")) hkcuCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts");
                    if (!hkcuExists(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.openrpa.msg")) hkcuCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.openrpa.msg");
                }
                var basepath = Interfaces.Extensions.PluginsDirectory;
                var filename = System.IO.Path.Combine(basepath, "ffmanifest.json");
                if (!System.IO.File.Exists(filename)) return;
                string json = System.IO.File.ReadAllText(filename);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);
                jsonObj["path"] = System.IO.Path.Combine(basepath, "OpenRPA.NativeMessagingHost.exe");
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                try
                {
                    System.IO.File.WriteAllText(filename, output);
                }
                catch (Exception)
                {
                }
                Microsoft.Win32.RegistryKey Chrome = null;
                if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.openrpa.msg", true);
                if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.openrpa.msg", true);
                Chrome.SetValue("", filename);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static NativeMessagingMessageTab updatetab(string browser, NativeMessagingMessageTab tab)
        {
            if (browser == "chrome")
            {
                if (chromeconnected)
                {
                    return chromeupdatetab(tab);
                }
            }
            else if (browser == "edge")
            {
                if (edgeconnected)
                {
                    return edgeupdatetab(tab);
                }
            }
            else
            {
                if (ffconnected)
                {
                    return ffupdatetab(tab);
                }
            }
            return null;
        }
        internal static NativeMessagingMessageTab ffupdatetab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("updatetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tab.id, tab = tab };
            NativeMessagingMessage result = ffpipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("ff", tab.id);
        }
        internal static NativeMessagingMessageTab chromeupdatetab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("updatetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tab.id, tab = tab };
            NativeMessagingMessage result = chromepipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("chrome", tab.id);
        }
        internal static NativeMessagingMessageTab edgeupdatetab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("updatetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tab.id, tab = tab };
            NativeMessagingMessage result = edgepipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("edge", tab.id);
        }
        public static NativeMessagingMessageTab selecttab(string browser, int tabid)
        {
            if (browser == "chrome")
            {
                if (chromeconnected)
                {
                    return chromeselecttab(tabid);
                }
            }
            else if (browser == "edge")
            {
                if (edgeconnected)
                {
                    return edgeselecttab(tabid);
                }
            }
            else
            {
                if (ffconnected)
                {
                    return ffselecttab(tabid);
                }
            }
            return null;
        }
        internal static NativeMessagingMessageTab ffselecttab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("selecttab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = ffpipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("ff", tabid);
        }
        internal static NativeMessagingMessageTab chromeselecttab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("selecttab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = chromepipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("chrome", tabid);
        }
        internal static NativeMessagingMessageTab edgeselecttab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("selecttab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = edgepipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("edge", tabid);
        }
        public static NativeMessagingMessageTab FindTabByURL(string browser, string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            NativeMessagingMessageTab CurrentTab = null;
            if (browser == "chrome") CurrentTab = CurrentChromeTab;
            if (browser == "edge") CurrentTab = CurrentEdgeTab;
            if (browser == "ff") CurrentTab = CurrentFFTab;
            if (CurrentTab != null && !string.IsNullOrEmpty(CurrentTab.url) && CurrentTab.url.ToLower().StartsWith(url.ToLower())) return CurrentTab;
            lock (tabs)
            {
                CurrentTab = tabs.Where(x => x.browser == browser && !string.IsNullOrEmpty(x.url) && x.url.ToLower().StartsWith(url.ToLower())).FirstOrDefault();
            }
            return CurrentTab;
        }
        public static NativeMessagingMessageTab FindTabById(string browser, int id)
        {
            NativeMessagingMessageTab CurrentTab = null;
            lock (tabs)
            {
                CurrentTab = tabs.Where(x => x.browser == browser && x.id == id).FirstOrDefault();
            }
            return CurrentTab;
        }
        public static NativeMessagingMessageTab FindTabByWindowId(string browser, int windowId, bool isSelectéd)
        {
            lock (tabs)
            {
                if(isSelectéd)
                {
                    // (x.selected || x.highlighted) ?
                    return tabs.Where(x => x.browser == browser && x.windowId == windowId && x.selected).FirstOrDefault();
                } else
                {
                    return tabs.Where(x => x.browser == browser && x.windowId == windowId).FirstOrDefault();
                }
            }
        }
        public static void CloseTab(string browser, int tabid)
        {
            if (browser == "chrome")
            {
                if (chromeconnected)
                {
                    chromeclosetab(tabid);
                }
            }
            else if (browser == "edge")
            {
                if (edgeconnected)
                {
                    edgeclosetab(tabid);
                }
            }
            else
            {
                if (ffconnected)
                {
                    ffclosetab(tabid);
                }
            }
        }
        public static void CloseAllTabs(string browser)
        {
            List<NativeMessagingMessageTab> _tabs;
            lock(tabs)
            {
                _tabs = tabs.Where(x => x.browser == browser).ToList();
            }
            foreach (var tab in _tabs)
            {
                NMHook.CloseTab(browser, tab.id);
            }
        }
        internal static void ffclosetab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = ffpipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
        }
        internal static void chromeclosetab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = chromepipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
        }
        internal static void edgeclosetab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = edgepipe.Message(message, true, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
        }
    }
}
