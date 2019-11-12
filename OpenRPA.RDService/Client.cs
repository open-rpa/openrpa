using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.RDService
{
    public class Client
    {
        private int LogonErrorCode { get; set; }
        public bool Connected { get; set; } = false;
        public bool Connecting { get; set; } = false;
        private AxMSTSCLib.AxMsRdpClient9NotSafeForScripting rdpConnection;
        private Form form;
        private string server;
        private string domain;
        private string user;
        private string password;
        public void CreateRdpConnectionasync(string server, string domain, string user, string password)
        {
            Connecting = true;
            this.server = server;
            this.domain = domain;
            this.user = user;
            this.password = password;
            var rdpClientThread = new Thread(ProcessTaskThread) { IsBackground = true };
            rdpClientThread.SetApartmentState(ApartmentState.STA);
            rdpClientThread.Start();
        }
        public void CreateRdpConnection(string server, string domain, string user, string password)
        {
            Connecting = true;
            this.server = server;
            this.domain = domain;
            this.user = user;
            this.password = password;
            var rdpClientThread = new Thread(ProcessTaskThread) { IsBackground = true };
            rdpClientThread.SetApartmentState(ApartmentState.STA);
            rdpClientThread.Start();
            while (rdpClientThread.IsAlive)
            {
                Task.Delay(500).GetAwaiter().GetResult();
            }
        }
        private Impersonator imp;
        void ProcessTaskThread()
        {
            try
            {
                // imp = new Impersonator(user, domain, password);
                form = new Form();
                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int w = form.Width >= screen.Width ? screen.Width : (screen.Width + form.Width) / 2;
                int h = form.Height >= screen.Height ? screen.Height : (screen.Height + form.Height) / 2;
                form.Location = new Point((screen.Width - w) / 2, (screen.Height - h) / 2);
                form.Size = new Size(w, h);
                form.Load += (sender, args) =>
                {
                    try
                    {
                        rdpConnection = new AxMSTSCLib.AxMsRdpClient9NotSafeForScripting();
                        form.Controls.Add(rdpConnection);
                        rdpConnection.Dock = DockStyle.Fill;
                        // rdpConnection.Enabled = false;
                        rdpConnection.Server = server;
                        rdpConnection.Domain = domain;
                        rdpConnection.UserName = user;
                        rdpConnection.AdvancedSettings9.ClearTextPassword = password;
                        rdpConnection.AdvancedSettings9.EnableCredSspSupport = true;
                        if (true)
                        {
                            rdpConnection.OnDisconnected += RdpConnectionOnOnDisconnected;
                            rdpConnection.OnLoginComplete += RdpConnectionOnOnLoginComplete;
                            rdpConnection.OnLogonError += RdpConnectionOnOnLogonError;
                        }
                        rdpConnection.Connect();
                        Application.Run(form);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        Connecting = false;
                    }
                };
                form.Show();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                Connecting = false;
            }
        }
        private void RdpConnectionOnOnLogonError(object sender, AxMSTSCLib.IMsTscAxEvents_OnLogonErrorEvent e)
        {
            LogonErrorCode = e.lError;
            Console.WriteLine("RdpConnectionOnOnLogonError: " + LogonErrorCode);
        }
        private void RdpConnectionOnOnLoginComplete(object sender, EventArgs e)
        {
            Connected = true;
            Connecting = false;
            if (LogonErrorCode == -2)
            {
                System.Diagnostics.Debug.WriteLine($"    ## New Session Detected ##");
                Task.Delay(10000).GetAwaiter().GetResult();
            }
            // var rdpSession = (AxMSTSCLib.AxMsRdpClient9NotSafeForScripting)sender;
            // rdpSession.Disconnect();
        }
        private void RdpConnectionOnOnDisconnected(object sender, AxMSTSCLib.IMsTscAxEvents_OnDisconnectedEvent e)
        {
            Application.Exit();
            Connected = false;
            Connecting = false;
            Dispose();
        }
        public void Disconnect()
        {
            if (!Connected) return;
            rdpConnection.Disconnect();
            Dispose();
        }
        public void Dispose()
        {
            if (!Connected) return;
            rdpConnection.Disconnect();
            try
            {
                form.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
