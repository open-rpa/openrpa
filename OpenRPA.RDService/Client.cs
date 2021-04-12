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
        void ProcessTaskThread()
        {
            try
            {
                // if (form != null) return;
                // imp = new Impersonator(user, domain, password);
                form = new Form();
                form.Text = domain + "\\" + user;
                if (string.IsNullOrEmpty(domain)) form.Text = user;
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
                        rdpConnection.AdvancedSettings7.ClearTextPassword = password;
                        rdpConnection.AdvancedSettings7.EnableCredSspSupport = true;
                        if(PluginConfig.width > 0 && PluginConfig.height > 0)
                        {
                            rdpConnection.DesktopWidth = PluginConfig.width;
                            rdpConnection.DesktopHeight = PluginConfig.height;
                        }

                        //rdpConnection.AdvancedSettings9.ClearTextPassword = password;
                        //// rdpConnection.AdvancedSettings9.EnableCredSspSupport = false;
                        //rdpConnection.AdvancedSettings9.EnableCredSspSupport = true;

                        //rdpConnection.AdvancedSettings9.AuthenticationLevel = 2;
                        //rdpConnection.AdvancedSettings9.EnableCredSspSupport = true;
                        //rdpConnection.AdvancedSettings9.NegotiateSecurityLayer = false;

                        //var ocx = (MSTSCLib.IMsRdpClientNonScriptable7)rdpConnection.GetOcx();
                        //ocx.EnableCredSspSupport = true;
                        //ocx.AllowCredentialSaving = false;
                        //ocx.PromptForCredentials = false;
                        //ocx.PromptForCredsOnClient = true;
                        //ocx.AllowPromptingForCredentials = false;
                        //ocx.EnableCredSspSupport = true;
                        //ocx.AllowCredentialSaving = false;
                        //ocx.WarnAboutSendingCredentials = false;
                        //ocx.MarkRdpSettingsSecure = true;

                        ////rdp.AdvancedSettings7.DisableRdpdr = 0;
                        ////rdp.CreateVirtualChannels("CH001,CH002");

                        //var settings = (MSTSCLib.IMsRdpClientAdvancedSettings8)rdpConnection.AdvancedSettings;
                        //settings.allowBackgroundInput = 1;
                        //settings.ClientProtocolSpec = MSTSCLib.ClientSpec.FullMode;
                        //settings.ConnectToServerConsole = true;
                        //settings.EnableCredSspSupport = true;
                        //settings.EncryptionEnabled = 1;


                        //MSTSCLib.IMsRdpClientNonScriptable4 ocx = (MSTSCLib.IMsRdpClientNonScriptable4)rdpConnection.GetOcx();
                        //ocx.EnableCredSspSupport = true;
                        //ocx.AllowCredentialSaving = false;
                        //ocx.PromptForCredentials = false;
                        //ocx.PromptForCredsOnClient = false;
                        if (true)
                        {
                            rdpConnection.OnDisconnected += RdpConnectionOnOnDisconnected;
                            rdpConnection.OnLoginComplete += RdpConnectionOnOnLoginComplete;
                            rdpConnection.OnLogonError += RdpConnectionOnOnLogonError;
                            rdpConnection.OnWarning += RdpConnection_OnWarning;
                            rdpConnection.OnFatalError += RdpConnection_OnFatalError;
                            rdpConnection.OnAuthenticationWarningDismissed += RdpConnection_OnAuthenticationWarningDismissed;
                            rdpConnection.OnAuthenticationWarningDisplayed += RdpConnection_OnAuthenticationWarningDisplayed;
                            rdpConnection.OnConnected += RdpConnection_OnConnected;
                            rdpConnection.OnConnecting += RdpConnection_OnConnecting;
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
        private void RdpConnection_OnConnecting(object sender, EventArgs e)
        {
            Log.Debug("RdpConnection_OnConnecting");
        }
        private void RdpConnection_OnConnected(object sender, EventArgs e)
        {
            Log.Debug("RdpConnection_OnConnected");
        }
        private void RdpConnection_OnAuthenticationWarningDisplayed(object sender, EventArgs e)
        {
            Log.Output("RdpConnection_OnAuthenticationWarningDisplayed");
        }
        private void RdpConnection_OnFatalError(object sender, AxMSTSCLib.IMsTscAxEvents_OnFatalErrorEvent e)
        {
            Log.Error("RdpConnection_OnFatalError: " + e.errorCode);
            Connecting = false;
        }
        private void RdpConnection_OnAuthenticationWarningDismissed(object sender, EventArgs e)
        {
            Log.Debug("RdpConnection_OnAuthenticationWarningDismissed: ");
        }
        private void RdpConnection_OnWarning(object sender, AxMSTSCLib.IMsTscAxEvents_OnWarningEvent e)
        {
            Log.Debug("RdpConnection_OnWarning: " + e.warningCode);
        }
        private void RdpConnectionOnOnLogonError(object sender, AxMSTSCLib.IMsTscAxEvents_OnLogonErrorEvent e)
        {
            Connecting = false;
            LogonErrorCode = e.lError;
            Log.Debug("RdpConnectionOnOnLogonError: " + LogonErrorCode);
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
            var discReason = e.discReason;
            var discMsg = "";
            switch (discReason) // https://social.technet.microsoft.com/wiki/contents/articles/37870.remote-desktop-client-troubleshooting-disconnect-codes-and-reasons.aspx
            {
                case 0: discMsg = "No error"; break;
                case 1: discMsg = "User-initiated client disconnect."; break;
                case 2: discMsg = "User-initiated client logoff."; break;
                case 3: discMsg = "Your Remote Desktop Services session has ended, possibly for one of the following reasons:  The administrator has ended the session. An error occurred while the connection was being established. A network problem occurred.  For help solving the problem, see \"Remote Desktop\" in Help and Support."; break;
                case 260: discMsg = "Remote Desktop can't find the computer \". This might mean that \" does not belong to the specified network.  Verify the computer name and domain that you are trying to connect to."; break;
                case 262: discMsg = "This computer can't connect to the remote computer.  Your computer does not have enough virtual memory available. Close your other programs, and then try connecting again. If the problem continues, contact your network administrator or technical support."; break;
                case 264: discMsg = "This computer can't connect to the remote computer.  The two computers couldn't connect in the amount of time allotted. Try connecting again. If the problem continues, contact your network administrator or technical support."; break;
                case 266: discMsg = "The smart card service is not running. Please start the smart card service and try again."; break;
                case 516: discMsg = "Remote Desktop can't connect to the remote computer for one of these reasons:  1) Remote access to the server is not enabled 2) The remote computer is turned off 3) The remote computer is not available on the network  Make sure the remote computer is turned on and connected to the network, and that remote access is enabled."; break;
                case 522: discMsg = "A smart card reader was not detected. Please attach a smart card reader and try again."; break;
                case 772: discMsg = "This computer can't connect to the remote computer.  The connection was lost due to a network error. Try connecting again. If the problem continues, contact your network administrator or technical support."; break;
                case 778: discMsg = "There is no card inserted in the smart card reader. Please insert your smart card and try again."; break;
                case 1030: discMsg = "Because of a security error, the client could not connect to the remote computer. Verify that you are logged on to the network, and then try connecting again."; break;
                case 1032: discMsg = "The specified computer name contains invalid characters. Please verify the name and try again."; break;
                case 1034: discMsg = "An error has occurred in the smart card subsystem. Please contact your helpdesk about this error."; break;
                case 1796: discMsg = "This computer can't connect to the remote computer.  Try connecting again. If the problem continues, contact the owner of the remote computer or your network administrator."; break;
                case 1800: discMsg = "Your computer could not connect to another console session on the remote computer because you already have a console session in progress."; break;
                case 2056: discMsg = "The remote computer disconnected the session because of an error in the licensing protocol. Please try connecting to the remote computer again or contact your server administrator."; break;
                case 2308: discMsg = "Your Remote Desktop Services session has ended.  The connection to the remote computer was lost, possibly due to network connectivity problems. Try connecting to the remote computer again. If the problem continues, contact your network administrator or technical support."; break;
                case 2311: discMsg = "The connection has been terminated because an unexpected server authentication certificate was received from the remote computer. Try connecting again. If the problem continues, contact the owner of the remote computer or your network administrator."; break;
                case 2312: discMsg = "A licensing error occurred while the client was attempting to connect (Licensing timed out). Please try connecting to the remote computer again."; break;
                case 2567: discMsg = "The specified username does not exist. Verify the username and try logging in again. If the problem continues, contact your system administrator or technical support."; break;
                case 2820: discMsg = "This computer can't connect to the remote computer.  An error occurred that prevented the connection. Try connecting again. If the problem continues, contact the owner of the remote computer or your network administrator."; break;
                case 2822: discMsg = "Because of an error in data encryption, this session will end. Please try connecting to the remote computer again."; break;
                case 2823: discMsg = "The user account is currently disabled and cannot be used. For assistance, contact your system administrator or technical support."; break;
                case 2825: discMsg = "The remote computer requires Network Level Authentication, which your computer does not support. For assistance, contact your system administrator or technical support."; break;
                case 3079: discMsg = "A user account restriction (for example, a time-of-day restriction) is preventing you from logging on. For assistance, contact your system administrator or technical support."; break;
                case 3080: discMsg = "The remote session was disconnected because of a decompression failure at the client side. Please try connecting to the remote computer again."; break;
                case 3335: discMsg = "As a security precaution, the user account has been locked because there were too many logon attempts or password change attempts. Wait a while before trying again, or contact your system administrator or technical support."; break;
                case 3337: discMsg = "The security policy of your computer requires you to type a password on the Windows Security dialog box. However, the remote computer you want to connect to cannot recognize credentials supplied using the Windows Security dialog box. For assistance, contact your system administrator or technical support."; break;
                case 3590: discMsg = "The client can't connect because it doesn't support FIPS encryption level.  Please lower the server side required security level Policy, or contact your network administrator for assistance"; break;
                case 3591: discMsg = "This user account has expired. For assistance, contact your system administrator or technical support."; break;
                case 3592: discMsg = "Failed to reconnect to your remote session. Please try to connect again."; break;
                case 3593: discMsg = "The remote PC doesn't support Restricted Administration mode."; break;
                case 3847: discMsg = "This user account's password has expired. The password must change in order to logon. Please update the password or contact your system administrator or technical support."; break;
                case 3848: discMsg = "A connection will not be made because credentials may not be sent to the remote computer. For assistance, contact your system administrator."; break;
                case 4103: discMsg = "The system administrator has restricted the times during which you may log in. Try logging in later. If the problem continues, contact your system administrator or technical support."; break;
                case 4104: discMsg = "The remote session was disconnected because your computer is running low on video resources.  Close your other programs, and then try connecting again. If the problem continues, contact your network administrator or technical support."; break;
                case 4359: discMsg = "The system administrator has limited the computers you can log on with. Try logging on at a different computer. If the problem continues, contact your system administrator or technical support."; break;
                case 4615: discMsg = "You must change your password before logging on the first time. Please update your password or contact your system administrator or technical support."; break;
                case 4871: discMsg = "The system administrator has restricted the types of logon (network or interactive) that you may use. For assistance, contact your system administrator or technical support."; break;
                case 5127: discMsg = "The Kerberos sub-protocol User2User is required. For assistance, contact your system administrator or technical support."; break;
                case 6919: discMsg = "Remote Desktop cannot connect to the remote computer because the authentication certificate received from the remote computer is expired or invalid.  In some cases, this error might also be caused by a large time discrepancy between the client and server computers."; break;
                case 7431: discMsg = "Remote Desktop cannot verify the identity of the remote computer because there is a time or date difference between your computer and the remote computer. Make sure your computer's clock is set to the correct time, and then try connecting again. If the problem occurs again, contact your network administrator or the owner of the remote computer."; break;
                case 8711: discMsg = "Your computer can't connect to the remote computer because your smart card is locked out. Contact your network administrator about unlocking your smart card or resetting your PIN."; break;
                case 9479: discMsg = "Could not auto-reconnect to your applications,please re-launch your applications"; break;
                case 9732: discMsg = "Client and server versions do not match. Please upgrade your client software and then try connecting again."; break;
                case 33554433: discMsg = "Failed to reconnect to the remote program. Please restart the remote program."; break;
                case 33554434: discMsg = "The remote computer does not support RemoteApp. For assistance, contact your system administrator."; break;
                case 50331649: discMsg = "Your computer can't connect to the remote computer because the username or password is not valid. Type a valid user name and password."; break;
                case 50331650: discMsg = "Your computer can't connect to the remote computer because it can't verify the certificate revocation list. Contact your network administrator for assistance."; break;
                case 50331651: discMsg = "Your computer can't connect to the remote computer due to one of the following reasons:  1) The requested Remote Desktop Gateway server address and the server SSL certificate subject name do not match. 2) The certificate is expired or revoked. 3) The certificate root authority does not trust the certificate.  Contact your network administrator for assistance."; break;
                case 50331652: discMsg = "Your computer can't connect to the remote computer because the SSL certificate was revoked by the certification authority. Contact your network administrator for assistance."; break;
                case 50331653: discMsg = "This computer can't verify the identity of the RD Gateway . It's not safe to connect to servers that can't be identified. Contact your network administrator for assistance."; break;
                case 50331654: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server address requested and the certificate subject name do not match. Contact your network administrator for assistance."; break;
                case 50331655: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server's certificate has expired or has been revoked. Contact your network administrator for assistance."; break;
                case 50331656: discMsg = "Your computer can't connect to the remote computer because an error occurred on the remote computer that you want to connect to. Contact your network administrator for assistance."; break;
                case 50331657: discMsg = "An error occurred while sending data to the Remote Desktop Gateway server. The server is temporarily unavailable or a network connection is down. Try again later, or contact your network administrator for assistance."; break;
                case 50331658: discMsg = "An error occurred while receiving data from the Remote Desktop Gateway server. Either the server is temporarily unavailable or a network connection is down. Try again later, or contact your network administrator for assistance."; break;
                case 50331659: discMsg = "Your computer can't connect to the remote computer because an alternate logon method is required. Contact your network administrator for assistance."; break;
                case 50331660: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server address is unreachable or incorrect. Type a valid Remote Desktop Gateway server address."; break;
                case 50331661: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server is temporarily unavailable. Try reconnecting later or contact your network administrator for assistance."; break;
                case 50331662: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Services client component is missing or is an incorrect version. Verify that setup was completed successfully, and then try reconnecting later."; break;
                case 50331663: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server is running low on server resources and is temporarily unavailable. Try reconnecting later or contact your network administrator for assistance."; break;
                case 50331664: discMsg = "Your computer can't connect to the remote computer because an incorrect version of rpcrt4.dll has been detected. Verify that all components for Remote Desktop Gateway client were installed correctly."; break;
                case 50331665: discMsg = "Your computer can't connect to the remote computer because no smart card service is installed. Install a smart card service and then try again, or contact your network administrator for assistance."; break;
                case 50331666: discMsg = "Your computer can't stay connected to the remote computer because the smart card has been removed. Try again using a valid smart card, or contact your network administrator for assistance."; break;
                case 50331667: discMsg = "Your computer can't connect to the remote computer because no smart card is available. Try again using a smart card."; break;
                case 50331668: discMsg = "Your computer can't stay connected to the remote computer because the smart card has been removed. Reinsert the smart card and then try again."; break;
                case 50331669: discMsg = "Your computer can't connect to the remote computer because the user name or password is not valid. Please type a valid user name and password."; break;
                case 50331671: discMsg = "Your computer can't connect to the remote computer because a security package error occurred in the transport layer. Retry the connection or contact your network administrator for assistance."; break;
                case 50331672: discMsg = "The Remote Desktop Gateway server has ended the connection. Try reconnecting later or contact your network administrator for assistance."; break;
                case 50331673: discMsg = "The Remote Desktop Gateway server administrator has ended the connection. Try reconnecting later or contact your network administrator for assistance."; break;
                case 50331674: discMsg = "Your computer can't connect to the remote computer due to one of the following reasons:   1) Your credentials (the combination of user name, domain, and password) were incorrect. 2) Your smart card was not recognized."; break;
                case 50331675: discMsg = "Remote Desktop can't connect to the remote computer for one of these reasons:  1) Your user account is not listed in the RD Gateway's permission list 2) You might have specified the remote computer in NetBIOS format (for example, computer1), but the RD Gateway is expecting an FQDN or IP address format (for example, computer1.fabrikam.com or 157.60.0.1).  Contact your network administrator for assistance."; break;
                case 50331676: discMsg = "Remote Desktop can't connect to the remote computer for one of these reasons:  1) Your user account is not authorized to access the RD Gateway 2) Your computer is not authorized to access the RD Gateway 3) You are using an incompatible authentication method (for example, the RD Gateway might be expecting a smart card but you provided a password)  Contact your network administrator for assistance."; break;
                case 50331679: discMsg = "Your computer can't connect to the remote computer because your network administrator has restricted access to this RD Gateway server. Contact your network administrator for assistance."; break;
                case 50331680: discMsg = "Your computer can't connect to the remote computer because the web proxy server requires authentication. To allow unauthenticated traffic to an RD Gateway server through your web proxy server, contact your network administrator."; break;
                case 50331681: discMsg = "Your computer can't connect to the remote computer because your password has expired or you must change the password. Please change the password or contact your network administrator or technical support for assistance."; break;
                case 50331682: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server reached its maximum allowed connections. Try reconnecting later or contact your network administrator for assistance."; break;
                case 50331683: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server does not support the request. Contact your network administrator for assistance."; break;
                case 50331684: discMsg = "Your computer can't connect to the remote computer because the client does not support one of the Remote Desktop Gateway's capabilities. Contact your network administrator for assistance."; break;
                case 50331685: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server and this computer are incompatible. Contact your network administrator for assistance."; break;
                case 50331686: discMsg = "Your computer can't connect to the remote computer because the credentials used are not valid. Insert a valid smart card and type a PIN or password, and then try connecting again."; break;
                case 50331687: discMsg = "Your computer can't connect to the remote computer because your computer or device did not pass the Network Access Protection requirements set by your network administrator. Contact your network administrator for assistance."; break;
                case 50331688: discMsg = "Your computer can't connect to the remote computer because no certificate was configured to use at the Remote Desktop Gateway server. Contact your network administrator for assistance."; break;
                case 50331689: discMsg = "Your computer can't connect to the remote computer because the RD Gateway server that you are trying to connect to is not allowed by your computer administrator. If you are the administrator, add this Remote Desktop Gateway server name to the trusted Remote Desktop Gateway server list on your computer and then try connecting again."; break;
                case 50331690: discMsg = "Your computer can't connect to the remote computer because your computer or device did not meet the Network Access Protection requirements set by your network administrator, for one of the following reasons:  1) The Remote Desktop Gateway server name and the server's public key certificate subject name do not match. 2) The certificate has expired or has been revoked. 3) The certificate root authority does not trust the certificate. 4) The certificate key extension does not support encryption. 5) Your computer cannot verify the certificate revocation list.  Contact your network administrator for assistance."; break;
                case 50331691: discMsg = "Your computer can't connect to the remote computer because a user name and password are required to authenticate to the Remote Desktop Gateway server instead of smart card credentials."; break;
                case 50331692: discMsg = "Your computer can't connect to the remote computer because smart card credentials are required to authenticate to the Remote Desktop Gateway server instead of a user name and password."; break;
                case 50331693: discMsg = "Your computer can't connect to the remote computer because no smart card reader is detected. Connect a smart card reader and then try again, or contact your network administrator for assistance."; break;
                case 50331695: discMsg = "Your computer can't connect to the remote computer because authentication to the firewall failed due to missing firewall credentials. To resolve the issue, go to the firewall website that your network administrator recommends, and then try the connection again, or contact your network administrator for assistance."; break;
                case 50331696: discMsg = "Your computer can't connect to the remote computer because authentication to the firewall failed due to invalid firewall credentials. To resolve the issue, go to the firewall website that your network administrator recommends, and then try the connection again, or contact your network administrator for assistance."; break;
                case 50331698: discMsg = "Your Remote Desktop Services session ended because the remote computer didn't receive any input from you."; break;
                case 50331699: discMsg = "The connection has been disconnected because the session timeout limit was reached."; break;
                case 50331700: discMsg = "Your computer can't connect to the remote computer because an invalid cookie was sent to the Remote Desktop Gateway server. Contact your network administrator for assistance."; break;
                case 50331701: discMsg = "Your computer can't connect to the remote computer because the cookie was rejected by the Remote Desktop Gateway server. Contact your network administrator for assistance."; break;
                case 50331703: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway server is expecting an authentication method different from the one attempted. Contact your network administrator for assistance."; break;
                case 50331704: discMsg = "The RD Gateway connection ended because periodic user authentication failed. Try reconnecting with a correct user name and password. If the reconnection fails, contact your network administrator for further assistance."; break;
                case 50331705: discMsg = "The RD Gateway connection ended because periodic user authorization failed. Try reconnecting with a correct user name and password. If the reconnection fails, contact your network administrator for further assistance."; break;
                case 50331707: discMsg = "Your computer can't connect to the remote computer because the Remote Desktop Gateway and the remote computer are unable to exchange policies. This could happen due to one of the following reasons:     1. The remote computer is not capable of exchanging policies with the Remote Desktop Gateway.     2. The remote computer's configuration does not permit a new connection.     3. The connection between the Remote Desktop Gateway and the remote computer ended.    Contact your network administrator for assistance."; break;
                case 50331708: discMsg = "Your computer can't connect to the remote computer, possibly because the smart card is not valid, the smart card certificate was not found in the certificate store, or the Certificate Propagation service is not running. Contact your network administrator for assistance."; break;
                case 50331709: discMsg = "To use this program or computer, first log on to the following website: "; break;
                case 50331710: discMsg = "To use this program or computer, you must first log on to an authentication website. Contact your network administrator for assistance."; break;
                case 50331711: discMsg = "Your session has ended. To continue using the program or computer, first log on to the following website:."; break;
                case 50331712: discMsg = "Your session has ended. To continue using the program or computer, you must first log on to an authentication website. Contact your network administrator for assistance."; break;
                case 50331713: discMsg = "The RD Gateway connection ended because periodic user authorization failed. Your computer or device didn't pass the Network Access Protection (NAP) requirements set by your network administrator. Contact your network administrator for assistance."; break;
                case 50331714: discMsg = "Your computer can't connect to the remote computer because the size of the cookie exceeded the supported size. Contact your network administrator for assistance."; break;
                case 50331716: discMsg = "Your computer can't connect to the remote computer using the specified forward proxy configuration. Contact your network administrator for assistance."; break;
                case 50331717: discMsg = "This computer cannot connect to the remote resource because you do not have permission to this resource. Contact your network administrator for assistance."; break;
                case 50331718: discMsg = "There are currently no resources available to connect to. Retry the connection or contact your network administrator."; break;
                case 50331719: discMsg = "An error occurred while Remote Desktop Connection was accessing this resource. Retry the connection or contact your system administrator."; break;
                case 50331721: discMsg = "Your Remote Desktop Client needs to be updated to the newest version. Contact your system administrator for help installing the update, and then try again."; break;
                case 50331722: discMsg = "Your network configuration doesn't allow the necessary HTTPS ports. Contact your network administrator for help allowing those ports or disabling the web proxy, and then try connecting again."; break;
                case 50331723: discMsg = "We're setting up more resources, and it might take a few minutes. Please try again later."; break;
                case 50331724: discMsg = "The user name you entered does not match the user name used to subscribe to your applications. If you wish to sign in as a different user please choose Sign Out from the Home menu."; break;
                case 50331725: discMsg = "Looks like there are too many users trying out the Azure RemoteApp service at the moment. Please wait a few minutes and then try again."; break;
                case 50331726: discMsg = "Maximum user limit has been reached. Please contact your administrator for further assistance."; break;
                case 50331727: discMsg = "Your trial period for Azure RemoteApp has expired. Ask your admin or tech support for help."; break;
                case 50331728: discMsg = "You no longer have access to Azure RemoteApp. Ask your admin or tech support for help."; break;
                case 4498: discMsg = "Extended Reason: The remote session was disconnected because of a decryption error at the server. Please try connecting to the remote computer again."; break;
            }
            Log.Output("RdpConnectionOnOnDisconnected: " + discReason + " " + discMsg);
            // Application.Exit();
            Connected = false;
            Connecting = false;
            Dispose();
        }
        // https://stackoverflow.com/questions/1567017/com-object-that-has-been-separated-from-its-underlying-rcw-cannot-be-used
        public void Disconnect()
        {
            if (!Connected) return;
            try
            {
                if(form!=null && !form.Disposing)
                {
                    GenericTools.RunUI(form, () =>
                    {
                        try
                        {
                            if (rdpConnection != null && rdpConnection.Connected == 1) rdpConnection.Disconnect();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            //try
            //{
            //    if (rdpConnection != null && rdpConnection.Connected == 1) rdpConnection.Disconnect();
            //}
            //catch (Exception)
            //{
            //}
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                if(rdpConnection!=null && rdpConnection.Connected != 0) rdpConnection.Disconnect();
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
            if (rdpConnection != null) rdpConnection.Dispose();
            rdpConnection = null;
            try
            {
                if(form != null) form.Close();
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
            if (form != null) form.Dispose();
            form = null;
        }
    }
}
