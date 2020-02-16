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

namespace OpenRPA.NM
{
    public class NMHook
    {
        public static event Action<NativeMessagingMessage> onMessage;
        public static event Action<string> onDisconnected;
        public static event Action<string> Connected;
        private static NamedPipeClientAsync<NativeMessagingMessage> chromepipe = null;
        private static NamedPipeClientAsync<NativeMessagingMessage> ffpipe = null;
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
        public static bool connected
        {
            get
            {
                if (chromeconnected || ffconnected) return true;
                return false;
            }
        }
        public static void checkForPipes(bool chrome, bool ff)
        {
            //registreChromeNativeMessagingHost(false);
            //registreffNativeMessagingHost(false);
            if (chromepipe == null && chrome)
            {
                chromepipe = new NamedPipeClientAsync<NativeMessagingMessage>(PIPE_NAME + "_chrome");
                chromepipe.ServerMessage += Client_OnReceivedMessage;
                chromepipe.Disconnected += () => { onDisconnected?.Invoke("chrome"); };
                chromepipe.Connected += () => {  Connected?.Invoke("chrome"); Task.Run(()=> reloadtabs());  };
                chromepipe.Error += (e) => { Log.Debug(e.ToString()); };
                chromepipe.Start();
            }
            if (ffpipe == null && ff)
            {
                ffpipe = new NamedPipeClientAsync<NativeMessagingMessage>(PIPE_NAME + "_ff");
                ffpipe.ServerMessage += Client_OnReceivedMessage;
                ffpipe.Disconnected += () => { onDisconnected?.Invoke("ff"); };
                ffpipe.Connected += () => { Connected?.Invoke("ff"); Task.Run(() => reloadtabs()); };
                ffpipe.Error += (e) => { Log.Debug(e.ToString()); };
                ffpipe.Start();
            }
        }
        public static List<int> windows = new List<int>();
        public static List<NativeMessagingMessageTab> tabs = new List<NativeMessagingMessageTab>();
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
                if (msg.functionName == "windowcreated" && !windows.Contains(msg.windowId)) windows.Add(msg.windowId);
                if (msg.functionName == "windowremoved" && windows.Contains(msg.windowId)) windows.Remove(msg.windowId);
                if (msg.tab != null)
                {
                    if (msg.functionName == "tabcreated")
                    {
                        var tab = tabs.Where(x => x.id == msg.tab.id && x.browser == msg.browser).FirstOrDefault();
                        if (tab != null) tabs.Remove(tab);
                        msg.tab.browser = msg.browser;
                        Log.Verbose(msg.browser + " " + msg.functionName + " " + msg.tab.id + " " + msg.tab.status);
                        tabs.Add(msg.tab);
                    }
                    if (msg.functionName == "tabremoved" || msg.functionName == "tabupdated")
                    {
                        var tab = tabs.Where(x => x.id == msg.tab.id && x.browser == msg.browser).FirstOrDefault();
                        if (tab != null) tabs.Remove(tab);
                        if (msg.functionName == "tabupdated")
                        {
                            msg.tab.browser = msg.browser;
                            tabs.Add(msg.tab);
                            Log.Verbose(msg.browser + " " + msg.functionName + " " + msg.tab.id + " " + msg.tab.status);
                        }
                        else
                        {
                            Log.Verbose(msg.browser + " " + msg.functionName + " " + msg.tab.id);
                        }

                    }
                } else
                {
                    msg.tab = tabs.Where(x => x.id == msg.tabid && x.browser == msg.browser).FirstOrDefault();
                }


