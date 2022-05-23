using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var win = new TerminalRecorder();
            Config = new termVB5250Config();
            win.Config = Config;

            bool as400 = false;
            if (as400)
            {
                win.Config.Hostname = "PUB400.COM";
                win.Config.Port = 23;
                win.Config.TermType = "IBM-3278-2";
            }
            else
            {
                win.Config.Hostname = "localhost";
                win.Config.Port = 3270;
                win.Config.TermType = "IBM-3179-2";
            }

            string Hostname = ModelItem.GetValue<string>("Hostname");
            string TermType = ModelItem.GetValue<string>("TermType");
            int Port = ModelItem.GetValue<int>("Port");
            bool UseSSL = ModelItem.GetValue<bool>("UseSSL");
            if (!string.IsNullOrEmpty(Hostname)) win.Config.Hostname = Hostname;
            if (!string.IsNullOrEmpty(TermType)) win.Config.TermType = TermType;
            if (Port > 0) win.Config.Port = Port;
            win.Config.UseSSL = UseSSL;
            win.Show();
            win.Closed += Win_Closed;
        }
        private void Win_Closed(object sender, EventArgs e)
        {
            string Hostname = ModelItem.GetValue<string>("Hostname");
            string TermType = ModelItem.GetValue<string>("TermType");
            int Port = ModelItem.GetValue<int>("Port");
            bool UseSSL = ModelItem.GetValue<bool>("UseSSL");
            if (Hostname != Config.Hostname) ModelItem.Properties["Hostname"].SetValue(new InArgument<string>() { Expression = new Literal<string>(Config.Hostname) });
            if (TermType != Config.TermType) ModelItem.Properties["TermType"].SetValue(new InArgument<string>() { Expression = new Literal<string>(Config.TermType) });
            if (Port != Config.Port) ModelItem.Properties["Port"].SetValue(new InArgument<int>() { Expression = new Literal<int>(Config.Port) });
            if (UseSSL != Config.UseSSL) ModelItem.Properties["UseSSL"].SetValue(new InArgument<bool>() { Expression = new Literal<bool>(Config.UseSSL) });
        }
    }
}