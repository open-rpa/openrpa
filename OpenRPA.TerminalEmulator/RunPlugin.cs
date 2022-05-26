using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.TerminalEmulator
{
    class RunPlugin : ObservableObject, IRunPlugin
    {
        private Views.RunPluginView view;
        public UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.RunPluginView();
                    view.PropertyChanged += (s, e) =>
                    {
                        NotifyPropertyChanged("Entity");
                        NotifyPropertyChanged("Name");
                    };
                }
                return view;
            }
        }
        public static string PluginName => "TerminalEmulator";
        public string Name => PluginName;
        public static List<Interfaces.VT.ITerminalSession> Sessions = new List<Interfaces.VT.ITerminalSession>();
        public IOpenRPAClient client;
        public static TerminalRecorder GetRecorderWindow(Interfaces.VT.ITerminalConfig Config)
        {
            TerminalRecorder win = null;
            foreach (var session in RunPlugin.Sessions)
            {
                if (session is TerminalRecorder _win)
                {
                    if (_win.Config.Hostname == Config.Hostname &&
                        _win.Config.Port == Config.Port &&
                        _win.Config.TermType == Config.TermType)
                    {
                        win = _win;
                    }
                }
            }
            if (win == null)
            {
                win = new TerminalRecorder();
                win.Config = Config;
                Sessions.Add(win);
            }
            return win;
        }
        public void Initialize(IOpenRPAClient client)
        {
            _ = PluginConfig.auto_close;
            this.client = client;
        }
        public void CleanUp(ref IWorkflowInstance e)
        {
            if (!PluginConfig.auto_close) return;
            foreach (var sesion in Sessions)
            {
                if (sesion.WorkflowInstanceId == e.InstanceId)
                {
                    try
                    {
                        GenericTools.RunUI(sesion.Close);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        public void onWorkflowAborted(ref IWorkflowInstance e)
        {
            CleanUp(ref e);
        }
        public void onWorkflowCompleted(ref IWorkflowInstance e)
        {
            CleanUp(ref e);
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
