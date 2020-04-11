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
                    foreach (var w in p.Workflows) result.Add(w);
                }
                result = result.OrderBy(x => x.name).OrderBy(x => x.Project.name).ToList();
                foreach (var w in result) workflows.Add(w);
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
                string workflowid = (string)ModelItem.Properties["workflow"].Value.GetCurrentValue();
                if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow property is null");
                var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(workflowid);
                var designer = MainWindow.instance.Designer;
                if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("workflow is null, not found");
                if (string.IsNullOrEmpty(workflowid)) throw new ArgumentException("designer is null, cannot find current designer");
                foreach (var p in workflow.Parameters)
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
    }
}