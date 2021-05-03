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
        }
        public void ReloadConfig()
        {
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
