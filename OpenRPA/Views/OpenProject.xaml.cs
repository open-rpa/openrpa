using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for OpenProject.xaml
    /// </summary>
    public partial class OpenProject : UserControl
    {
        public event Action<Workflow> onOpenWorkflow;
        public event Action<Project> onOpenProject;
        //public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; }
        private MainWindow main = null;

        public System.Collections.ObjectModel.ObservableCollection<Project> Projects
        {
            get
            {
                return main.Projects;
            }
        }
        public OpenProject(MainWindow main)
        {
            InitializeComponent();
            this.main = main;
            DataContext = this;
        }
        private void ListWorkflows_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
        private void ListWorkflows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(listWorkflows.SelectedItem is Workflow)
            {
                var f = (Workflow)listWorkflows.SelectedItem;
                onOpenWorkflow?.Invoke(f);
                return;
            }
            var p = (Project)listWorkflows.SelectedItem;
            if (p == null) return;
            onOpenProject?.Invoke(p);

        }

    }
}
