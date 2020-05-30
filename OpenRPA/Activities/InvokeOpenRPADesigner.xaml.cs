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
    public partial class InvokeOpenRPADesigner : INotifyPropertyChanged
    {
        public InvokeOpenRPADesigner()
        {
            InitializeComponent();
            workflows = new ObservableCollection<Workflow>();
            DataContext = this;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<Workflow> workflows { get; set; }
        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RobotInstance.instance == null) throw new ArgumentException("RobotInstance.instance");
                if (RobotInstance.instance.Projects == null) throw new ArgumentException("RobotInstance.instance.Projects");
                if (RobotInstance.instance.Projects.Count == 0) throw new ArgumentException("RobotInstance.instance.Projects.Count == 0");
                var result = new List<Workflow>();
                foreach (var p in RobotInstance.instance.Projects)
                {
                    foreach (var w in p.Workflows)
                    {
                        if(RobotInstance.instance.Window.Designer!=null && RobotInstance.instance.Window.Designer.Workflow != null)
                        {
                            if (RobotInstance.instance.Window.Designer.Workflow._id != w._id) workflows.Add(w);
                        } 
                        else
                        {
                            workflows.Add(w);
                        }
                        
                    }
                }
                result = result.OrderBy(x => x.name).OrderBy(x => x.Project.name).ToList();
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
            string workflowid = (string)ModelItem.Properties["workflow"].Value.GetCurrentValue();
            if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow property is null");
            var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(workflowid);
            var designer = RobotInstance.instance.Window.Designer;
            if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow is null, not found");
            if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("designer is null, cannot find current designer");
            ModelItemDictionary dictionary = base.ModelItem.Properties["Arguments"].Dictionary;
            foreach (var p in workflow.Parameters)
            {
                bool exists = false;
                foreach(var key in dictionary.Keys)
                {
                    if(key.ToString() == p.name) exists = true;
                    if (key.GetValue<string>("AnnotationText") == p.name) exists = true;
                    if (key.GetValue<string>("Name") == p.name) exists = true;
                }
                if(!exists)
                {

                    Type t = Type.GetType(p.type);
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
            foreach(var a in dictionary.ToList())
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
                string workflowid = (string)ModelItem.Properties["workflow"].Value.GetCurrentValue();
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