                if (msg.functionName == "tabactivated")
                {
                    foreach (var tab in tabs.Where(x => x.browser == msg.browser && x.windowId == msg.windowId))
                    {
                        tab.highlighted = (tab.id == msg.tabid);
                    }
                }
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
                    Log.Debug("Send and queue message " + message.functionName);
                    result = ffpipe.Message(message, throwError, timeout);
                }
            }
            else
            {
                if (chromeconnected)
                {
                    Log.Debug("Send and queue message " + message.functionName);
                    result = chromepipe.Message(message, throwError, timeout);
                }
            }
            return result;
        }
        public static void reloadtabs()
        {
            tabs.Clear();
            NativeMessagingMessage result = null;
            NativeMessagingMessage message = new NativeMessagingMessage("refreshtabs");
            result = sendMessageChromeResult(message, true, TimeSpan.FromSeconds(2));
            result = sendMessageFFResult(message, true, TimeSpan.FromSeconds(2));
        }
        public static void UpdateTab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("updatetab");
            NativeMessagingMessage result = null;
            message.browser = tab.browser; message.tabid = tab.id; message.tab = tab;
            message.windowId = tab.windowId;
            if (connected)
            {
                result = sendMessageResult(message, true, TimeSpan.FromSeconds(2));
                WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            }
        }
        public static void CloseTab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab");
            NativeMessagingMessage result = null;
            message.browser = tab.browser; message.tabid = tab.id; message.tab = tab;
            message.windowId = tab.windowId;
            if (connected)
            {
                result = sendMessageResult(message, true, TimeSpan.FromSeconds(2));
            }
        }
        public static void HighlightTab(NativeMessagingMessageTab tab)
        {
            if (!tab.highlighted)
            {
                tab.highlighted = true;
                UpdateTab(tab);
            }
        }
        public static void openurl(string browser, string url)
        {
            if (browser == "chrome")
            {
                if (!chromeconnected || tabs.Where(x=> x.browser == "chrome").Count() == 0)
                {
                    System.Diagnostics.Process.Start("chrome.exe", url);
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    do
                    {

                    } while (sw.Elapsed < TimeSpan.FromSeconds(10) && !chromeconnected);
                }
                else
                {
                    chromeopenurl(url, false);
                }
            }
            else
            {
                if (!ffconnected || tabs.Where(x => x.browser == "ff").Count() == 0)
                {
                    System.Diagnostics.Process.Start("firefox.exe", url);
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    do
                    {

                    } while (sw.Elapsed < TimeSpan.FromSeconds(10) && !ffconnected);

                }
                else
                {
                    ffopenurl(url, false);
                }

            }
        }
        internal static void ffopenurl(string url, bool forceNew)
        {
            if (ffconnected)
            {
                NativeMessagingMessage result = null;
                NativeMessagingMessage message = new NativeMessagingMessage("openurl") { data = url };
                reloadtabs();
                var tab = tabs.Where(x => x.url == url && x.highlighted == true && x.browser == "ff").FirstOrDefault();
                if (tab == null)
                {
                    tab = tabs.Where(x => x.url == url && x.browser == "ff").FirstOrDefault();
                }
                if (tab == null)
                {
                    tab = tabs.Where(x => x.highlighted == true && x.browser == "ff").FirstOrDefault();
                }
                if (tab != null && !forceNew)
                {
                    //if (tab.highlighted && tab.url == url) return;
                    message.functionName = "updatetab";
                    message.data = url;
                    tab.highlighted = true;
                    message.tab = tab;
                    result = ffpipe.Message(message, true, TimeSpan.FromSeconds(2));
                    WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
                    return;
                }
                result = ffpipe.Message(message, true, TimeSpan.FromSeconds(2));
                if (result == null) throw new Exception("Failed loading url " + url + " in ff");
                WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
                return;
            }
        }
        internal static void chromeopenurl(string url, bool forceNew)
        {
            if (chromeconnected)
            {
                NativeMessagingMessage result = null;
                NativeMessagingMessage message = new NativeMessagingMessage("openurl") { data = url };
                reloadtabs();
                var tab = tabs.Where(x => x.url == url && x.highlighted == true && x.browser == "chrome").FirstOrDefault();
                if (tab == null)
                {
                    tab = tabs.Where(x => x.url == url && x.browser == "chrome").FirstOrDefault();
                }
                if (tab == null)
                {
                    tab = tabs.Where(x => x.highlighted == true && x.browser == "chrome").FirstOrDefault();
                }
                if (tab != null && !forceNew)
                {
                    //if (tab.highlighted && tab.url == url) return;
                    message.functionName = "updatetab";
                    message.data = url;
                    tab.highlighted = true;
                    message.tab = tab;
                    result = chromepipe.Message(message, true, TimeSpan.FromSeconds(2));
                    if(result!=null && result.tab != null) WaitForTab(result.tab.id, result.browser, TimeSpan.FromSeconds(5));
                    return;
                }
                result = chromepipe.Message(message, true, TimeSpan.FromSeconds(2));
                if (result == null) throw new Exception("Failed loading url " + url + " in chrome");
                WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
                return;
            }
        }
        public static NMElement[] getElement(int tabid, string browser, string xPath, TimeSpan timeout)
        {
            var results = new List<NMElement>();
            var getelement = new NativeMessagingMessage("getelement");
            getelement.browser = browser;
            getelement.tabid = tabid;
            getelement.xPath = xPath;
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
                        res.tab = NMHook.tabs.Where(x => x.id == res.tabid  && x.browser == res.browser).FirstOrDefault();
                        var html = new NMElement(res);
                        results.Add(html);
                    }
                }
                //result = result.results[0];
            }
            return results.ToArray();
        }
        public static void WaitForTab(int tabid, string browser, TimeSpan timeout)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var tab = tabs.Where(x => x.id == tabid && x.browser == browser).FirstOrDefault();
            do
            {
                if (tab != null)
                {
                    Log.Debug("WaitForTab: " + tabid + " " + tab.status);
                }
                else
                {
                    Log.Debug("WaitForTab, failed locating tab: " + tabid);
                    reloadtabs();
                }
                System.Threading.Thread.Sleep(500);
                tab = tabs.Where(x => x.id == tabid).FirstOrDefault();
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
                if (PluginConfig.register_old_portname)
                {
                    if (localMachine)
                    {
                        if (!hklmExists(@"SOFTWARE\Google")) return;
                        if (!hklmExists(@"SOFTWARE\Google\Chrome")) return;
                        if (!hklmExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                        if (!hklmExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.zenamic.msg")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.zenamic.msg");
                    }
                    else
                    {
                        if (!hkcuExists(@"SOFTWARE\Google")) return;
                        if (!hkcuExists(@"SOFTWARE\Google\Chrome")) return;
                        if (!hkcuExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                        if (!hkcuExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.zenamic.msg")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.zenamic.msg");
                    }
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
                if (PluginConfig.register_old_portname)
                {

                    filename = System.IO.Path.Combine(basepath, "chromemanifestold.json");
                    if (!System.IO.File.Exists(filename)) return;
                    json = System.IO.File.ReadAllText(filename);
                    jsonObj = JsonConvert.DeserializeObject(json);
                    jsonObj["path"] = System.IO.Path.Combine(basepath, "OpenRPA.NativeMessagingHost.exe");
                    output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                    try
                    {
                        System.IO.File.WriteAllText(filename, output);
                    }
                    catch (Exception)
                    {
                    }
                }
                Microsoft.Win32.RegistryKey Chrome = null;
                if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.openrpa.msg", true);
                if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.openrpa.msg", true);
                Chrome.SetValue("", filename);
                if (PluginConfig.register_old_portname)
                {
                    if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.zenamic.msg", true);
                    if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.zenamic.msg", true);
                    Chrome.SetValue("", filename);
                }                
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
                if (PluginConfig.register_old_portname)
                {
                    if (localMachine)
                    {
                        if (!hklmExists(@"Software\Mozilla")) return;
                        if (!hklmExists(@"SOFTWARE\Mozilla\NativeMessagingHosts")) hklmCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts");
                        if (!hklmExists(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.zenamic.msg")) hklmCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.zenamic.msg");
                    }
                    else
                    {
                        if (!hkcuExists(@"SOFTWARE\Mozilla")) return;
                        if (!hkcuExists(@"SOFTWARE\Mozilla\NativeMessagingHosts")) hkcuCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts");
                        if (!hkcuExists(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.zenamic.msg")) hkcuCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.zenamic.msg");
                    }
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
                if (PluginConfig.register_old_portname)
                {
                    basepath = Interfaces.Extensions.PluginsDirectory;
                    filename = System.IO.Path.Combine(basepath, "ffmanifestold.json");
                    if (!System.IO.File.Exists(filename)) return;
                    json = System.IO.File.ReadAllText(filename);
                    jsonObj = JsonConvert.DeserializeObject(json);
                    jsonObj["path"] = System.IO.Path.Combine(basepath, "OpenRPA.NativeMessagingHost.exe");
                    output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                    try
                    {
                        System.IO.File.WriteAllText(filename, output);
                    }
                    catch (Exception)
                    {
                    }
                }
                Microsoft.Win32.RegistryKey Chrome = null;
                if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.openrpa.msg", true);
                if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.openrpa.msg", true);
                Chrome.SetValue("", filename);
                if (PluginConfig.register_old_portname)
                {
                    if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.zenamic.msg", true);
                    if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.zenamic.msg", true);
                    Chrome.SetValue("", filename);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

    }
}
