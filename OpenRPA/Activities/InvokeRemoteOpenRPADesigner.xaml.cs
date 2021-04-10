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
            workflows = new ObservableCollection<IWorkflow>();
            robots = new ObservableCollection<apiuser>();
            DataContext = this;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<IWorkflow> workflows { get; set; }
        public ObservableCollection<apiuser> robots { get; set; }
        private string originalworkflow = null;
        private string originaltarget = null;
        private void loadLocalWorkflows()
        {
            workflows.Clear();
            var result = new List<IWorkflow>();
            foreach (var p in RobotInstance.instance.Projects.FindAll())
            {
                foreach (var w in p.Workflows) result.Add(w);
            }
            result = result.OrderBy(x => x.name).OrderBy(x => x.Project.name).ToList();
            foreach (var w in result) workflows.Add(w);
        }
        private async void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (robots.Count() > 0) return;
                originalworkflow = ModelItem.GetValue<string>("workflow");
                originaltarget = ModelItem.GetValue<string>("target");
                //if (string.IsNullOrEmpty(originalworkflow)) throw new ArgumentException("ModelItem.workflow is null");
                //if (string.IsNullOrEmpty(originaltarget)) throw new ArgumentException("ModelItem.target is null");
                // loadLocalWorkflows();
                var _users = await global.webSocketClient.Query<apiuser>("users", "{\"$or\":[ {\"_type\": \"user\"}, {\"_type\": \"role\", \"rparole\": true} ]}", top: 5000);
                foreach (var u in _users) robots.Add(u);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ModelItem.Properties["workflow"].Value == null) return;
                string workflowid = ModelItem.GetValue<string>("workflow");
                var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(workflowid);
                var designer = RobotInstance.instance.Window.Designer;
                foreach(var p in workflow.Parameters)
                {
                    Type t = Type.GetType(p.type);
                    if (p.type == "System.Data.DataTable") t = typeof(System.Data.DataTable);
                    if (t == null) throw new ArgumentException("Failed resolving type '" + p.type + "'");
                    Log.Information("Checking for variable " + p.name + " of type " + p.type);
                    designer.GetVariable(p.name, t);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private string oldworkflow = null;
        private string oldtarget = null;
        private async void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                string workflow = ModelItem.GetValue<string>("workflow");
                string target = ModelItem.GetValue<string>("target");
                if (oldworkflow == workflow && oldtarget == target) return;
                oldworkflow = workflow; oldtarget = target;
                if (string.IsNullOrEmpty(target)) { loadLocalWorkflows(); return; }
                if (target != originaltarget)
                {
                    workflows.Clear();
                    workflows.Add(new Workflow() { name = "Loading...", _id = "loading" });
                    ModelItem.Properties["workflow"].SetValue(new InArgument<string>() { Expression = new Literal<string>("Loading...") });
                }
                // var _workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}", "{'name':1, 'projectandname': 1}", orderby: "{projectid:-1,name:-1}", queryas: target);
                var _workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}", queryas: target, top: 5000);
                _workflows = _workflows.OrderBy(x => x.ProjectAndName).ToArray();
                workflows.Clear();
                foreach (var w in _workflows) workflows.Add(w);
                var currentworkflow = ModelItem.GetValue<string>("workflow");
                if (workflow != currentworkflow)
                {
                    // ModelItem.Properties["workflow"].SetValue(workflow);
                    ModelItem.Properties["workflow"].SetValue(new InArgument<string>() { Expression = new Literal<string>(workflow) });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string workflowid = ModelItem.GetValue<string>("workflow");
            if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow property is null");
            var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(workflowid);
            var designer = RobotInstance.instance.Window.Designer;
            if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow is null, not found");
            if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("designer is null, cannot find current designer");
            ModelItemDictionary dictionary = base.ModelItem.Properties["Arguments"].Dictionary;
            foreach (var p in workflow.Parameters)
            {
                bool exists = false;
                foreach (var key in dictionary.Keys)
                {
                    if (key.ToString() == p.name) exists = true;
                    if (key.GetValue<string>("AnnotationText") == p.name) exists = true;
                    if (key.GetValue<string>("Name") == p.name) exists = true;
                }
                if (!exists)
                {

                    Type t = Type.GetType(p.type);
                    if (p.type == "System.Data.DataTable") t = typeof(System.Data.DataTable);
                    if (t == null) throw new ArgumentException("Failed resolving type '" + p.type + "'");


                    Type atype = typeof(VisualBasicValue<>);
                    Type constructed = atype.MakeGenericType(t);
                    object o = Activator.CreateInstance(constructed, p.name);

                    //Log.Information("Checking for variable " + p.name + " of type " + p.type);
                    //designer.GetVariable(p.name, t);
                    Argument a = null;
                    if (p.direction == workflowparameterdirection.@in) a = Argument.Create(t, ArgumentDirection.In);
                    if (p.direction == workflowparameterdirection.inout) a = Argument.Create(t, ArgumentDirection.InOut);
                    if (p.direction == workflowparameterdirection.@out) a = Argument.Create(t, ArgumentDirection.Out);
                    // a.GetType().GetProperties().Where(x => x.Name == "Expression").Last().SetValue(a, o);
                    //a.Expression = o as VisualBasicValue<>;
                    dictionary.Add(p.name, a);
                }
            }
            foreach (var a in dictionary.ToList())
            {
                bool exists = workflow.Parameters.Where(x => x.name == a.Key.ToString()).Count() > 0;
                if (!exists) dictionary.Remove(a.Key);
            }

            var options = new System.Activities.Presentation.DynamicArgumentDesignerOptions() { Title = "Map Arguments" };
            using (ModelEditingScope modelEditingScope = dictionary.BeginEdit())
            {
                if (System.Activities.Presentation.DynamicArgumentDialog.ShowDialog(base.ModelItem, dictionary, base.Context, base.ModelItem.View, options))
                {
                    modelEditingScope.Complete();
                }
                else
                {
                    modelEditingScope.Revert();
                }
            }
        }
    }
}