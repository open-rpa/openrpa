using OpenRPA.Interfaces;
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
    public partial class AgentViewProjects : UserControl
    {
        public DelegateCommand DockAsDocumentCommand = new DelegateCommand((e) => { }, (e) => false);
        public DelegateCommand AutoHideCommand { get; set; } = new DelegateCommand((e) => { }, (e) => false);
        public bool CanClose { get; set; } = false;
        public bool CanHide { get; set; } = false;
        public event Action<Workflow> onOpenWorkflow;
        public event Action<Project> onOpenProject;
        //public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; }
        private AgentWindow main = null;
        public ICommand PlayCommand { get { return new RelayCommand<object>(AgentWindow.instance.OnPlay, AgentWindow.instance.CanPlay); } }
        //public ICommand ExportCommand { get { return new RelayCommand<object>(AgentWindow.instance.OnExport, AgentWindow.instance.CanExport); } }
        //public ICommand RenameCommand { get { return new RelayCommand<object>(AgentWindow.instance.OnRename, AgentWindow.instance.CanRename); } }
        //public ICommand DeleteCommand { get { return new RelayCommand<object>(AgentWindow.instance.OnDelete2, AgentWindow.instance.CanDelete); } }
        //public ICommand DeleteCommand { get { return new RelayCommand<object>(AgentWindow.instance.OnDelete, AgentWindow.instance.CanDelete); } }
        //public ICommand CopyIDCommand { get { return new RelayCommand<object>(AgentWindow.instance.OnCopyID, AgentWindow.instance.CanCopyID); } }
        //public ICommand CopyRelativeFilenameCommand { get { return new RelayCommand<object>(AgentWindow.instance.OnCopyRelativeFilename, AgentWindow.instance.CanCopyID); } }
        public System.Collections.ObjectModel.ObservableCollection<Project> Projects
        {
            get
            {
                return RobotInstance.instance.Projects;
            }
        }
        public AgentViewProjects(AgentWindow main)
        {
            Log.FunctionIndent("AgentViewProjects", "AgentViewProjects");
            InitializeComponent();
            this.main = main;
            DataContext = this;
            Log.FunctionOutdent("AgentViewProjects", "AgentViewProjects");
        }
        private void ListWorkflows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Log.FunctionIndent("AgentViewProjects", "ListWorkflows_MouseDoubleClick");
            try
            {
                if (listWorkflows.SelectedItem is Workflow f)
                {
                    onOpenWorkflow?.Invoke(f);
                    return;
                }
                var p = (Project)listWorkflows.SelectedItem;
                if (p == null) return;
                onOpenProject?.Invoke(p);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("AgentViewProjects", "ListWorkflows_MouseDoubleClick");
        }
        private async void ButtonEditXAML(object sender, RoutedEventArgs e)
        {
            Log.FunctionIndent("AgentViewProjects", "ButtonEditXAML");
            try
            {
                if (listWorkflows.SelectedItem is Workflow workflow)
                {
                    try
                    {
                        var f = new EditXAML();
                        f.XAML = workflow.Xaml;
                        f.ShowDialog();
                        workflow.Xaml = f.XAML;
                        await workflow.Save(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        System.Windows.MessageBox.Show("ButtonEditXAML: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("AgentViewProjects", "ButtonEditXAML");
        }
        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
            }
        }
    }
}
