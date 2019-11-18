using OpenRPA.Interfaces.entity;
using OpenRPA.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    using OpenRPA.Interfaces;
    using System.IO.Pipes;

    class Program
    {
        public const int StartupWaitSeconds = 0;
        public const string ServiceName = "OpenRPA";
        // public const string ServiceName = "rdptest";
        public static bool isService = false;
        private static Tracing tracing = null;
        private static System.Threading.Thread uithread;


        const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        static void Main(string[] args)
        {
            //            SeServiceLogonRight
            //SeTcbName
            //SeSystemtimePrivilege
            //SeDebugName
            //var _username = NativeMethods.GetProcessUserName();
            //long result = -1;
            //TokPriv1Luid tp = new TokPriv1Luid();
            //tp.Count = 1;
            //tp.Luid = 0;
            //if (!LookupPrivilegeValue(null, SE_INCREASE_QUOTA_NAME, ref tp.Luid))
            //{
            //    Console.WriteLine("LookupPrivilegeValue failed: " + System.Runtime.InteropServices.Marshal.GetLastWin32Error());
            //}

            //result = LsaUtility.SetRight(_username, "SeServiceLogonRight");
            //result = LsaUtility.SetRight(_username, "SeTcbName");
            //result = LsaUtility.SetRight(_username, "SeSystemtimePrivilege");
            //result = LsaUtility.SetRight(_username, "SeDebugName");

            //result = LsaUtility.SetRight(_username, "SeTcbName");
            //Console.WriteLine("SeTcbName: " + result);
            //result = LsaUtility.SetRight(_username, "SeIncreaseQuotaPrivilege");
            //Console.WriteLine("SeIncreaseQuotaPrivilege: " + result);
            //result = LsaUtility.SetRight(_username, "SeTcbPrivilege");
            //Console.WriteLine("SeTcbPrivilege: " + result);



            try
            {
                if(PluginConfig.usefreerdp)
                {
                    var rdp = new FreeRDP.Core.RDP();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed initilizing FreeRDP, is Visual C++ Runtime installed ?");
                // Console.WriteLine("https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads");
                Console.WriteLine("https://www.microsoft.com/en-us/download/details.aspx?id=40784");
                return;
            }
            //uithread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            //{
            //    var form = new System.Windows.Forms.Form();
            //    System.Windows.Forms.Application.Run(form);
            //}));
            //uithread.IsBackground = true;
            //uithread.Start();

            var parentProcess = NativeMethods.GetParentProcessId();
            isService = (parentProcess.ProcessName.ToLower() == "services");
            if (args.Length == 0)
            {
                System.Threading.Thread.Sleep(1000 * StartupWaitSeconds);
                // WindowStationAndDesktop.CreateWindowStation("LupoWinStation");

                //Application.EnableVisualStyles();
                //Application.Run(f);
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
                    manager.InstallService(typeof(Program), new string[] {  });
                }
            }

            tracing = new Tracing(Console.Out);
            System.Diagnostics.Trace.Listeners.Add(tracing);
            Console.SetOut(new ConsoleDecorator(Console.Out));
            Console.SetError(new ConsoleDecorator(Console.Out, true));

            if (args.Length > 0)
            {
                if (args[0].ToLower() == "auth" || args[0].ToLower() == "reauth")
                {
                    if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                    {
                        Log.Information("Saving temporart jwt token, from local settings.json");
                        PluginConfig.tempjwt = new System.Net.NetworkCredential(string.Empty, Config.local.UnprotectString(Config.local.jwt)).Password;
                        Config.Save();
                    }
                    return;
                }
                else if (args[0].ToLower() == "uninstall" || args[0].ToLower() == "u")
                {
                    if (manager.IsServiceInstalled)
                    {
                        manager.UninstallService(typeof(Program));
                    }
                    return;
                }
            }
            if (isService)
            {
                System.ServiceProcess.ServiceBase.Run(new MyServiceBase(ServiceName, DoWork));
            } 
            else
            {
                DoWork();
            }
        }
        private static void WebSocketClient_OnQueueMessage(Interfaces.IQueueMessage message, Interfaces.QueueMessageEventArgs e)
        {
            Log.Debug("WebSocketClient_OnQueueMessage");
        }
        private static void WebSocketClient_OnClose(string obj)
        {
            Log.Information("WebSocketClient_OnClose");
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
            Log.Information("WebSocketClient_OnOpen");
            TokenUser user = null;
            while (user == null)
            {
                if (!string.IsNullOrEmpty(PluginConfig.tempjwt) )
                {
                    user = await global.webSocketClient.Signin(PluginConfig.tempjwt); if (user != null)
                    {
                        if(isService)
                        {
                            PluginConfig.jwt = Base64Encode(PluginConfig.ProtectString(PluginConfig.tempjwt));
                            PluginConfig.tempjwt = null;
                            Config.Save();
                        }
                        Log.Information("Signed in as " + user.username);
                    }
                }
                else if (PluginConfig.jwt != null && PluginConfig.jwt.Length > 0)
                {
                    user = await global.webSocketClient.Signin(PluginConfig.UnprotectString(Base64Decode(PluginConfig.jwt))); if (user != null)
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
            string computername = NativeMethods.GetHostName().ToLower();
            string computerfqdn = NativeMethods.GetFQDN().ToLower();
            var servers = await global.webSocketClient.Query<unattendedserver>("openrpa", "{'_type':'unattendedserver', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
            unattendedserver server = servers.FirstOrDefault();
            if (servers.Length == 0)
            {
                Log.Information("Adding new unattendedserver for " + computerfqdn);
                server = new unattendedserver() { computername = computername, computerfqdn = computerfqdn, name = computerfqdn };
                server = await global.webSocketClient.InsertOne("openrpa", 1, false, server);
            }
            var clients = await global.webSocketClient.Query<unattendedclient>("openrpa", "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
            foreach (var c in clients) sessions.Add(new RobotUserSession(c));
            Log.Information("Loaded " + sessions.Count + " sessions");
            // Create listener for robots to connect too
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, System.Security.AccessControl.AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
            pipe = new OpenRPA.NamedPipeWrapper.NamedPipeServer<RPAMessage>("openrpa_service", ps);
            pipe.ClientConnected += Pipe_ClientConnected;
            pipe.ClientMessage += Pipe_ClientMessage;
            pipe.Start();
        }
        private static async Task ReloadConfig()
        {
            string computername = NativeMethods.GetHostName().ToLower();
            string computerfqdn = NativeMethods.GetFQDN().ToLower();
            //var servers = await global.webSocketClient.Query<unattendedserver>("openrpa", "{'_type':'unattendedserver', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
            //unattendedserver server = servers.FirstOrDefault();
            //if (servers.Length == 0)
            //{
            //    server = new unattendedserver() { computername = computername, computerfqdn = computerfqdn, name = computerfqdn };
            //    server = await global.webSocketClient.InsertOne("openrpa", 1, false, server);
            //}
            var clients = await global.webSocketClient.Query<unattendedclient>("openrpa", "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
            //foreach (var s in sessions.ToList()) { s.Dispose(); sessions.Remove(s); }
            //foreach (var c in clients) sessions.Add(new RobotUserSession(c));
            foreach (var c in clients)
            {
                var session = sessions.Where(x => x.client.windowsusername == c.windowsusername).FirstOrDefault();
                if (session == null)
                {
                    Log.Information("Adding session for " + c.windowsusername);
                    sessions.Add(new RobotUserSession(c));
                } else
                {
                    if (c._modified != session.client._modified)
                    {
                        Log.Information("Removing session for " + session.client.windowsusername);
                        sessions.Remove(session);
                        session.Dispose();
                        session = null;
                        Log.Information("Adding session for " + c.windowsusername);
                        sessions.Add(new RobotUserSession(c));
                    }
                }
            }
            foreach(var session in sessions.ToList())
            {
                var c = clients.Where(x => x.windowsusername == session.client.windowsusername).FirstOrDefault();
                if (c == null)
                {
                    Log.Information("Removing session for " + session.client.windowsusername);
                    sessions.Remove(session);
                    session.Dispose();
                }
            }
            Log.Information("Loaded " + sessions.Count + " sessions");

        }
        public static List<RobotUserSession> sessions = new List<RobotUserSession>();
        private static void Pipe_ClientConnected(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection)
        {
            Log.Information("Client connected!");
        }
        private static async void Pipe_ClientMessage(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection, RPAMessage message)
        {
            if (message.command == "pong") return;
            if (message.command == "hello")
            {
                var windowsusername = message.windowsusername.ToLower();
                var session = sessions.Where(x => x.client.windowsusername == windowsusername).FirstOrDefault();
                if(session== null)
                {
                    //Log.Information("Adding new unattendedclient for " + windowsusername);
                    string computername = NativeMethods.GetHostName().ToLower();
                    string computerfqdn = NativeMethods.GetFQDN().ToLower();
                    var client = new unattendedclient() { computername = computername, computerfqdn = computerfqdn, windowsusername = windowsusername, name = computername + " " + windowsusername, openrpapath = message.openrpapath };
                    // client = await global.webSocketClient.InsertOne("openrpa", 1, false, client);
                    session = new RobotUserSession(client);
                    sessions.Add(session);
                }
                if(session.client!=null)
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
        public static OpenRPA.NamedPipeWrapper.NamedPipeServer<RPAMessage> pipe = null;
        private static ServiceManager manager = new ServiceManager(ServiceName);
        private static void DoWork()
        {
            // Connect to OpenFlow
            Task.Run(() => {
                global.webSocketClient = new WebSocketClient(Config.local.wsurl);
                global.webSocketClient.OnOpen += WebSocketClient_OnOpen;
                global.webSocketClient.OnClose += WebSocketClient_OnClose;
                global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                _ = global.webSocketClient.Connect();
            });
            // NativeMethods.AllocConsole();
            // if (System.Diagnostics.Debugger.IsAttached && !isService)
            if (!isService)
            {
                Log.Information("******************************");
                Log.Information("* Done                       *");
                Log.Information("******************************");
                Console.ReadLine();
            } else
            {
                while (MyServiceBase.isRunning)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
