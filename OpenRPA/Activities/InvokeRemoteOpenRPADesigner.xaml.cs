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

namespace OpenRPA.Activities
{
    public partial class InvokeRemoteOpenRPADesigner : INotifyPropertyChanged
    {
        public InvokeRemoteOpenRPADesigner()
        {
            InitializeComponent();
            workflows = new ObservableCollection<Workflow>();
            robots = new ObservableCollection<apiuser>();
            DataContext = this;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<Workflow> workflows { get; set; }
        public ObservableCollection<apiuser> robots { get; set; }
        private string originalworkflow = null;
        private string originaltarget = null;
        private void loadLocalWorkflows()
        {
            workflows.Clear();
            var result = new List<Workflow>();
            foreach (var p in MainWindow.instance.Projects)
            {
                foreach (var w in p.Workflows) result.Add(w);
            }
            result = result.OrderBy(x => x.name).OrderBy(x => x.Project.name).ToList();
            foreach (var w in result) workflows.Add(w);
        }
        private async void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            originalworkflow = ModelItem.GetValue<string>("workflow");
            originaltarget = ModelItem.GetValue<string>("target");
            // loadLocalWorkflows();
            var _users = await global.webSocketClient.Query<apiuser>("users", "{\"$or\":[ {\"_type\": \"user\"}, {\"_type\": \"role\", \"rparole\": true} ]}", top: 5000);
            foreach (var u in _users) robots.Add(u);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ModelItem.Properties["workflow"].Value == null) return;
                string workflowid = (string)ModelItem.Properties["workflow"].Value.GetCurrentValue();
                var workflow = MainWindow.instance.GetWorkflowByIDOrRelativeFilename(workflowid);
                var designer = MainWindow.instance.Designer;
                foreach(var p in workflow.Parameters)
                {
                    Type t = Type.GetType(p.type);
                    Log.Information("Checking for variable " + p.name + " of type " + p.type);
                    designer.GetVariable(p.name, t);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private async void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string workflow = ModelItem.GetValue<string>("workflow");
            string target = ModelItem.GetValue<string>("target");
            if (string.IsNullOrEmpty(target)) { loadLocalWorkflows();  return; }
            if(target != originaltarget)
            {
                workflows.Clear();
                workflows.Add(new Workflow() { name = "Loading...", _id = "loading" });
                ModelItem.Properties["workflow"].SetValue("loading");
            }
            // var _workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}", "{'name':1, 'projectandname': 1}", orderby: "{projectid:-1,name:-1}", queryas: target);
            var _workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}", queryas: target);
            _workflows = _workflows.OrderBy(x => x.ProjectAndName).ToArray();
            workflows.Clear();
            foreach (var w in _workflows) workflows.Add(w);
            var currentworkflow = ModelItem.GetValue<string>("workflow");
            if(workflow != currentworkflow && !string.IsNullOrEmpty(currentworkflow))
            {
                ModelItem.Properties["workflow"].SetValue(workflow);
            }
        }
    }
}