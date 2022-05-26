using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.TerminalEmulator
{
    public partial class TerminalSessionDesigner
    {
        public TerminalSessionDesigner()
        {
            InitializeComponent();
        }
        public Interfaces.VT.ITerminalConfig Config = null;
        public string sessionid;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Config = new termVB5250Config();

            bool as400 = false;
            if (as400)
            {
                Config.Hostname = "PUB400.COM";
                Config.Port = 23;
                Config.TermType = "IBM-3278-2";
            }
            else
            {
                Config.Hostname = "localhost";
                Config.Port = 3270;
                Config.TermType = "IBM-3179-2";
            }

            var body = ModelItem.Properties["Body"].Value;
            var handler = body.Properties["Handler"].Value;
            global.OpenRPAClient.CurrentDesigner.SelectedActivity = handler;
            //body.Focus(20);
            //handler.Focus(20);

            string Hostname = ModelItem.GetValue<string>("Hostname");
            string TermType = ModelItem.GetValue<string>("TermType");
            int Port = ModelItem.GetValue<int>("Port");
            bool UseSSL = ModelItem.GetValue<bool>("UseSSL");
            if (!string.IsNullOrEmpty(Hostname)) Config.Hostname = Hostname;
            if (!string.IsNullOrEmpty(TermType)) Config.TermType = TermType;
            if (Port > 0) Config.Port = Port;
            Config.UseSSL = UseSSL;

            TerminalRecorder win = null;
            if (string.IsNullOrEmpty(Hostname) && string.IsNullOrEmpty(TermType))
            {
                win = new TerminalRecorder();
                win.Config = Config;
                RunPlugin.Sessions.Add(win);
            }
            else
            {
                win = RunPlugin.GetRecorderWindow(Config);
            }
            sessionid = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(win.WorkflowInstanceId)) win.WorkflowInstanceId = sessionid;
            win.Show();
            //win.ShowDialog();
            win.Focus();
            win.Closed += Win_Closed;
        }
        private void Win_Closed(object sender, EventArgs e)
        {
            var win = sender as TerminalRecorder;
            if(win == null) return;
            if (win.WorkflowInstanceId != sessionid) return;
            string Hostname = ModelItem.GetValue<string>("Hostname");
            string TermType = ModelItem.GetValue<string>("TermType");
            int Port = ModelItem.GetValue<int>("Port");
            bool UseSSL = ModelItem.GetValue<bool>("UseSSL");
            if (Hostname != Config.Hostname) ModelItem.Properties["Hostname"].SetValue(new InArgument<string>() { Expression = new Literal<string>(Config.Hostname) });
            if (TermType != Config.TermType) ModelItem.Properties["TermType"].SetValue(new InArgument<string>() { Expression = new Literal<string>(Config.TermType) });
            if (Port != Config.Port) ModelItem.Properties["Port"].SetValue(new InArgument<int>() { Expression = new Literal<int>(Config.Port) });
            if (UseSSL != Config.UseSSL) ModelItem.Properties["UseSSL"].SetValue(new InArgument<bool>() { Expression = new Literal<bool>(Config.UseSSL) });
            RunPlugin.Sessions.Remove(win);
        }
    }
}