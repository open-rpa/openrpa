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
            //var _workflows = await global.webSocketClient.Query<Workflow>("workflow", "{_type: 'workflow'}");
            //workflows.Clear();
            foreach (var p in MainWindow.instance.Projects)
            {
                foreach(var w in p.Workflows) workflows.Add(w);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string workflowid = (string)ModelItem.Properties["workflow"].Value.GetCurrentValue();
                var workflow = MainWindow.instance.GetWorkflowById(workflowid);
                var designer = MainWindow.instance.designer;
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
    }
}