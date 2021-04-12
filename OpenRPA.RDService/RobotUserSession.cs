using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    using OpenRPA.Interfaces;
    using SimpleImpersonation;
    using System.Windows.Threading;

    public class RobotUserSession : IDisposable
    {
        public RobotUserSession(unattendedclient client)
        {
            this.client = client;
            BeginWork();
        }
        public void BeginWork()
        {
            if (cancellationTokenSource != null )
            {
                if(!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new System.Threading.CancellationTokenSource(); ;
            }
            var t = Task.Factory.StartNew(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        lock (mylock)
                        {
                            DoWork();
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }, cancellationTokenSource.Token).ContinueWith(task =>
            {
                try
                {
                    if (!task.IsCompleted || task.IsFaulted)
                    {
                        if (task.Exception != null) Log.Error(task.Exception.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }, cancellationTokenSource.Token);
        }
        private System.Threading.CancellationTokenSource cancellationTokenSource = new System.Threading.CancellationTokenSource();
        public Client rdp;
        public FreeRDP.Core.RDP freerdp;
        public unattendedclient client;
        public NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection;
        private DateTime created = DateTime.Now;
        private DateTime lastheartbeat = DateTime.Now;
        private DateTime lastrdp = DateTime.Now - TimeSpan.FromMinutes(1);
        private int ConnectionAttempts = 0;
        private bool skiprdp = false;
        private bool hasShownLaunchWarning = false;
        private static object mylock = new object();
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        public void DoWork()
        {
            try
            {
                if(System.Diagnostics.Debugger.IsAttached)
                {
                    // skiprdp = true;
                }
                if (client == null)
                {
                    Log.Debug("client is null");
                    return;
                }
                if (!client.enabled) return;
                if (string.IsNullOrEmpty(client._id))
                {
                    // Log.Debug("client._id is null, Dummy client, ignore");
                    return; // Dummy client, ignore
                }
                if (connection != null && connection.IsConnected == false) { connection = null; created = DateTime.Now; }
                // Is OpenRPA connected for this user ?
                if (connection != null)
                {
                    connection.PushMessage(new RPAMessage("ping"));
                    // created = DateTime.Now;
                    // return;
                }
                if ((DateTime.Now - created).TotalSeconds < 5) return;
                // Is user signed in ?
                // ownerexplorer = null;
                //if (ownerexplorer == null)
                var rdpip = "127.0.0.2";
                // if ((ConnectionAttempts % 2) == 1) rdpip = "127.0.0.1";
                if (PluginConfig.usefreerdp && !skiprdp)
                {
                    if (freerdp == null || freerdp.Connected == false)
                    {
                        if (freerdp == null) Log.Debug("rdp is null");
                        if (string.IsNullOrEmpty(client.windowspassword)) return;
                        lastrdp = DateTime.Now;
                    }
                    if (freerdp == null) freerdp = new FreeRDP.Core.RDP();
                    if (!freerdp.Connected)
                    {
                        var hostname = NativeMethods.GetHostName().ToLower();
                        try
                        {
                            Log.Debug("Tesing connection to " + rdpip + " port 3389");
                            using (var tcpClient = new System.Net.Sockets.TcpClient())
                            {
                                var ipAddress = System.Net.IPAddress.Parse(rdpip);
                                var ipEndPoint = new System.Net.IPEndPoint(ipAddress, 3389);
                                tcpClient.Connect(ipEndPoint);
                            }
                            Log.Debug("Success");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            return;
                        }

                        var windowsusername = client.windowsusername.Substring(client.windowsusername.IndexOf("\\") + 1);
                        var windowsdomain = client.windowsusername.Substring(0, client.windowsusername.IndexOf("\\"));
                        if(string.IsNullOrEmpty(client.windowslogin))
                        {
                            if (client.windowsusername.StartsWith(hostname + @"\"))
                            {
                                client.windowslogin = windowsusername;
                            }
                            else
                            {
                                client.windowslogin = client.windowsusername;
                            }
                        }

                        Log.Debug("Impersonate " + client.windowslogin);
                        try
                        {
                            //Log.Debug("windowsusername: " + windowsusername);
                            //Log.Debug("windowsdomain: " + windowsdomain);
                            //Log.Debug("windowspassword: " + client.windowspassword);
                            var credentials = new UserCredentials(windowsdomain, windowsusername, client.windowspassword);
                            Impersonation.RunAsUser(credentials, LogonType.Interactive, () =>
                            {
                                ConnectionAttempts++;
                                Log.Debug("Connecting RDP connection to " + rdpip + " for " + client.windowslogin);
                                freerdp.Connect(rdpip, "", client.windowslogin, client.windowspassword);

                            });
                            //using (var imp = new Impersonator(windowsusername, windowsdomain, client.windowspassword))
                            //{
                            //    ConnectionAttempts++;
                            //    Log.Debug("Connecting RDP connection to " + rdpip + " for " + client.windowslogin);
                            //    freerdp.Connect(rdpip, "", client.windowslogin, client.windowspassword);
                            //    //if (client.windowsusername.StartsWith(hostname + @"\"))
                            //    //{
                            //    //    // var windowsusername = client.windowsusername.Substring(hostname.Length + 1);
                            //    //    Log.Debug("Connecting RDP connection to " + rdpip + " for " + windowsusername);
                            //    //    freerdp.Connect(rdpip, "", windowsusername, client.windowspassword);
                            //    //}
                            //    //else
                            //    //{
                            //    //    Log.Debug("Connecting RDP connection to " + rdpip + " for " + client.windowsusername);
                            //    //    freerdp.Connect(rdpip, "", client.windowsusername, client.windowspassword);
                            //    //}
                            //}
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Login failed, waiting 10 seconds: " + ex.Message);
                            System.Threading.Thread.Sleep(10 * 1000);
                            throw;
                        }
                        created = DateTime.Now;
                        return;
                    }
                    if (freerdp == null || freerdp.Connected == false) return;
                }
                else if (!skiprdp)
                {
                    if (rdp == null || rdp.Connected == false)
                    {
                        if (rdp == null) Log.Debug("rdp is null");
                        if (string.IsNullOrEmpty(client.windowspassword)) return;
                        lastrdp = DateTime.Now;
                    }
                    if (rdp == null) rdp = new Client();
                    if (rdp.Connecting) return;
                    if (!rdp.Connected)
                    {
                        try
                        {
                            Log.Debug("Tesing connection to " + rdpip + " port 3389");
                            using (var tcpClient = new System.Net.Sockets.TcpClient())
                            {
                                var ipAddress = System.Net.IPAddress.Parse(rdpip);
                                var ipEndPoint = new System.Net.IPEndPoint(ipAddress, 3389);
                                tcpClient.Connect(ipEndPoint);
                            }
                            Log.Debug("Success");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            return;
                        }

                        Log.Debug("Increment ConnectionAttempts");
                        ConnectionAttempts++;
                        Log.Debug("Get HostName");
                        var hostname = NativeMethods.GetHostName().ToLower();
                        Log.Debug("hostname is: " + hostname);
                        Log.Debug("Connecting RDP connection to " + rdpip + " for " + client.windowslogin);
                        // Task.Run(()=>rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername.Substring(hostname.Length + 1), client.windowspassword));
                        if (string.IsNullOrEmpty(client.windowslogin))
                        {
                            if (client.windowsusername.StartsWith(hostname + @"\"))
                            {
                                var windowsusername = client.windowsusername.Substring(hostname.Length + 1);
                                client.windowslogin = windowsusername;
                            }
                            else
                            {
                                client.windowslogin = client.windowsusername;
                            }
                        }
                        rdp.CreateRdpConnectionasync(rdpip, "", client.windowslogin, client.windowspassword);
                        Log.Debug("Connection initialized");
                        //if (client.windowsusername.StartsWith(hostname + @"\"))
                        //{
                        //    var windowsusername = client.windowsusername.Substring(hostname.Length + 1);
                        //    Log.Debug("Connecting RDP connection to " + rdpip + " for " + windowsusername);
                        //    // Task.Run(()=>rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername.Substring(hostname.Length + 1), client.windowspassword));
                        //    rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername.Substring(hostname.Length + 1), client.windowspassword);
                        //    Log.Debug("Connection initialized");
                        //}
                        //else
                        //{
                        //    Log.Debug("Connecting RDP connection to " + rdpip + " for " + client.windowsusername);
                        //    // Task.Run(() => rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername, client.windowspassword));
                        //    rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername, client.windowspassword);
                        //    Log.Debug("Connection initialized");
                        //}
                        created = DateTime.Now;
                    }
                    if (rdp == null) {
                        Log.Debug("rdp is null, exit");
                        return; 
                    }
                    if (rdp.Connected == false)
                    {
                        Log.Debug("rdp.Connected is false, exit");
                        return;
                    }
                }

                Log.Debug("EnableDisablePrivilege's");
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_ASSIGNPRIMARYTOKEN_NAME), true);
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_BACKUP_NAME), true);
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_DEBUG_NAME), true);
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_LOAD_DRIVER_NAME), true);
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_TCB_NAME), true);
                Log.Debug("get explorer process'");
                Log.Debug("windowsusername: " + client.windowsusername);
                Log.Debug("windowslogin: " + client.windowslogin);
                    try
                {
                    System.Diagnostics.Process ownerexplorer = GetOwnerExplorer();
                    if (ownerexplorer == null)
                    {
                        Log.Debug("ownerexplorer is null, exit");
                        return;
                    }
                    Log.Debug("get openrpa process'");
                    System.Diagnostics.Process ownerrpa = null;
                    var procs = Process.GetProcessesByName("openrpa");
                    foreach (var rpa in procs)
                    {
                        try
                        {
                            var owner = NativeMethods.GetProcessUserName(rpa.Id).ToLower();
                            if (owner == client.windowsusername || owner == client.windowslogin)
                            {
                                Log.Debug("Found openrpa process for " + owner);
                                ownerrpa = rpa;
                            } else
                            {
                                Log.Debug("skip openrpa process for " + owner);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            created = DateTime.Now;
                            return;
                        }
                    }
                    if (ownerrpa != null)
                    {
                        Log.Debug("ownerrpa is not null, exit");
                        //if(client.autorestart != TimeSpan.Zero && (DateTime.Now - lastheartbeat) > client.autorestart )
                        //{
                        //    try
                        //    {
                        //        lastheartbeat = DateTime.Now;
                        //        ownerrpa.Kill();
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        Log.Error(ex.ToString());
                        //    }                    
                        //}
                        return;
                    }
                    if (string.IsNullOrEmpty(client.openrpapath))
                    {
                        Log.Debug("openrpapath not set for user");
                        return;
                    }
                    if (!System.IO.File.Exists(client.openrpapath))
                    {
                        Log.Debug("openrpapath not found " + client.openrpapath);
                        return;
                    }
                    var path = System.IO.Path.GetDirectoryName(client.openrpapath);
                    //if (!Program.isService)
                    //{
                    //    Log.Debug("Not running as service, so just launching openrpa here");
                    //    Log.Debug(client.openrpapath);
                    //    created = DateTime.Now;
                    //    Process.Start(new ProcessStartInfo(client.openrpapath) { WorkingDirectory = path });
                    //    return;
                    //}
                    // IntPtr hSessionToken = IntPtr.Zero;
                    // SessionFinder sf = new SessionFinder();
                    // hSessionToken = sf.GetLocalInteractiveSession();
                    //var windowsusername = client.windowsusername.Substring(client.windowsusername.IndexOf("\\") + 1);
                    //var windowsdomain = client.windowsusername.Substring(0, client.windowsusername.IndexOf("\\") );
                    //// hSessionToken = sf.GetSessionByUser(windowsdomain, windowsusername);
                    //Impersonation.ExecuteAppAsLoggedOnUser(client.openrpapath, null, System.IO.Path.GetDirectoryName(client.openrpapath));
                    //var runner = new InteractiveProcessRunner(client.openrpapath, hSessionToken);
                    //var p = runner.Run();

                    //if (!NativeMethods.Launch(ownerexplorer, path, @"c:\windows\system32\cmd.exe /C " + "\"" + client.openrpapath + "\""))


                    //var me = System.Security.Principal.WindowsIdentity.GetCurrent();
                    ////var mep = new System.Security.Principal.WindowsPrincipal(me);
                    //var acct = new System.Security.Principal.NTAccount(me.Name);
                    //var rule = new ProcessAccessRule(acct, ProcessAccessRights.PROCESS_ALL_ACCESS, false, System.Security.AccessControl.InheritanceFlags.None, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow);

                    //SafeTokenHandle handle = new SafeTokenHandle(ownerexplorer.Handle);
                    ////var perm = new OpenRPA.Interfaces.ProcessSecurity(handle);
                    //var perm = new OpenRPA.Interfaces.ProcessSecurity(handle);
                    //perm.AddAccessRule(rule);
                    //try
                    //{
                    //    perm.SaveChanges(handle);
                    //}
                    //catch (Exception ex)
                    //{
                    //    Log.Error(new Exception("Failed setting DACL on explore proccess: " + ex.Message, ex).ToString());
                    //    created = DateTime.Now;
                    //    return;
                    //}
                    if (Program.isService)
                    {
                        Log.Debug("Attaching to user explorer and launching robot in session");
                        Log.Debug(client.openrpapath);
                        created = DateTime.Now;
                        hasShownLaunchWarning = false;
                        if (!NativeMethods.Launch(ownerexplorer, path, client.openrpapath.Replace("/", @"\")))
                        {
                            Log.Error("Failed launching robot in session");
                            string errorMessage = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()).Message;
                            Log.Error(errorMessage);
                        }
                    }
                    else if(!hasShownLaunchWarning)
                    {
                        Log.Warning("Not running as Local System, so cannot spawn processes in other users desktops");
                        created = DateTime.Now;
                        hasShownLaunchWarning = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    created = DateTime.Now;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                created = DateTime.Now;
            }
        }

        public Process GetOwnerExplorer()
        {
            return RobotUserSession.GetOwnerExplorer(client);
        }
        public static Process GetOwnerExplorer(unattendedclient client)
        {
            var procs = Process.GetProcessesByName("explorer");
            System.Diagnostics.Process ownerexplorer = null;
            foreach (var explorer in procs)
            {
                try
                {
                    var owner = NativeMethods.GetProcessUserName(explorer.Id).ToLower();
                    if (owner == client.windowsusername || owner == client.windowslogin)
                    {
                        Log.Debug("Found explorer process for " + owner);
                        ownerexplorer = explorer;
                    }
                    else
                    {
                        Log.Debug("skip explorer process for " + owner);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return null;
                }
            }
            return ownerexplorer;
        }
        public void AddConnection(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection)
        {
            try
            {
                if (this.connection != null)
                {
                    this.connection.ReceiveMessage -= Connection_ReceiveMessage;
                }
                this.connection = connection;
                this.connection.ReceiveMessage += Connection_ReceiveMessage;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Connection_ReceiveMessage(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection, RPAMessage message)
        {
            try
            {
                if (message.command == "pong")
                {
                    lastheartbeat = DateTime.Now;
                    return;
                }
                Log.Information(message.command.ToString());
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        #region IDisposable Support
        private bool disposedValue = false;
        async public Task SendSignout()
        {
            if (connection != null)
            {
                if (connection.IsConnected && client.autosignout)
                {
                    connection.PushMessage(new RPAMessage("signout"));
                    await Task.Delay(2000);
                }
            }
        }
        public void disconnectrdp()
        {
            // _ = SendSignout();
            if (freerdp != null)
            {
                if (freerdp.Connected) freerdp.Disconnect();
                freerdp.Dispose();
            }
            freerdp = null;
            if (rdp != null)
            {
                if (rdp.Connected) rdp.Disconnect();
                rdp.Dispose();
            }
            rdp = null;
            cancellationTokenSource.Cancel();
        }
        public void disconnect()
        {
            disconnectrdp();
            if (connection != null)
            {
                if (connection.IsConnected) connection.Close();
                connection = null;
            }

        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    disconnect();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
