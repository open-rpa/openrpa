using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.RDServicePlugin
{
    public class Plugin : IRunPlugin
    {
        private NamedPipeWrapper.NamedPipeClient<RPAMessage> pipe = null;
        public const string ServiceName = "OpenRPA";
        public static ServiceManager manager = new ServiceManager(ServiceName);
        public string Name => "rdservice";
        // public System.Windows.Controls.UserControl editor => null;
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private IOpenRPAClient client = null;
        private Views.RunPluginView view;
        public UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.RunPluginView(this);
                    view.PropertyChanged += (s, e) =>
                    {
                        //NotifyPropertyChanged("Entity");
                        //NotifyPropertyChanged("Name");
                    };
                }
                return view;
            }
        }
        public void Initialize(IOpenRPAClient client)
        {
            this.client = client;
            this.client.Signedin += Client_Signedin;
        }
        private void Client_Signedin(Interfaces.entity.TokenUser user)
        {
            if (pipe != null) return;
            try
            {
                pipe = new OpenRPA.NamedPipeWrapper.NamedPipeClient<RPAMessage>("openrpa_service");
                pipe.Connected += Pipe_Connected;
                pipe.Disconnected += Pipe_Disconnected;
                pipe.Error += Pipe_Error;
                pipe.ServerMessage += Pipe_ServerMessage;
                pipe.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Pipe_ServerMessage(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection, RPAMessage message)
        {
            if(message.command == "ping")
            {
                connection.PushMessage(new RPAMessage("pong"));
                return;
            }
            if(message.command == "signout")
            {
                try
                {
                    Config.Save();
                }
                catch (Exception)
                {
                }
                NativeMethods.ExitWindowsEx((uint)NativeMethods.ExitWindows.LogOff, (uint)(NativeMethods.ShutdownReason.MajorOther | NativeMethods.ShutdownReason.MinorOther));
            }
            Console.WriteLine("OpenRPA Windows Service: " + message.command);
        }
        private void Pipe_Error(Exception ex)
        {
            Log.Error(ex.ToString());
        }
        private void Pipe_Disconnected(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection)
        {
            Log.Debug("OpenRPA disconnected from OpenRPA Windows Service");
        }
        private void Pipe_Connected(NamedPipeWrapper.NamedPipeConnection<RPAMessage, RPAMessage> connection)
        {
            Log.Debug("OpenRPA connected to OpenRPA Windows Service");
            var asm = System.Reflection.Assembly.GetEntryAssembly();
            var path = asm.CodeBase.Replace("file:///", "");
            pipe.PushMessage(new RPAMessage("hello", NativeMethods.GetProcessUserName(System.Diagnostics.Process.GetCurrentProcess().Id), global.webSocketClient.user, path));
        }
        public void ReloadConfig()
        {
            pipe.PushMessage(new RPAMessage("reloadconfig"));
            NotifyPropertyChanged("editor");
        }
        public void onWorkflowAborted(ref IWorkflowInstance e)
        {
        }
        public void onWorkflowCompleted(ref IWorkflowInstance e)
        {
        }
        public void onWorkflowIdle(ref IWorkflowInstance e)
        {
        }
        public bool onWorkflowResumeBookmark(ref IWorkflowInstance e, string bookmarkName, object value)
        {
            return true;
        }
        public bool onWorkflowStarting(ref IWorkflowInstance e, bool resumed)
        {
            return true;
        }
    }
}
