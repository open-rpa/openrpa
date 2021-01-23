using AxMSTSCLib;
using Microsoft.VisualBasic.Activities;
using MSTSCLib;
using Newtonsoft.Json.Linq;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using OpenRPA.Net;
using OpenRPA.Views;
using System;
using System.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.AvalonDock.Layout;

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ChildSession : Window
    {
        private AxMsRdpClient9NotSafeForScripting rdp;
        //private bool AllowClose = false;
        //private IMsRdpClient7 rdpClient;
        private bool isClosing = false;
        private bool isConnected = false;
        public ChildSession()
        {
            InitializeComponent();
            rdp = new AxMSTSCLib.AxMsRdpClient9NotSafeForScripting();
            // rdp = new AxMsTscAxNotSafeForScripting();
            formhost.Child = rdp;
            
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var exepath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    key.SetValue("openrpa_childsession", exepath);
                }
            }
            catch (Exception)
            {
            }

            Connect();
        }
        public void Connect()
        {
            rdp.Server = "localhost";
            object otrue = true;
            try
            {
                var settings = (rdp.GetOcx() as IMsRdpExtendedSettings);
                settings.set_Property("ConnectToChildSession", ref otrue);
            }
            catch (Exception)
            {

                throw;
            }
            // var _rdpSettings = rdpClient.AdvancedSettings7;
            rdp.AdvancedSettings7.EnableCredSspSupport = true;
            rdp.AdvancedSettings7.SmartSizing = true;
            rdp.AdvancedSettings7.DisplayConnectionBar = false;
            rdp.AdvancedSettings7.RedirectSmartCards = true;
            rdp.OnLoginComplete += (_, __) => { Log.Information("ChildSession LoginComplete"); LabelStatusBar.Content = "Login completed"; };
            rdp.OnWarning += (_, e) => { Log.Information($"ChildSession Warning: {e.warningCode}"); LabelStatusBar.Content = $"Warning: {e.warningCode}"; };
            rdp.OnConnecting += (_, e) => { Log.Information("ChildSession Connecting"); isConnected = false; LabelStatusBar.Content = $"Connecting"; };
            rdp.OnConnected += (_, e) => {
                isConnected = true;
                Log.Information("ChildSession Connected"); 
                LabelStatusBar.Content = $"Connected";
                Task.Run(() =>
                {
                    bool connected = false;
                    while (!isConnected && !isClosing)
                    {
                        try
                        {
                            if(!connected)
                            {
                                connected = Interfaces.IPCService.OpenRPAServiceUtil.GetInstance(ChildSession: true);
                                if(connected)
                                {
                                    try
                                    {
                                        using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                                        {
                                            key.DeleteValue("openrpa_childsession", false);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            } 
                            else
                            {
                                connected = Interfaces.IPCService.OpenRPAServiceUtil.GetInstance(ChildSession: true);
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                });
                //var sw = new System.Diagnostics.Stopwatch(); sw.Start();
                //bool connected = false;
                //while (!connected && sw.Elapsed < TimeSpan.FromSeconds(10))
                //{
                //    try
                //    {
                //        connected = Interfaces.IPCService.OpenRPAServiceUtil.GetInstance(ChildSession: true);
                //    }
                //    catch (Exception)
                //    {
                //    }
                //}
                //uint ChildSessionId = Interfaces.win32.ChildSession.GetChildSessionId();
                //if (!connected)
                //{
                //    var explorer = System.Diagnostics.Process.GetProcessesByName("explorer").Where(p => p.SessionId == (int)ChildSessionId).ToList();
                //    if(explorer.Count == 1)
                //    {
                //        var exepath = Assembly.GetExecutingAssembly().Location;
                //        var path = System.IO.Path.GetDirectoryName(exepath);
                //        if (!NativeMethods.Launch(explorer[0], path, exepath))
                //        {
                //            Log.Error("Failed launching robot in session");
                //            string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                //            Log.Error(errorMessage);
                //        }
                //    }
                //}
            };
            rdp.OnDisconnected += OnDisconnected;
            rdp.OnLogonError += delegate (object _, IMsTscAxEvents_OnLogonErrorEvent e) {
                if (e.lError == -2)
                {
                    Log.Information("ChildSession Continuing the logon process");
                    LabelStatusBar.Content = $"Continuing the logon process";
                }
                else
                {
                    isConnected = false;
                    Log.Error($"ChildSession LogonError: {e.lError}");
                    LabelStatusBar.Content = $"LogonError: {e.lError}";
                    Close();
                }
            };
            rdp.OnFatalError += delegate (object _, IMsTscAxEvents_OnFatalErrorEvent e) {
                isConnected = false;
                Log.Error($"ChildSession FatalError: {e.errorCode}");
                LabelStatusBar.Content = $"FatalError: {e.errorCode}";
                Close();
            };
            rdp.Connect();
        }
        private void OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            isConnected = false;
            ExtendedDisconnectReasonCode extendedDisconnectReason = rdp.ExtendedDisconnectReason;
            string errorDescription = rdp.GetErrorDescription((uint)e.discReason, (uint)extendedDisconnectReason);
            Log.Error($"ChildSession Disconnected: {errorDescription}, Reason: {e.discReason}, ExtendedReason {extendedDisconnectReason}");
            Close();
        }
        private void ChildSession_Closed(object sender, EventArgs e)
        {
            if(MainWindow.instance != null) MainWindow.instance.childSession = null;
        }
        private void ChildSession_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if(rdp!=null)
                {
                    rdp.OnDisconnected -= OnDisconnected;
                    if (rdp.Connected != 0)
                    {
                        rdp.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            isClosing = true;
        }
    }
}
