using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
        }

        public SplashScreenWindow(App app)
        {
            this.app = app;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
            Task.Run(() =>
            {
                LabelStatusBar.Text = "Loading plugins";
                Plugins.loadPlugins(Extensions.projectsDirectory);
                if (string.IsNullOrEmpty(Config.local.wsurl))
                {
                    var Detectors = Interfaces.entity.Detector.loadDetectors(Extensions.projectsDirectory);
                    foreach (var d in Detectors)
                    {
                        IDetectorPlugin dp = null;
                        d.Path = Extensions.projectsDirectory;
                        dp = Plugins.AddDetector(d);
                        if (dp != null) dp.OnDetector += OnDetector;
                    }
                }

                ExpressionEditor.EditorUtil.init();
                AutomationHelper.init();
                new System.Activities.Core.Presentation.DesignerMetadata().Register();
                if (!string.IsNullOrEmpty(Config.local.wsurl))
                {
                    global.webSocketClient = new Net.WebSocketClient(Config.local.wsurl);
                    global.webSocketClient.OnOpen += WebSocketClient_OnOpen;
                    _ = global.webSocketClient.Connect();
                }
                else
                {
                    CloseSplash();
                }
            });
        }
        private bool loginInProgress = false;
        private App app;
        public void CloseSplash()
        {
            Task.Factory.StartNew(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    app.MainWindow = new MainWindow(); ;
                    app.MainWindow.Show();
                    Close();
                });
            });
        }

        private void WebSocketClient_OnOpen()
        {
            AutomationHelper.syncContext.Post(async o =>
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();



                Log.Debug("WebSocketClient_OnOpen::begin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                LabelStatusBar.Text = "Connected to " + Config.local.wsurl;
                Interfaces.entity.TokenUser user = null;
                while (user == null)
                {
                    string errormessage = string.Empty;
                    if (!string.IsNullOrEmpty(Config.local.username))
                    {
                        try
                        {
                            LabelStatusBar.Text = "Connected to " + Config.local.wsurl + " signing in as " + Config.local.username + " ...";
                            Log.Debug("Signing in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            user = await global.webSocketClient.Signin(Config.local.username, Config.local.UnprotectString(Config.local.password));
                            Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            LabelStatusBar.Text = "Connected to " + Config.local.wsurl + " as " + user.name;
                        }
                        catch (Exception ex)
                        {
                            this.Hide();
                            Log.Error(ex, "");
                            errormessage = ex.Message;
                        }
                    }
                    if (user == null)
                    {
                        if (loginInProgress == false)
                        {
                            loginInProgress = true;
                            var w = new Views.LoginWindow();
                            w.username = Config.local.username;
                            w.errormessage = errormessage;
                            w.fqdn = new Uri(Config.local.wsurl).Host;
                            this.Hide();
                            if (w.ShowDialog() != true) { this.Show(); return; }
                            Config.local.username = w.username; Config.local.password = Config.local.ProtectString(w.password);
                            Config.Save();
                            loginInProgress = false;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                try
                {
                    try
                    {
                        Log.Debug("Registering queue for robot " + global.webSocketClient.user._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        await global.webSocketClient.RegisterQueue(global.webSocketClient.user._id);
                        foreach (var role in global.webSocketClient.user.roles)
                        {
                            Log.Debug("Registering queue for role " + role.name + " " + role._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            await global.webSocketClient.RegisterQueue(role._id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error RegisterQueue" + ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    MessageBox.Show("WebSocketClient_OnOpen::Sync projects " + ex.Message);
                }
                CloseSplash();
            }, null);
        }

    }
}
