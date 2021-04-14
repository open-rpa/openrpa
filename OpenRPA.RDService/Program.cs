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
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
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
                Console.WriteLine(ex.ToString());
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



                Console.WriteLine("main 1");
                log("GetParentProcessId");
                Console.WriteLine("main 200");
                var parentProcess = NativeMethods.GetParentProcessId();
                log("Check parentProcess");
                Console.WriteLine("main 5");
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
            Log.Error(ex, "");
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
            openrpa_watchid = "";
            await Task.Delay(1000);
            if (autoReconnect)
            {
                autoReconnect = false;
                try
                {
                    global.webSocketClient.OnOpen -= WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose -= WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage -= WebSocketClient_OnQueueMessage;
                    global.webSocketClient = null;

                    global.webSocketClient = new WebSocketClient(PluginConfig.wsurl);
                    global.webSocketClient.OnOpen += WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
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
        private static string openrpa_watchid;
        private static async void WebSocketClient_OnOpen()
        {
            var span = source.StartActivity("WebSocketClient_OnOpen");
            try
            {
                InitializeOTEL();
                Log.Information("WebSocketClient_OnOpen");
                TokenUser user = null;
                while (user == null)
                {
                    if (!string.IsNullOrEmpty(PluginConfig.tempjwt))
                    {
                        span?.AddEvent(new System.Diagnostics.ActivityEvent("Try signin using temp token"));
                        user = await global.webSocketClient.Signin(PluginConfig.tempjwt, "RDService", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()); 
                        if (user != null)
                        {
                            if (isService)
                            {
                                PluginConfig.jwt = Base64Encode(PluginConfig.ProtectString(PluginConfig.tempjwt));
                                PluginConfig.tempjwt = null;
                                PluginConfig.Save();
                            }
                            span?.AddEvent(new System.Diagnostics.ActivityEvent("Signed in as " + user.username));
                            Log.Information("Signed in as " + user.username);
                        }
                    }
                    else if (PluginConfig.jwt != null && PluginConfig.jwt.Length > 0)
                    {
                        span?.AddEvent(new System.Diagnostics.ActivityEvent("Try signin using token"));
                        user = await global.webSocketClient.Signin(PluginConfig.UnprotectString(Base64Decode(PluginConfig.jwt)), "RDService", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()); 
                        if (user != null)
                        {
                            span?.AddEvent(new System.Diagnostics.ActivityEvent("Signed in as " + user.username));
                            Log.Information("Signed in as " + user.username);
                        }
                    }
                    else
                    {
                        span?.AddEvent(new System.Diagnostics.ActivityEvent("Missing jwt from config, close down"));
                        Log.Error("Missing jwt from config, close down");
                        _ = global.webSocketClient.Close();
                        if (isService) await manager.StopService();
                        if (!isService) Environment.Exit(0);
                        return;
                    }
                }
                string computername = NativeMethods.GetHostName().ToLower();
                string computerfqdn = NativeMethods.GetFQDN().ToLower();
                var servers = await global.webSocketClient.Query<unattendedserver>("openrpa", "{'_type':'unattendedserver', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
                server = servers.FirstOrDefault();
                if (servers.Length == 0)
                {
                    span?.AddEvent(new System.Diagnostics.ActivityEvent("Adding new unattendedserver for " + computerfqdn));
                    Log.Information("Adding new unattendedserver for " + computerfqdn);
                    server = new unattendedserver() { computername = computername, computerfqdn = computerfqdn, name = computerfqdn, enabled = true };
                    server = await global.webSocketClient.InsertOne("openrpa", 1, false, server);
                }
                //var clients = await global.webSocketClient.Query<unattendedclient>("openrpa", "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
                //foreach (var c in clients) sessions.Add(new RobotUserSession(c));
                // Log.Information("Loaded " + sessions.Count + " sessions");
                // Create listener for robots to connect too
                if(pipe==null)
                {
                    span?.AddEvent(new System.Diagnostics.ActivityEvent("Creating pipe service for robots"));
                    PipeSecurity ps = new PipeSecurity();
                    ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, System.Security.AccessControl.AccessControlType.Allow));
                    ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
                    ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
                    pipe = new OpenRPA.NamedPipeWrapper.NamedPipeServer<RPAMessage>("openrpa_service", ps);
                    pipe.ClientConnected += Pipe_ClientConnected;
                    pipe.ClientMessage += Pipe_ClientMessage;
                    pipe.Start();
                }

                if (global.openflowconfig.supports_watch)
                {
                    if (string.IsNullOrEmpty(openrpa_watchid))
                    {
                        span?.AddEvent(new System.Diagnostics.ActivityEvent("Add database watch"));
                        // "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}"
                        // openrpa_watchid = await global.webSocketClient.Watch("openrpa", "[{ '$match': { 'fullDocument._type': {'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'} } }]", onWatchEvent);
                        openrpa_watchid = await global.webSocketClient.Watch("openrpa", "[{ '$match': {'fullDocument.computername':'" + computername + "', 'fullDocument.computerfqdn':'" + computerfqdn + "'} }]", onWatchEvent);

                        // openrpa_watchid = await global.webSocketClient.Watch("openrpa", "[]", onWatchEvent);
                        await ReloadConfig();
                    }
                } 
                else
                {
                    if (reloadTimer == null)
                    {
                        span?.AddEvent(new System.Diagnostics.ActivityEvent("Add reload timer"));
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
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                span?.RecordException(ex);
            }
            finally
            {
                span?.Dispose();
            }
        }
        private static void onWatchEvent(string id, Newtonsoft.Json.Linq.JObject data)
        {
            var span = source.StartActivity("onWatchEvent");
            Log.Information("onWatchEvent");
            try
            {
                string _type = data["fullDocument"].Value<string>("_type");
                span?.AddTag("_type", _type);
                string _id = data["fullDocument"].Value<string>("_id");
                span?.AddTag("_id", _id);
                long _version = data["fullDocument"].Value<long>("_version");
                span?.AddTag("_version", _version);
                string operationType = data.Value<string>("operationType");
                span?.AddTag("operationType", operationType);
                Log.Information("operationType: " + operationType);
                if (operationType != "replace" && operationType != "insert" && operationType != "update") return; // we don't support delete right now
                if (_type == "unattendedclient")
                {
                    span?.AddEvent(new System.Diagnostics.ActivityEvent("DeserializeObject"));
                    var unattendedclient = Newtonsoft.Json.JsonConvert.DeserializeObject<unattendedclient>(data["fullDocument"].ToString());
                    if(unattendedclient != null && unattendedclient.computerfqdn == server.computerfqdn && unattendedclient.computername == server.computername)
                    {
                        UnattendedclientUpdated(unattendedclient);
                    } else if(unattendedclient==null)
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
                span?.RecordException(ex);
                Log.Error(ex.ToString());
            }
            span?.Dispose();
        }
        private static void UnattendedclientUpdated(unattendedclient unattendedclient)
        {
            if (unattendedclient == null) return;
            var span = source.StartActivity("UnattendedclientUpdated for " + unattendedclient.windowsusername);
            try
            {
                span?.AddTag("windowsusername", unattendedclient.windowsusername);
                span?.AddTag("enabled", unattendedclient.enabled);
                RobotUserSession session = null;
                if (sessions != null) session = sessions.Where(x => x.client._id == unattendedclient._id).FirstOrDefault();
                if (!unattendedclient.enabled)
                {
                    if (session != null)
                    {
                        //if(session.client.autosignout)
                        //{
                        //    span?.AddEvent(new System.Diagnostics.ActivityEvent("Send Signout signal"));
                        //    _ = session.SendSignout();
                        //}
                        if (session.rdp != null || session.freerdp != null)
                        {
                            Log.Information("disconnecting session for " + session.client.windowsusername);
                            span?.AddEvent(new System.Diagnostics.ActivityEvent("disconnecting session for " + session.client.windowsusername));
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
                    span?.AddEvent(new System.Diagnostics.ActivityEvent("GetOwnerExplorer"));
                    Log.Information("GetOwnerExplorer");
                    System.Diagnostics.Process ownerexplorer = RobotUserSession.GetOwnerExplorer(unattendedclient);
                    if (ownerexplorer != null)
                    {
                        if(server.logoff)
                        {
                            span?.AddEvent(new System.Diagnostics.ActivityEvent("WTSLogoffSession " + ownerexplorer.SessionId));
                            Log.Information("WTSLogoffSession " + ownerexplorer.SessionId);
                            NativeMethods.WTSLogoffSession(IntPtr.Zero, (int)ownerexplorer.SessionId, true);
                        } else
                        {
                            span?.AddEvent(new System.Diagnostics.ActivityEvent("WTSDisconnectSession " + ownerexplorer.SessionId));
                            Log.Information("WTSDisconnectSession " + ownerexplorer.SessionId);
                            NativeMethods.WTSDisconnectSession(IntPtr.Zero, (int)ownerexplorer.SessionId, true);
                        }
                    }
                }
                if (unattendedclient.enabled)
                {
                    if (server != null && server.singleuser)
                    {
                        span?.AddEvent(new System.Diagnostics.ActivityEvent("GetOwnerExplorer"));
                        Log.Information("GetOwnerExplorer");
                        System.Diagnostics.Process ownerexplorer = RobotUserSession.GetOwnerExplorer(unattendedclient);
                        int sessionid = -1;
                        if (ownerexplorer != null) sessionid = ownerexplorer.SessionId;
                        var procs = System.Diagnostics.Process.GetProcessesByName("explorer");
                        foreach (var explorer in procs)
                        {
                            if (explorer.SessionId != sessionid)
                            {
                                if (server.logoff)
                                {
                                    span?.AddEvent(new System.Diagnostics.ActivityEvent("WTSLogoffSession " + explorer.SessionId));
                                    Log.Information("WTSLogoffSession " + explorer.SessionId);
                                    NativeMethods.WTSLogoffSession(IntPtr.Zero, (int)explorer.SessionId, true);
                                }
                                else
                                {
                                    span?.AddEvent(new System.Diagnostics.ActivityEvent("WTSDisconnectSession " + explorer.SessionId));
                                    Log.Information("WTSDisconnectSession " + explorer.SessionId);
                                    NativeMethods.WTSDisconnectSession(IntPtr.Zero, (int)explorer.SessionId, true);
                                }
                            }
                        }
                    }
                    else if (server == null) Log.Information("server is null!!!");

                    if (session != null)
                    {
                        span?.AddEvent(new System.Diagnostics.ActivityEvent("Updating session for " + unattendedclient.windowsusername));
                        Log.Information("Updating session for " + unattendedclient.windowsusername);
                        session.client = unattendedclient;
                        if (session.rdp == null && session.freerdp == null) session.BeginWork();
                    }
                    else
                    {
                        span?.AddEvent(new System.Diagnostics.ActivityEvent("Adding session for " + unattendedclient.windowsusername));
                        Log.Information("Adding session for " + unattendedclient.windowsusername);
                        sessions.Add(new RobotUserSession(unattendedclient));
                    }
                }
                cleanup();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                span?.RecordException(ex);
            }
            finally
            {
                span?.Dispose();
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
                if(server != null && server.enabled)
                {
                    disabledmessageshown = false;
                    clients = await global.webSocketClient.Query<unattendedclient>("openrpa", "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
                } else if (disabledmessageshown == false)
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
                        if(c.enabled)
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
        private static void Pipe_ClientConnected(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection)
        {
            Log.Information("Client connected!");
        }
        private static async void Pipe_ClientMessage(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection, RPAMessage message)
        {
            try
            {
                if (message.command == "pong") return;
                if (message.command == "hello")
                {
                    var windowsusername = message.windowsusername.ToLower();
                    var session = sessions.Where(x => x.client.windowsusername == windowsusername).FirstOrDefault();
                    if (session == null)
                    {
                        //Log.Information("Adding new unattendedclient for " + windowsusername);
                        string computername = NativeMethods.GetHostName().ToLower();
                        string computerfqdn = NativeMethods.GetFQDN().ToLower();
                        var client = new unattendedclient() { computername = computername, computerfqdn = computerfqdn, windowsusername = windowsusername, name = computername + " " + windowsusername, openrpapath = message.openrpapath };
                        // client = await global.webSocketClient.InsertOne("openrpa", 1, false, client);
                        session = new RobotUserSession(client);
                        sessions.Add(session);
                    }
                    if (session.client != null)
                    {
                        session.client.openrpapath = message.openrpapath;
                        session.AddConnection(connection);
                    }
                }
                if (message.command == "reloadconfig")
                {
                    await ReloadConfig();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static OpenRPA.NamedPipeWrapper.NamedPipeServer<RPAMessage> pipe = null;
        private static TracerProvider StatsTracerProvider;
        private static TracerProvider tracerProvider;
        public static System.Diagnostics.ActivitySource source = new System.Diagnostics.ActivitySource("OpenRPA.RDService");
        private static bool InitializeOTEL()
        {
            var span = source.StartActivity("InitializeOTEL");
            try
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                span?.SetTag("enable_analytics", Config.local.enable_analytics);
                if (Config.local.enable_analytics && StatsTracerProvider == null)
                {
                    span?.AddEvent(new System.Diagnostics.ActivityEvent("Adding listener for analytics"));
                    StatsTracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource("OpenRPA").AddSource("OpenRPA.RobotInstance").AddSource("OpenRPA.Net").AddSource("OpenRPA.RDService")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OpenRPA.RDService"))
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri("https://otel.stats.openiap.io");
                    })
                    .Build();
                }
                span?.SetTag("otel_trace_url", Config.local.otel_trace_url);
                if (!string.IsNullOrEmpty(Config.local.otel_trace_url) && tracerProvider == null)
                {
                    span?.AddEvent(new System.Diagnostics.ActivityEvent("Adding listener for otel"));
                    tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource("OpenRPA").AddSource("OpenRPA.RobotInstance").AddSource("OpenRPA.Net").AddSource("OpenRPA.RDService")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OpenRPA.RDService"))
                    .AddOtlpExporter(otlpOptions =>
                    {
                        if (Config.local.otel_trace_url.Contains("http://") && Config.local.otel_trace_url.Contains(":80"))
                        {
                            Config.local.otel_trace_url = Config.local.otel_trace_url.Replace("http://", "https://").Replace(":80", "");
                        }
                        otlpOptions.Endpoint = new Uri(Config.local.otel_trace_url);
                    })
                    .Build();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                span?.Dispose();
            }
            return false;
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
                    // System.Threading.Thread.Sleep(1000 * StartupWaitSeconds);
                    if (!manager.IsServiceInstalled)
                    {
                        //Console.Write("Username (" + NativeMethods.GetProcessUserName() + "): ");
                        //var username = Console.ReadLine();
                        //if (string.IsNullOrEmpty(username)) username = NativeMethods.GetProcessUserName();
                        //Console.Write("Password: ");
                        //string pass = "";
                        //do
                        //{
                        //    ConsoleKeyInfo key = Console.ReadKey(true);
                        //    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                        //    {
                        //        pass += key.KeyChar;
                        //        Console.Write("*");
                        //    }
                        //    else
                        //    {
                        //        if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                        //        {
                        //            pass = pass.Substring(0, (pass.Length - 1));
                        //            Console.Write("\b \b");
                        //        }
                        //        else if (key.Key == ConsoleKey.Enter)
                        //        {
                        //            break;
                        //        }
                        //    }
                        //} while (true);
                        //manager.InstallService(typeof(Program), new string[] { "username=" + username, "password=" + pass });
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
                
                Task.Run(async () => {
                    try
                    {
                        Console.WriteLine("Connect to " + PluginConfig.wsurl);
                        global.webSocketClient = new WebSocketClient(PluginConfig.wsurl);
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
                    if(Monitormanager.IsServiceInstalled)
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
