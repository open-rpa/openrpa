using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

namespace OpenRPA.RDServicePlugin.Views
{
    /// <summary>
    /// Interaction logic for RunPluginView.xaml
    /// </summary>
    public partial class RunPluginView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private RDService.unattendedserver server = null;
        private RDService.unattendedclient client = null;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        private Plugin plugin;
        public RunPluginView(Plugin plugin)
        {
            InitializeComponent();
            DataContext = this;
            this.plugin = plugin;
            lblWindowsusername.Text = NativeMethods.GetProcessUserName(System.Diagnostics.Process.GetCurrentProcess().Id);
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                var filepath = asm.CodeBase.Replace("file:///", "");
                var path = System.IO.Path.GetDirectoryName(filepath);
                RDService.PluginConfig.Save();
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
        }
        private void ReauthenticateButtonClick(object sender, RoutedEventArgs e)
        {
            if (Config.local.jwt != null && Config.local.jwt.Length > 0)
            {
                Log.Information("Saving temporart jwt token, from local settings.json");
                RDService.PluginConfig.tempjwt = new System.Net.NetworkCredential(string.Empty, Config.local.UnprotectString(Config.local.jwt)).Password;
                RDService.PluginConfig.wsurl = Config.local.wsurl;
                RDService.PluginConfig.Save();
            }
            else
            {
                Log.Error("Fail locating a JWT token to seed into service config!");
                MessageBox.Show("Fail locating a JWT token to seed into service config!");
            }
        }
        private async void StartServiceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableButtons();
                await Plugin.manager.StartService();
            }
            catch (Exception ex)
            {
                MessageBox.Show("StartServiceButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private async void StopServiceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableButtons();
                await Plugin.manager.StopService();
                await Plugin.manager.StartService();
            }
            catch (Exception ex)
            {
                MessageBox.Show("StopServiceButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private void InstallServiceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Plugin.manager.IsServiceInstalled)
                {
                    // if (string.IsNullOrEmpty(windowspassword.Password)) { MessageBox.Show("Password missing"); return; }
                    DisableButtons();
                    var asm = System.Reflection.Assembly.GetEntryAssembly();
                    var filepath = asm.CodeBase.Replace("file:///", "");
                    var path = System.IO.Path.GetDirectoryName(filepath);
                    var filename = System.IO.Path.Combine(path, "OpenRPA.RDService.exe");
                    // Plugin.manager.InstallService(filename, new string[] { "username=" + NativeMethods.GetProcessUserName(), "password="+ windowspassword.Password });
                    Plugin.manager.InstallService(filename,null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("InstallServiceButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private void UninstallServiceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Plugin.manager.IsServiceInstalled)
                {
                    DisableButtons();
                    var asm = System.Reflection.Assembly.GetEntryAssembly();
                    var filepath = asm.CodeBase.Replace("file:///", "");
                    var path = System.IO.Path.GetDirectoryName(filepath);
                    var filename = System.IO.Path.Combine(path, "OpenRPA.RDService.exe");
                    Plugin.manager.UninstallService(filename);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UninstallServiceButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        public void DisableButtons()
        {
            AddcurrentuserButton.IsEnabled = false;
            RemovecurrentuserButton.IsEnabled = false;
            ReauthenticateButton.IsEnabled = false;
            StartServiceButton.IsEnabled = false;
            StopServiceButton.IsEnabled = false;
            InstallServiceButton.IsEnabled = false;
            UninstallServiceButton.IsEnabled = false;

        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string computername = NativeMethods.GetHostName().ToLower();
                string computerfqdn = NativeMethods.GetFQDN().ToLower();
                string windowsusername = NativeMethods.GetProcessUserName(System.Diagnostics.Process.GetCurrentProcess().Id).ToLower();

                var servers = await global.webSocketClient.Query<RDService.unattendedserver>("openrpa", "{'_type':'unattendedserver', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "'}");
                server = servers.FirstOrDefault();

                var clients = await global.webSocketClient.Query<RDService.unattendedclient>("openrpa", "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "', 'windowsusername':'" + windowsusername.Replace(@"\", @"\\") + "'}");
                AddcurrentuserButton.Content = "Add current user";
                if (clients.Length == 1)
                {
                    client = clients.First();
                    AddcurrentuserButton.Content = "Update current user";
                    chkautosignout.IsChecked = client.autosignout;
                }
                txtreloadinterval.Text = RDService.PluginConfig.reloadinterval.ToString();
                chkUseFreeRDP.IsChecked = RDService.PluginConfig.usefreerdp;
                AddcurrentuserButton.IsEnabled = false;
                RemovecurrentuserButton.IsEnabled = false;
                ReauthenticateButton.IsEnabled = false;
                StartServiceButton.IsEnabled = false;
                StopServiceButton.IsEnabled = false;
                InstallServiceButton.IsEnabled = true;
                UninstallServiceButton.IsEnabled = false;
                if(client!=null)
                {
                    lblExecutable.Text = client.openrpapath;
                }
                if (Plugin.manager.IsServiceInstalled)
                {
                    AddcurrentuserButton.IsEnabled = true;
                    RemovecurrentuserButton.IsEnabled = (client!=null);
                    ReauthenticateButton.IsEnabled = true;
                    StartServiceButton.IsEnabled = (Plugin.manager.Status != System.ServiceProcess.ServiceControllerStatus.Running);
                    StopServiceButton.IsEnabled = (Plugin.manager.Status == System.ServiceProcess.ServiceControllerStatus.Running);

                    InstallServiceButton.IsEnabled = false;
                    UninstallServiceButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Log.Information("UserControl_Loaded: " + ex.Message);
            }
        }
        private async void AddcurrentuserButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableButtons();
                string computername = NativeMethods.GetHostName().ToLower();
                string computerfqdn = NativeMethods.GetFQDN().ToLower();
                string windowsusername = NativeMethods.GetProcessUserName(System.Diagnostics.Process.GetCurrentProcess().Id).ToLower();
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                var path = asm.CodeBase.Replace("file:///", "");
                if (client == null)
                {
                    client = new RDService.unattendedclient() { computername = computername, computerfqdn = computerfqdn, windowsusername = windowsusername, name = computername + " " + windowsusername, openrpapath = path };
                    client._acl = server._acl;
                    client = await global.webSocketClient.InsertOne("openrpa", 1, false, client);
                }
                lblExecutable.Text = client.openrpapath;
                if (!string.IsNullOrEmpty(windowspassword.Password)) client.windowspassword = windowspassword.Password;
                client.computername = computername;
                client.computerfqdn = computerfqdn;
                client.windowsusername = windowsusername;
                client.name = computername + " " + windowsusername;
                client.openrpapath = path;
                client = await global.webSocketClient.UpdateOne("openrpa", 1, false, client);
                windowspassword.Clear();
                plugin.ReloadConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddcurrentuserButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private async void RemovecurrentuserClick(object sender, RoutedEventArgs e)
        {
            if (client == null) return;
            try
            {
                DisableButtons();
                await global.webSocketClient.DeleteOne("openrpa", client._id);
                client = null;
                plugin.ReloadConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddcurrentuserButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private void chkUseFreeRDP_IsEnabledChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkUseFreeRDP.IsChecked == null) return;
                RDService.PluginConfig.usefreerdp = chkUseFreeRDP.IsChecked.Value;
                RDService.PluginConfig.Save();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void chkUseFreeRDP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkUseFreeRDP.IsChecked == null) return;
                RDService.PluginConfig.usefreerdp = chkUseFreeRDP.IsChecked.Value;
                RDService.PluginConfig.Save();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void txtreloadinterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TimeSpan ts = TimeSpan.Zero;
                if (TimeSpan.TryParse(txtreloadinterval.Text, out ts))
                {
                    RDService.PluginConfig.reloadinterval = ts;
                    RDService.PluginConfig.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private async void chkautosignout_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (client == null) return;
                client.autosignout = chkautosignout.IsChecked.Value;
                await global.webSocketClient.UpdateOne("openrpa", 1, false, client);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private async void chkautosignout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (client == null) return;
                client.autosignout = chkautosignout.IsChecked.Value;
                await global.webSocketClient.UpdateOne("openrpa", 1, false, client);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
