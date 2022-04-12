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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Activities
{
    public partial class StopOpenRPADesigner : INotifyPropertyChanged
    {
        public StopOpenRPADesigner()
        {
            InitializeComponent();
            workflows = new ObservableCollection<IWorkflow>();
            DataContext = this;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<IWorkflow> workflows { get; set; }
        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RobotInstance.instance == null) throw new ArgumentException("RobotInstance.instance");
                if (RobotInstance.instance.Projects == null) throw new ArgumentException("RobotInstance.instance.Projects");
                if (RobotInstance.instance.Projects.Count() == 0) throw new ArgumentException("RobotInstance.instance.Projects.Count == 0");
                var result = new List<Workflow>();
                var designer = RobotInstance.instance.Window.Designer;
                foreach (var w in RobotInstance.instance.Workflows)
                {
                    if (designer != null && designer.Workflow != null)
                    {
                        if (designer.Workflow._id != w._id || w._id == null) result.Add(w as Workflow);
                    }
                    else
                    {
                        result.Add(w as Workflow);
                    }
                }
                result = result.OrderBy(x => x.name).OrderBy(x => x.Project().name).ToList();
                foreach (var w in result) workflows.Add(w);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            if (ModelItem.Properties["workflow"].Value == null) return;
            string workflowid = ModelItem.GetValue<string>("workflow");
            if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow property is null");
            var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(workflowid);
            var designer = RobotInstance.instance.Window.Designer;
            if (workflow == null) throw new ArgumentException("workflow is null, not found");
            if (designer == null) throw new ArgumentException("designer is null, cannot find current designer");
            ModelItemDictionary dictionary = base.ModelItem.Properties["Arguments"].Dictionary;
            try
            {
                workflow.ParseParameters();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                MessageBox.Show("Failed mapping arguments" + Environment.NewLine + ex.Message);
            }
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

                    Type t = OpenRPA.Interfaces.Extensions.FindType(p.type);
                    if (p.type == "System.Data.DataTable") t = typeof(System.Data.DataTable);
                    if (t == null) throw new ArgumentException("Failed resolving type '" + p.type + "'");

                    //Type atype = typeof(VisualBasicValue<>);
                    //Type constructed = atype.MakeGenericType(t);
                    //object o = Activator.CreateInstance(constructed, p.name);

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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string workflowid = ModelItem.GetValue<string>("workflow");
                if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow property is null");
                var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(workflowid);
                var designer = RobotInstance.instance.Window.Designer;
                if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow is null, not found");
                if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("designer is null, cannot find current designer");
                foreach (var p in workflow.Parameters)
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
    }
}