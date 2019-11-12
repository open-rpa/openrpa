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
        private System.Threading.CancellationTokenSource cancellationTokenSource = new System.Threading.CancellationTokenSource();
        public RobotUserSession(unattendedclient client)
        {
            // rdpClient = new RdpClient();
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
                if (!task.IsCompleted || task.IsFaulted)
                {
                    if (task.Exception != null) Log.Error(task.Exception.ToString());
                }
            }, cancellationTokenSource.Token);
        }
        // public RdpClient rdpClient;
        // public FreeRDP.Core.RDP rdp;
        // public Client rdp;
        public FreeRDP.Core.RDP rdp;
        public unattendedclient client;
        public NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection;
        private DateTime created = DateTime.Now;
        private DateTime lastheartbeat = DateTime.Now;
        private DateTime lastrdp = DateTime.Now - TimeSpan.FromMinutes(1);
        public void DoWork()
        {
            if (client == null) return;
            if (string.IsNullOrEmpty(client._id)) return; // Dummy client, ignore
            if (connection != null && connection.IsConnected == false) { connection = null; created = DateTime.Now; }
            // Is OpenRPA connected for this user ?
            if (connection != null)
            {
                connection.PushMessage(new RPAMessage("ping"));
                created = DateTime.Now;
                return;
            }
            if ((DateTime.Now - created).TotalSeconds < 5) return;
            // Is user signed in ?
            // ownerexplorer = null;
            //if (ownerexplorer == null)
            if (rdp==null || rdp.Connected== false)
            {
                if (rdp == null)
                {
                    Console.WriteLine("rdp is null");
                }
                else
                {
                    //Console.WriteLine("rdp.Connected: " + rdp.Connected + " rdp.Connecting: " + rdp.Connecting);
                    //if (rdp.Connecting) return;
                }
                

                // if((DateTime.Now - lastrdp) < client.autorestart) return;
                if (string.IsNullOrEmpty(client.windowspassword)) return;
                lastrdp = DateTime.Now;
                if (rdp != null)
                {
                    //if (rdp.Connected)
                    //{
                    //    Log.Information("Disconnecting RDP connection for " + client.windowsusername);
                    //    rdp.Disconnect();
                    //    rdp.Dispose();
                    //    rdp = null;
                    //}
                }
                //if (rdpClient!=null)
                //{
                //    if(rdpClient.isConnected) 
                //    { 
                //        Log.Information("Disconnecting RDP connection for " + client.windowsusername) ;
                //        rdpClient.Disconnect();
                //        rdpClient.Dispose();
                //        rdpClient = null;
                //    }
                //}
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/2daeecb3-778b-478e-b379-102b790777c2/c-remote-desktop-over-the-same-computer?forum=csharpgeneral

                // https://github.com/terminals-Origin/Terminals
                // https://www.codeproject.com/Articles/43705/Remote-Desktop-using-C-NET
                // https://www.codeproject.com/Articles/16374/How-to-Write-a-Terminal-Services-Add-in-in-Pure-C
                // https://stackoverflow.com/questions/52801093/create-windows-session-programmatically-from-console-or-windows-service

                try
                {
                    // https://stackoverflow.com/questions/46807645/passing-a-struct-pointer-in-c-sharp-interop-results-in-null

                    //var thread = new System.Threading.Thread(() =>
                    //{
                    //    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    //    {
                    //        if(rdp==null) rdp = new Client();
                    //        //if(NativeMethods.GetProcessUserName)
                    //        var hostname = NativeMethods.GetHostName().ToLower();
                    //        if (client.windowsusername.StartsWith(hostname + @"\"))
                    //        {
                    //            Log.Information("Connecting RDP connection for " + client.windowsusername.Substring(hostname.Length + 1));
                    //            rdp.CreateRdpConnectionasync("127.0.0.2", "", client.windowsusername.Substring(hostname.Length + 1), client.windowspassword);
                    //        }
                    //        else
                    //        {
                    //            Log.Information("Connecting RDP connection for " + client.windowsusername);
                    //            rdp.CreateRdpConnectionasync("127.0.0.2", "", client.windowsusername, client.windowspassword);
                    //        }
                    //        created = DateTime.Now;

                    //    }));

                    //    Dispatcher.Run();
                    //});
                    //thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    //thread.Start();
                    //thread.Join();



                    //var thread = new System.Threading.Thread(() =>
                    //{
                    //    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    //    {
                    //        //rdpClient = new RdpClient();
                    //        //rdpClient.Connect("127.0.0.2", "", client.windowsusername, client.windowspassword);
                    //        rdp = new FreeRDP.Core.RDP();
                    //        rdp.Connect("127.0.0.2", "", client.windowsusername, client.windowspassword);
                    //    }));

                    //    Dispatcher.Run();
                    //});

                    //thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    //thread.Start();
                    //thread.Join();

                    if (rdp == null) rdp = new FreeRDP.Core.RDP();
                    //if(NativeMethods.GetProcessUserName)
                    var hostname = NativeMethods.GetHostName().ToLower();
                    if (client.windowsusername.StartsWith(hostname + @"\"))
                    {
                        var windowsusername = client.windowsusername.Substring(hostname.Length + 1);
                        Log.Information("Connecting RDP connection for " + windowsusername);
                        // rdp.CreateRdpConnectionasync("127.0.0.2", "", client.windowsusername.Substring(hostname.Length + 1), client.windowspassword);
                        rdp.Connect("127.0.0.2", "", windowsusername, client.windowspassword);
                    }
                    else
                    {
                        Log.Information("Connecting RDP connection for " + client.windowsusername);
                        // rdp.CreateRdpConnectionasync("127.0.0.2", "", client.windowsusername, client.windowspassword);
                        rdp.Connect("127.0.0.2", "", client.windowsusername, client.windowspassword);
                    }
                    created = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return;
            }
            if (rdp == null)
            {
                Console.WriteLine("rdp is null");
            }
            else
            {
                // Console.WriteLine("rdp.Connected: " + rdp.Connected + " rdp.Connecting: " + rdp.Connecting);
                Console.WriteLine("rdp.Connected: " + rdp.Connected);
            }
            if (rdp == null || rdp.Connected == false) return;

            try
            {
                var procs = Process.GetProcessesByName("explorer");
                System.Diagnostics.Process ownerexplorer = null;
                foreach (var explorer in procs)
                {
                    try
                    {
                        var owner = NativeMethods.GetProcessUserName(explorer).ToLower();
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
                        var owner = NativeMethods.GetProcessUserName(rpa).ToLower();
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
                if (!Program.isService)
                {
                    Log.Information("Not running as service, so just launching openrpa here");
                    Log.Information(client.openrpapath);
                    created = DateTime.Now;
                    Process.Start(new ProcessStartInfo(client.openrpapath) { WorkingDirectory = path });
                    return;
                }
                Log.Information("Attaching to user explorer and launching robot in session");
                Log.Information(client.openrpapath);
                created = DateTime.Now;
                if (!NativeMethods.Launch(ownerexplorer, path, @"c:\windows\system32\cmd.exe /C " + "\"" + client.openrpapath + "\""))
                {
                    Log.Error("Failed launching robot in session");
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
            if(this.connection != null)
            {
                this.connection.ReceiveMessage -= Connection_ReceiveMessage;
            }
            this.connection = connection;
            this.connection.ReceiveMessage += Connection_ReceiveMessage;
        }
        private void Connection_ReceiveMessage(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection, RPAMessage message)
        {
            if (message.command == "pong")
            {
                lastheartbeat = DateTime.Now;
                return;
            }
            Log.Debug(message.command.ToString());
        }
        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //if (rdpClient != null)
                    //{
                    //    if(rdpClient.isConnected) rdpClient.Disconnect();
                    //    rdpClient.Dispose();
                    //}
                    //rdpClient = null;
                    if (rdp != null)
                    {
                        if (rdp.Connected) rdp.Disconnect();
                        rdp.Dispose();
                    }
                    rdp = null;
                    cancellationTokenSource.Cancel();

                    if (connection != null)
                    {
                        if (connection.IsConnected) connection.Close();
                        connection = null;
                    }
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
