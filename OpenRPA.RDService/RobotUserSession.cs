using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    using OpenRPA.Interfaces;
    using System.Windows.Threading;

    public class RobotUserSession : IDisposable
    {
        public RobotUserSession(unattendedclient client)
        {
            this.client = client;
            var t = Task.Factory.StartNew(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        DoWork();
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
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        public void DoWork()
        {
            try
            {
                if(System.Diagnostics.Debugger.IsAttached)
                {
                    skiprdp = true;
                }
                if (client == null)
                {
                    Log.Information("client is null");
                    return;
                }
                if (string.IsNullOrEmpty(client._id))
                {
                    // Log.Information("client._id is null, Dummy client, ignore");
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
                        if (freerdp == null) Console.WriteLine("rdp is null");
                        if (string.IsNullOrEmpty(client.windowspassword)) return;
                        lastrdp = DateTime.Now;
                    }
                    if (freerdp == null) freerdp = new FreeRDP.Core.RDP();
                    if (!freerdp.Connected)
                    {
                        var hostname = NativeMethods.GetHostName().ToLower();
                        try
                        {
                            Log.Information("Tesing connection to " + rdpip + " port 3389");
                            using (var tcpClient = new System.Net.Sockets.TcpClient())
                            {
                                var ipAddress = System.Net.IPAddress.Parse(rdpip);
                                var ipEndPoint = new System.Net.IPEndPoint(ipAddress, 3389);
                                tcpClient.Connect(ipEndPoint);
                            }
                            Log.Information("Success");
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

                        using (var imp = new Impersonator(windowsusername, windowsdomain, client.windowspassword))
                        {
                            ConnectionAttempts++;
                            Log.Information("Connecting RDP connection to " + rdpip + " for " + client.windowslogin);
                            freerdp.Connect(rdpip, "", client.windowslogin, client.windowspassword);
                            //if (client.windowsusername.StartsWith(hostname + @"\"))
                            //{
                            //    // var windowsusername = client.windowsusername.Substring(hostname.Length + 1);
                            //    Log.Information("Connecting RDP connection to " + rdpip + " for " + windowsusername);
                            //    freerdp.Connect(rdpip, "", windowsusername, client.windowspassword);
                            //}
                            //else
                            //{
                            //    Log.Information("Connecting RDP connection to " + rdpip + " for " + client.windowsusername);
                            //    freerdp.Connect(rdpip, "", client.windowsusername, client.windowspassword);
                            //}
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
                        if (rdp == null) Console.WriteLine("rdp is null");
                        if (string.IsNullOrEmpty(client.windowspassword)) return;
                        lastrdp = DateTime.Now;
                    }
                    if (rdp == null) rdp = new Client();
                    if (rdp.Connecting) return;
                    if (!rdp.Connected)
                    {
                        try
                        {
                            Log.Information("Tesing connection to " + rdpip + " port 3389");
                            using (var tcpClient = new System.Net.Sockets.TcpClient())
                            {
                                var ipAddress = System.Net.IPAddress.Parse(rdpip);
                                var ipEndPoint = new System.Net.IPEndPoint(ipAddress, 3389);
                                tcpClient.Connect(ipEndPoint);
                            }
                            Log.Information("Success");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            return;
                        }

                        ConnectionAttempts++;
                        var hostname = NativeMethods.GetHostName().ToLower();
                        Log.Information("Connecting RDP connection to " + rdpip + " for " + client.windowslogin);
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
                        Log.Information("Connection initialized");
                        //if (client.windowsusername.StartsWith(hostname + @"\"))
                        //{
                        //    var windowsusername = client.windowsusername.Substring(hostname.Length + 1);
                        //    Log.Information("Connecting RDP connection to " + rdpip + " for " + windowsusername);
                        //    // Task.Run(()=>rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername.Substring(hostname.Length + 1), client.windowspassword));
                        //    rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername.Substring(hostname.Length + 1), client.windowspassword);
                        //    Log.Information("Connection initialized");
                        //}
                        //else
                        //{
                        //    Log.Information("Connecting RDP connection to " + rdpip + " for " + client.windowsusername);
                        //    // Task.Run(() => rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername, client.windowspassword));
                        //    rdp.CreateRdpConnectionasync(rdpip, "", client.windowsusername, client.windowspassword);
                        //    Log.Information("Connection initialized");
                        //}
                        created = DateTime.Now;
                    }
                    if (rdp == null || rdp.Connected == false) return;
                }

                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_ASSIGNPRIMARYTOKEN_NAME), true);
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_BACKUP_NAME), true);
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_DEBUG_NAME), true);
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_LOAD_DRIVER_NAME), true);
                NativeMethods.EnableDisablePrivilege(NativeMethods.GetSecurityEntityValue(NativeMethods.SecurityEntity.SE_TCB_NAME), true);

                try
                {
                    var procs = Process.GetProcessesByName("explorer");
                    System.Diagnostics.Process ownerexplorer = null;
                    foreach (var explorer in procs)
                    {
                        try
                        {
                            var owner = NativeMethods.GetProcessUserName(explorer.Id).ToLower();
                            if (owner == client.windowsusername)
                            {
                                ownerexplorer = explorer;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            created = DateTime.Now;
                            return;
                        }
                    }
                    if (ownerexplorer == null) return;
                    System.Diagnostics.Process ownerrpa = null;
                    procs = Process.GetProcessesByName("openrpa");
                    foreach (var rpa in procs)
                    {
                        try
                        {
                            var owner = NativeMethods.GetProcessUserName(rpa.Id).ToLower();
                            if (owner == client.windowsusername)
                            {
                                ownerrpa = rpa;
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
                    if (string.IsNullOrEmpty(client.openrpapath)) return;
                    if (!System.IO.File.Exists(client.openrpapath)) return;
                    var path = System.IO.Path.GetDirectoryName(client.openrpapath);
                    //if (!Program.isService)
                    //{
                    //    Log.Information("Not running as service, so just launching openrpa here");
                    //    Log.Information(client.openrpapath);
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
                        Log.Information("Attaching to user explorer and launching robot in session");
                        Log.Information(client.openrpapath);
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
        public void disconnectrdp()
        {
            if(connection!=null)
            {
                if(connection.IsConnected && client.autosignout)
                {
                    connection.PushMessage(new RPAMessage("signout"));
                }
            }
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
