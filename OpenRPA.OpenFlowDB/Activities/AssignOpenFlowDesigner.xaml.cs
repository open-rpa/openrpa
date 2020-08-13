using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.OpenFlowDB
{
    public partial class AssignOpenFlowDesigner : INotifyPropertyChanged
    {
        public AssignOpenFlowDesigner()
        {
            InitializeComponent();
            workflows = new ObservableCollection<apibase>();
            robots = new ObservableCollection<apiuser>();
            DataContext = this;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<apibase> workflows { get; set; }
        public ObservableCollection<apiuser> robots { get; set; }
        private string originalworkflow = null;
        private string originaltarget = null;
        private async void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                originalworkflow = ModelItem.GetValue<string>("workflowid");
                originaltarget = ModelItem.GetValue<string>("targetid");

                var _users = await global.webSocketClient.Query<apiuser>("users", "{\"$or\":[ {\"_type\": \"user\"}, {\"_type\": \"role\", \"rparole\": true} ]}", top: 5000);
                foreach (var u in _users) robots.Add(u);
                //var _workflows = await global.webSocketClient.Query<openflowworkflow>("workflow", "{_type: 'workflow', rpa: true}");
                //_workflows = _workflows.OrderBy(x => x.name).ToArray();
                //workflows.Clear();
                //foreach (var w in _workflows)
                //{
                //    workflows.Add(w);
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private async void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                string workflowid = ModelItem.GetValue<string>("workflowid");
                string targetid = ModelItem.GetValue<string>("targetid");
                // if (string.IsNullOrEmpty(targetid)) { loadLocalWorkflows(); return; }
                if (targetid != originaltarget)
                {
                    workflows.Clear();
                    workflows.Add(new apibase() { name = "Loading...", _id = "loading" });
                    ModelItem.Properties["workflowid"].SetValue("loading");
                }
                var _workflows = await global.webSocketClient.Query<IWorkflow>("workflow", "{_type: 'workflow'}", queryas: targetid);
                _workflows = _workflows.OrderBy(x => x.ProjectAndName).ToArray();
                workflows.Clear();
                foreach (var w in _workflows) workflows.Add(w as apibase);
                var currentworkflow = ModelItem.GetValue<string>("workflowid");
                if (workflowid != currentworkflow && !string.IsNullOrEmpty(currentworkflow))
                {
                    ModelItem.Properties["workflowid"].SetValue(workflowid);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}