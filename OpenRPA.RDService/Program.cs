using OpenRPA.Interfaces.entity;
using OpenRPA.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    using FlaUI.UIA3.Patterns;
    using OpenRPA.Interfaces;
    using System.IO.Pipes;
    using System.Threading;

    class Program
    {
        public const int StartupWaitSeconds = 0;
        public const string ServiceName = "OpenRPA";
        private static ServiceManager manager = new ServiceManager(ServiceName);
        private static ServiceManager Monitormanager = new ServiceManager("OpenRPAMon");
        public static bool isService = false;
        private static Tracing tracing = null;
        private static System.Timers.Timer reloadTimer = null;
        private void GetSessions()
        {
            IntPtr server = IntPtr.Zero;
            List<string> ret = new List<string>();
            try
            {
                server = NativeMethods.WTSOpenServer(".");
                IntPtr ppSessionInfo = IntPtr.Zero;
                int count = 0;
                int retval = NativeMethods.WTSEnumerateSessions(server, 0, 1, ref ppSessionInfo, ref count);
                int dataSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.WTS_SESSION_INFO));
                long current = (int)ppSessionInfo;
                if (retval != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        NativeMethods.WTS_SESSION_INFO si = (NativeMethods.WTS_SESSION_INFO)System.Runtime.InteropServices.Marshal.PtrToStructure((System.IntPtr)current, typeof(NativeMethods.WTS_SESSION_INFO));
                        current += dataSize;
                        ret.Add(si.SessionID + " " + si.State + " " + si.pWinStationName);
                    }
                    NativeMethods.WTSFreeMemory(ppSessionInfo);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                try
                {
                    NativeMethods.WTSCloseServer(server);
                }
                catch (Exception)
                {
                }
            }
        }
        private static string logpath = "";
        private static void log(string message)
        {
            try
            {
                Console.WriteLine(message);
                DateTime dt = DateTime.Now;
                var _msg = string.Format(@"[{0:HH\:mm\:ss\.fff}] {1}", dt, message);
                System.IO.File.AppendAllText(System.IO.Path.Combine(logpath, "log_rdservice.txt"), _msg + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static string[] args;
        static void Main(string[] args)
        {
            try
            {
                InitializeOTEL();
                Program.args = args;
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                var filepath = asm.CodeBase.Replace("file:///", "");
                logpath = System.IO.Path.GetDirectoryName(filepath);


                //UIThread = new Thread(() =>
                //{
                //    System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                //    {
                //        AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
                //    }));

                //    System.Windows.Threading.Dispatcher.Run();
                //});

                //UIThread.SetApartmentState(ApartmentState.STA);
                //UIThread.Start();



                log("GetParentProcessId");
                var parentProcess = NativeMethods.GetParentProcessId();
                log("Check parentProcess");
                isService = (parentProcess.ProcessName.ToLower() == "services");
                Console.WriteLine("****** isService: " + isService);
                if (isService)
                {
                    log("ServiceBase.Run");
                    System.ServiceProcess.ServiceBase.Run(new MyServiceBase(ServiceName, DoWork));
                }
                else
                {
                    log("DoWork");
                    DoWork();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = (Exception)args.ExceptionObject;
            Log.Error(ex.ToString());
            Log.Error("MyHandler caught : " + ex.Message);
            Log.Error("Runtime terminating: {0}", (args.IsTerminating).ToString());
        }
        private static void WebSocketClient_OnQueueMessage(Interfaces.IQueueMessage message, Interfaces.QueueMessageEventArgs e)
        {
            Log.Debug("WebSocketClient_OnQueueMessage");
        }
        private static bool autoReconnect = true;
        private async static void WebSocketClient_OnClose(string reason)
        {
            Log.Information("Disconnected " + reason);
            await Task.Delay(1000);
            if (autoReconnect)
            {
                autoReconnect = false;
                try
                {
                    await global.webSocketClient.Connect();
                    autoReconnect = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
        public static byte[] Base64Decode(string base64EncodedData)
        {
            return System.Convert.FromBase64String(base64EncodedData);
        }
        public static string Base64Encode(byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes);
        }
        private static async void WebSocketClient_OnOpen()
        {
            try
            {
                InitializeOTEL();
                Log.Information("WebSocketClient_OnOpen");
                TokenUser user = null;
                while (user == null)
                {
                    if (!string.IsNullOrEmpty(PluginConfig.tempjwt))
                    {
                        Log.Information("Signin using tempjwt");
                        user = await global.webSocketClient.Signin(PluginConfig.tempjwt, "RDService", global.version);
                        if (user != null)
                        {
                            if (isService)
                            {
                                Log.Information("Store new jwt");
                                PluginConfig.jwt = Base64Encode(PluginConfig.ProtectString(PluginConfig.tempjwt));
                                PluginConfig.tempjwt = null;
                                PluginConfig.Save();
                            }
                            Log.Information("Signed in as " + user.username);
                        }
                    }
                    else if (PluginConfig.jwt != null && PluginConfig.jwt.Length > 0)
                    {
                        Log.Information("Signing in");
                        user = await global.webSocketClient.Signin(PluginConfig.UnprotectString(Base64Decode(PluginConfig.jwt)), "RDService", global.version);
                        if (user != null)
                        {
                            Log.Information("Signed in as " + user.username);
                        }
                    }
                    else
                    {
                        Log.Error("Missing jwt from config, close down");
                        _ = global.webSocketClient.Close();
                        if (isService) await manager.StopService();
                        if (!isService) Environment.Exit(0);
                        return;
                    }
                }
                Log.Information("Get hostname and fqdn");
                string computername = NativeMethods.GetHostName().ToLower();
                string computerfqdn = NativeMethods.GetFQDN().ToLower();
                Log.Information("Query for unattendedserver object in openrpa collection matcing computername " + computername + " and computerfqdn " + computerfqdn);
                var servers = await global.webSocketClient.Query<unattendedserver>("openrpa", "{'_type':'unattendedserver', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
                server = servers.FirstOrDefault();
                if (servers.Length == 0)
                {
                    Log.Information("Adding new unattendedserver for " + computerfqdn);
                    server = new unattendedserver() { computername = computername, computerfqdn = computerfqdn, name = computerfqdn, enabled = true };
                    server = await global.webSocketClient.InsertOne("openrpa", 1, false, server, "", "");
                }
                //var clients = await global.webSocketClient.Query<unattendedclient>("openrpa", "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
                //foreach (var c in clients) sessions.Add(new RobotUserSession(c));
                // Log.Information("Loaded " + sessions.Count + " sessions");
                // Create listener for robots to connect too
                var supports_watch = true;
                Log.Information("Check openflow config if it supports watches");
                try
                {
                    supports_watch = global.openflowconfig.supports_watch;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                if(supports_watch)
                {
                    try
                    {
                        //if (string.IsNullOrEmpty(openrpa_watchid))
                        //{
                        // "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}"
                        // openrpa_watchid = await global.webSocketClient.Watch("openrpa", "[{ '$match': { 'fullDocument._type': {'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'} } }]", onWatchEvent);
                        var watchid = await global.webSocketClient.Watch("openrpa", "[{ '$match': {'fullDocument.computername':'" + computername + "', 'fullDocument.computerfqdn':'" + computerfqdn + "'} }]", onWatchEvent, "", "");

                        // openrpa_watchid = await global.webSocketClient.Watch("openrpa", "[]", onWatchEvent);
                        await ReloadConfig();
                        log("watch created with id " + watchid);
                        //}
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        supports_watch = false;
                    }
                }
                if (!supports_watch)
                {
                    Log.Information("Watches are not supported use timer instead");
                    if (reloadTimer == null)
                    {
                        reloadTimer = new System.Timers.Timer(PluginConfig.reloadinterval.TotalMilliseconds);
                        reloadTimer.Elapsed += async (o, e) =>
                        {
                            reloadTimer.Stop();
                            try
                            {
                                await ReloadConfig();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                            reloadTimer.Start();
                        };
                    }
                    reloadTimer.Start();
                    log("watches not supported, added timer with " + PluginConfig.reloadinterval.TotalSeconds + " seconds itnerval");
                }
                log("initialization complete, if any unattendedclients was found they should be loggin in now");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
            }
        }
        private static void onWatchEvent(string id, Newtonsoft.Json.Linq.JObject data)
        {
            Log.Information("onWatchEvent");
            try
            {
                string _type = data["fullDocument"].Value<string>("_type");
                string _id = data["fullDocument"].Value<string>("_id");
                long _version = data["fullDocument"].Value<long>("_version");
                string operationType = data.Value<string>("operationType");
                Log.Information("operationType: " + operationType);
                if (operationType != "replace" && operationType != "insert" && operationType != "update") return; // we don't support delete right now
                if (_type == "unattendedclient")
                {
                    var unattendedclient = Newtonsoft.Json.JsonConvert.DeserializeObject<unattendedclient>(data["fullDocument"].ToString());
                    if (unattendedclient != null && unattendedclient.computerfqdn == server.computerfqdn && unattendedclient.computername == server.computername)
                    {
                        UnattendedclientUpdated(unattendedclient);
                    }
                    else if (unattendedclient == null)
                    {
                        Log.Error("Failed DeserializeObject");
                        return;
                    }
                }
                if (_type == "unattendedserver")
                {
                    // var unattendedserver = Newtonsoft.Json.JsonConvert.DeserializeObject<unattendedserver>(data["fullDocument"].ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private static void UnattendedclientUpdated(unattendedclient unattendedclient)
        {
            if (unattendedclient == null) return;
            try
            {
                RobotUserSession session = null;
                if (sessions != null) session = sessions.Where(x => x.client._id == unattendedclient._id).FirstOrDefault();
                if (!unattendedclient.enabled)
                {
                    if (session != null)
                    {
                        session.client = unattendedclient;
                        if (session.rdp != null || session.freerdp != null)
                        {
                            Log.Information("disconnecting session for " + session.client.windowsusername);
                            try
                            {
                                session.disconnectrdp();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        }

                    }
                    Log.Information("GetOwnerExplorer");
                    System.Diagnostics.Process ownerexplorer = RobotUserSession.GetOwnerExplorer(unattendedclient);
                    if (ownerexplorer != null)
                    {
                        if (server.logoff || session.client.autosignout)
                        {
                            Log.Information("WTSLogoffSession " + ownerexplorer.SessionId);
                            NativeMethods.WTSLogoffSession(IntPtr.Zero, (int)ownerexplorer.SessionId, true);
                        }
                        else
                        {
                            Log.Information("WTSDisconnectSession " + ownerexplorer.SessionId);
                            NativeMethods.WTSDisconnectSession(IntPtr.Zero, (int)ownerexplorer.SessionId, true);
                        }
                    }
                }
                if (unattendedclient.enabled)
                {
                    if (server != null && server.singleuser)
                    {
                        Log.Information("GetOwnerExplorer");
                        System.Diagnostics.Process ownerexplorer = RobotUserSession.GetOwnerExplorer(unattendedclient);
                        int sessionid = -1;
                        if (ownerexplorer != null) sessionid = ownerexplorer.SessionId;
                        var procs = System.Diagnostics.Process.GetProcessesByName("explorer");
                        foreach (var explorer in procs)
                        {
                            if (explorer.SessionId != sessionid)
                            {
                                if (server.logoff || session.client.autosignout)
                                {
                                    Log.Information("WTSLogoffSession " + explorer.SessionId);
                                    NativeMethods.WTSLogoffSession(IntPtr.Zero, (int)explorer.SessionId, true);
                                }
                                else
                                {
                                    Log.Information("WTSDisconnectSession " + explorer.SessionId);
                                    NativeMethods.WTSDisconnectSession(IntPtr.Zero, (int)explorer.SessionId, true);
                                }
                            }
                        }
                    }
                    else if (server == null) Log.Information("server is null!!!");

                    if (session != null)
                    {
                        Log.Information("Updating session for " + unattendedclient.windowsusername);
                        session.client = unattendedclient;
                        if (session.rdp == null && session.freerdp == null) session.BeginWork();
                    }
                    else
                    {
                        Log.Information("Adding session for " + unattendedclient.windowsusername);
                        sessions.Add(new RobotUserSession(unattendedclient, server));
                    }
                }
                cleanup();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
            }
        }
        private static void cleanup()
        {
            var sessioncount = sessions.Count();
            foreach (var session in sessions.ToList())
            {
                if (session.client != null && !string.IsNullOrEmpty(session.client._id) && !session.client.enabled)
                {
                    Log.Information("cleanup::Removing session for " + session.client.windowsusername);
                    sessions.Remove(session);
                    session.Dispose();
                }
            }
            if (sessioncount != sessions.Count())
            {
                Log.Information("Currently have " + sessions.Count() + " sessions");
            }
        }
        private static bool disabledmessageshown = false;
        private static unattendedserver server;
        private static async Task ReloadConfig()
        {
            try
            {
                string computername = NativeMethods.GetHostName().ToLower();
                string computerfqdn = NativeMethods.GetFQDN().ToLower();

                var servers = await global.webSocketClient.Query<unattendedserver>("openrpa", "{'_type':'unattendedserver', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
                server = servers.FirstOrDefault();

                unattendedclient[] clients = new unattendedclient[] { };
                if (server != null && server.enabled)
                {
                    disabledmessageshown = false;
                    clients = await global.webSocketClient.Query<unattendedclient>("openrpa", "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
                }
                else if (disabledmessageshown == false)
                {
                    Log.Information("No server for " + computerfqdn + " found, or server is disabled");
                    disabledmessageshown = true;
                }
                var sessioncount = sessions.Count();
                foreach (var c in clients)
                {
                    RobotUserSession session = null;
                    if (sessions != null) session = sessions.Where(x => x.client.windowsusername == c.windowsusername).FirstOrDefault();
                    if (session == null)
                    {
                        if (c.enabled)
                        {
                            UnattendedclientUpdated(c);
                        }
                    }
                    else
                    {
                        if (c._modified != session.client._modified || c._version != session.client._version)
                        {
                            UnattendedclientUpdated(c);
                        }
                    }
                }
                cleanup();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static List<RobotUserSession> sessions = new List<RobotUserSession>();
        private static bool InitializeOTEL()
        {
            return true;
        }
        private static void DoWork()
        {
            try
            {
                Console.WriteLine("Do work!");
                log("BEGIN::Set ProjectsDirectory");
                // Don't mess with ProjectsDirectory if we need to reauth
                if (args.Length == 0) Log.ResetLogPath(logpath);

                log("Set UnhandledException");
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                System.Threading.Thread.Sleep(1000 * StartupWaitSeconds);
                _ = PluginConfig.reloadinterval;
                _ = PluginConfig.jwt;
                _ = PluginConfig.wsurl;
                _ = PluginConfig.width;
                _ = PluginConfig.height;
                _ = PluginConfig.height;
                if (args.Length != 0)
                {
                    try
                    {
                        log("Get usefreerdp");
                        if (PluginConfig.usefreerdp)
                        {
                            log("Init Freerdp");
                            using (var rdp = new FreeRDP.Core.RDP())
                            {
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed initilizing FreeRDP, is Visual C++ Runtime installed ?");
                        // Console.WriteLine("https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads");
                        Console.WriteLine("https://www.microsoft.com/en-us/download/details.aspx?id=40784");
                        return;
                    }
                }
                if (args.Length == 0)
                {
                    log("Check IsServiceInstalled");
                    if (!manager.IsServiceInstalled)
                    {
                        log("InstallService");
                        manager.InstallService(typeof(Program), new string[] { });
                    }
                }
                if (args.Length > 0)
                {
                    if (args[0].ToLower() == "auth" || args[0].ToLower() == "reauth")
                    {
                        if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                        {
                            Log.Information("Saving temporart jwt token, from local settings.json");
                            PluginConfig.tempjwt = new System.Net.NetworkCredential(string.Empty, Config.local.UnprotectString(Config.local.jwt)).Password;
                            PluginConfig.wsurl = Config.local.wsurl;
                            PluginConfig.globallocal.wsurl = Config.local.wsurl;
                            _ = PluginConfig.entropy;
                            _ = PluginConfig.reloadinterval;
                            _ = PluginConfig.usefreerdp;
                            _ = PluginConfig.height;
                            _ = PluginConfig.width;
                            _ = PluginConfig.usefreerdp;
                            PluginConfig.Save();
                        }
                        return;
                    }
                    else if (args[0].ToLower() == "uninstall" || args[0].ToLower() == "u")
                    {
                        if (manager.IsServiceInstalled)
                        {
                            manager.UninstallService(typeof(Program));
                        }

                        var asm = System.Reflection.Assembly.GetEntryAssembly();
                        var filepath = asm.CodeBase.Replace("file:///", "");
                        var exepath = System.IO.Path.GetDirectoryName(filepath);
                        if (System.IO.File.Exists(System.IO.Path.Combine(exepath, "OpenRPA.RDServiceMonitor.exe")))
                        {
                            var process = System.Diagnostics.Process.Start(System.IO.Path.Combine(exepath, "OpenRPA.RDServiceMonitor.exe"), "uninstall");
                            process.WaitForExit();
                        }
                        return;
                    }
                    else if (args[0].ToLower() == "service" || args[0].ToLower() == "s")
                    {
                    }
                    else
                    {
                        Console.WriteLine("unknown command " + args[0]);
                        Console.WriteLine("try uninstall or reauth ");
                        return;
                    }

                }

                if(!System.IO.File.Exists(PluginConfig.configfile))
                {
                    log("config file missing. I just created one for you! Please update this now. using openrpa.rdservice reauth");
                    log(PluginConfig.configfile);
                    return;
                }


                log("Create Tracing");
                tracing = new Tracing(Console.Out);
                log("Add Tracing");
                System.Diagnostics.Trace.Listeners.Add(tracing);
                log("Override SetOut");
                Console.SetOut(new ConsoleDecorator(Console.Out));
                log("Override SetError");
                Console.SetError(new ConsoleDecorator(Console.Out, true));
                log("ResetLogPath");
                Log.ResetLogPath(logpath);
                Console.WriteLine("****** BEGIN");
                var wsurl = PluginConfig.globallocal.wsurl;
                if (string.IsNullOrEmpty(wsurl)) wsurl = PluginConfig.wsurl;
                if (string.IsNullOrEmpty(wsurl))
                {
                    log("wsurl is empty. Please validate config file and run openrpa.rdservice reauth");
                    log(PluginConfig.configfile);
                    return;
                }
                if(string.IsNullOrEmpty(PluginConfig.tempjwt) && (PluginConfig.jwt == null || PluginConfig.jwt.Length == 0)) {
                    log("settings file missing JWT token! please run openrpa.rdservice reauth");
                    log(PluginConfig.configfile);
                    return;
                }
                Task.Run(async () =>
                {
                    try
                    {
                        
                        Console.WriteLine("Connect to " + wsurl);
                        global.webSocketClient = WebSocketClient.Get(wsurl);
                        global.webSocketClient.OnOpen += WebSocketClient_OnOpen;
                        global.webSocketClient.OnClose += WebSocketClient_OnClose;
                        global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                        await global.webSocketClient.Connect();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
                // NativeMethods.AllocConsole();
                // if (System.Diagnostics.Debugger.IsAttached && !isService)
                if (!Monitormanager.IsServiceInstalled)
                {
                    var asm = System.Reflection.Assembly.GetEntryAssembly();
                    var filepath = asm.CodeBase.Replace("file:///", "");
                    var exepath = System.IO.Path.GetDirectoryName(filepath);
                    if (System.IO.File.Exists(System.IO.Path.Combine(exepath, "OpenRPA.RDServiceMonitor.exe")))
                    {
                        var process = System.Diagnostics.Process.Start(System.IO.Path.Combine(exepath, "OpenRPA.RDServiceMonitor.exe"));
                        process.WaitForExit();
                    }
                }
                if (!isService)
                {
                    if (args.Length > 0 && (args[0].ToLower() == "service" || args[0].ToLower() == "s"))
                    {
                        isService = true;
                    }
                    Log.Information("******************************");
                    Log.Information("* Done                       *");
                    Log.Information("******************************");
                    Console.ReadLine();
                }
                else
                {
                    if (Monitormanager.IsServiceInstalled)
                    {
                        _ = Monitormanager.StartService();
                    }
                    while (MyServiceBase.isRunning)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                    if (Monitormanager.IsServiceInstalled)
                    {
                        // _ = Monitormanager.StopService();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
