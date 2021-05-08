using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class OpenProject : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public DelegateCommand DockAsDocumentCommand = new DelegateCommand((e) => { }, (e) => false);
        public DelegateCommand AutoHideCommand { get; set; } = new DelegateCommand((e) => { }, (e) => false);
        public bool CanClose { get; set; } = false;
        public bool CanHide { get; set; } = false;
        public event Action<IWorkflow> onOpenWorkflow;
        // public event Action<Project> onOpenProject;
        public event Action onSelectedItemChanged;
        //public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; }
        private IMainWindow main = null;
        public ICommand PlayCommand
        {
            get
            {
                if (RobotInstance.instance.Window is MainWindow main) return new RelayCommand<object>(main.OnPlay, main.CanPlay);
                if (RobotInstance.instance.Window is AgentWindow agent) return new RelayCommand<object>(agent.OnPlay, agent.CanPlay);
                return null;
            }
        }
        public ICommand ExportCommand
        {
            get
            {
                if (RobotInstance.instance.Window is MainWindow main) return new RelayCommand<object>(main.OnExport, main.CanExport);
                return null;
            }
        }
        public ICommand RenameCommand
        {
            get
            {
                if (RobotInstance.instance.Window is MainWindow main) return new RelayCommand<object>(main.OnRename, main.CanRename);
                return null;
            }
        }
        public ICommand DeleteCommand
        {
            get
            {
                if (RobotInstance.instance.Window is MainWindow main) return new RelayCommand<object>(main.OnDelete2, main.CanDelete);
                return null;
            }
        }
        // public ICommand DeleteCommand { get { return new RelayCommand<object>(MainWindow.instance.OnDelete, MainWindow.instance.CanDelete); } }
        public ICommand CopyIDCommand
        {
            get
            {
                if (RobotInstance.instance.Window is MainWindow main) return new RelayCommand<object>(main.OnCopyID, main.CanCopyID);
                return null;
            }
        }
        public ICommand CopyRelativeFilenameCommand
        {
            get
            {
                if (RobotInstance.instance.Window is MainWindow main) return new RelayCommand<object>(main.OnCopyRelativeFilename, main.CanCopyID);
                return null;
            }
        }
        public ICommand DisableCachingCommand
        {
            get
            {
                return new RelayCommand<object>(OnDisableCaching, CanDisableCaching);
            }
        }
        internal bool CanDisableCaching(object _item)
        {
            try
            {
                if (RobotInstance.instance.Window is AgentWindow) return false;
                if (!global.webSocketClient.isConnected) return false;
                if (main.SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (view.listWorkflows.SelectedValue is Project p) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        internal async void OnDisableCaching(object _item)
        {
            if (main.SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (view.listWorkflows.SelectedValue is Project project)
                {
                    await project.Save();
                }
            }
        }
        private System.Collections.ObjectModel.ObservableCollection<IProject> _Projects = new System.Collections.ObjectModel.ObservableCollection<IProject>();
        public System.Collections.ObjectModel.ObservableCollection<IProject> Projects
        {
            get
            {
                return _Projects;
            }
        }
        public static OpenProject Instance;
        public static bool isUpdating = false;
        public static void UpdateProjectsList()
        {
            try
            {
                if (Instance == null) { Log.Debug("UpdateProjectsList, Instance is null"); return; }
                if (!string.IsNullOrEmpty(Instance.FilterText))
                {
                    foreach (var p in Instance.Projects)
                    {
                        // result[i].NotifyPropertyChanged("FilteredWorkflows");
                        //result[i].FilteredSource.Refresh();
                        //if (result[i].FilteredSource.Count > 0) result[i].UpdateWorkflowsList();
                        p.NotifyPropertyChanged("FilteredWorkflows");
                    }

                    return;
                }
                var result = RobotInstance.instance.Projects.FindAll().OrderBy(x => x.name).ToList();
                for (var i = 0; i < result.Count; i++)
                {
                    result[i].UpdateWorkflowsList();
                }
                GenericTools.RunUI(() =>
                {
                    isUpdating = true;
                    try
                    {
                        if (Instance != null) Instance._Projects.UpdateCollection(result);
                        System.Windows.Input.CommandManager.InvalidateRequerySuggested();

                    }
                    catch (Exception)
                    {
                    }
                    isUpdating = false;
                });
            }
            catch (Exception)
            {
            }
        }
        public OpenProject(IMainWindow main)
        {
            Instance = this;
            Log.FunctionIndent("OpenProject", "OpenProject");
            try
            {
                InitializeComponent();
                if (RobotInstance.instance.Window is AgentWindow) EditXAMLPanel.Visibility = Visibility.Hidden;
                if (RobotInstance.instance.Window is AgentWindow) PackageManagerPanel.Visibility = Visibility.Hidden;
                this.main = main;
                DataContext = this;
                RobotInstance.instance.PropertyChanged += Instance_PropertyChanged;
                UpdateProjectsList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("OpenProject", "OpenProject");
        }
        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Projects") NotifyPropertyChanged("Projects");
        }

        public static bool isFiltering = false;
        public static bool ReFilter = false;
        private string _FilterText = "";
        public string FilterText
        {
            get
            {
                return _FilterText;
            }
            set
            {
                _FilterText = value;
                if (isFiltering)
                {
                    ReFilter = true;
                    return;
                }
                ReFilter = false;
                isFiltering = true;
                isUpdating = true;
                foreach (Project p in _Projects)
                {
                    p.IsExpanded = string.IsNullOrEmpty(_FilterText) ? false : p.FilteredSource.Count > 0;
                }
                isUpdating = false;
                // UpdateProjectsList();
                foreach (var p in Instance.Projects) p.NotifyPropertyChanged("FilteredWorkflows");
                NotifyPropertyChanged("FilterText");
                NotifyPropertyChanged("Projects");
                if (ReFilter)
                {
                    isUpdating = true;
                    foreach (Project p in _Projects)
                    {
                        p.IsExpanded = string.IsNullOrEmpty(_FilterText) ? false : p.FilteredSource.Count > 0;
                    }
                    isUpdating = false;
                    // UpdateProjectsList();
                    foreach (var p in Instance.Projects) p.NotifyPropertyChanged("FilteredWorkflows");
                    NotifyPropertyChanged("FilterText");
                    NotifyPropertyChanged("Projects");
                }
                isFiltering = false;
            }
        }
        private void ListWorkflows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Log.FunctionIndent("OpenProject", "ListWorkflows_MouseDoubleClick");
            try
            {
                if (listWorkflows.SelectedItem is Workflow f)
                {
                    var freshwf = RobotInstance.instance.Workflows.FindById(f._id);
                    onOpenWorkflow?.Invoke(freshwf);
                    return;
                }
                //var p = (Project)listWorkflows.SelectedItem;
                //if (p == null) return;
                //onOpenProject?.Invoke(p);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("OpenProject", "ListWorkflows_MouseDoubleClick");
        }
        private async void ButtonEditXAML(object sender, RoutedEventArgs e)
        {
            Log.FunctionIndent("OpenProject", "ButtonEditXAML");
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
                        await workflow.Save();
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
            Log.FunctionOutdent("OpenProject", "ButtonEditXAML");
        }
        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            Log.FunctionIndent("OpenProject", "UserControl_KeyUp");
            try
            {
                if (e.Key == Key.F2)
                {
                    if (RobotInstance.instance.Window is MainWindow main) main.OnRename(null);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("OpenProject", "UserControl_KeyUp");
        }
        public Workflow Workflow
        {
            get
            {
                if (listWorkflows.SelectedItem == null) return null;
                if (listWorkflows.SelectedItem is Project) return null;
                if (listWorkflows.SelectedItem is Workflow) return listWorkflows.SelectedItem as Workflow;
                return null;

            }
            set
            {
            }
        }
        public Project Project
        {
            get
            {
                if (listWorkflows.SelectedItem == null) return null;
                if (listWorkflows.SelectedItem is Project) return listWorkflows.SelectedItem as Project;
                if (listWorkflows.SelectedItem is Workflow wf) return wf.Project() as Project;
                return null;
            }
            set
            {
            }
        }
        private void listWorkflows_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (isUpdating) return;
            NotifyPropertyChanged("Workflow");
            NotifyPropertyChanged("Project");
            NotifyPropertyChanged("IsWorkflowSelected");
            NotifyPropertyChanged("IsWorkflowOrProjectSelected");
            onSelectedItemChanged?.Invoke();
        }
        public bool IsWorkflowSelected
        {
            get
            {
                if (listWorkflows.SelectedItem == null) return false;
                if (listWorkflows.SelectedItem is Workflow wf) return true;
                return false;
            }
            set { }
        }
        public bool IsWorkflowOrProjectSelected
        {
            get
            {
                if (listWorkflows.SelectedItem == null) return false;
                if (listWorkflows.SelectedItem is Workflow wf) return true;
                if (listWorkflows.SelectedItem is Project p) return true;
                return false;
            }
            set { }
        }
        public bool IncludePrerelease { get; set; }
        private async void ButtonOpenPackageManager(object sender, RoutedEventArgs e)
        {
            Log.FunctionIndent("OpenProject", "ButtonOpenPackageManager");
            try
            {
                if (listWorkflows.SelectedItem is Project project)
                {
                    try
                    {
                        var f = new PackageManager(project);
                        if (RobotInstance.instance.Window is MainWindow main) f.Owner = main;
                        f.ShowDialog();
                        if (f.NeedsReload)
                        {
                            await project.InstallDependencies(true);
                            WFToolbox.Instance.InitializeActivitiesToolbox();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        System.Windows.MessageBox.Show("ButtonOpenPackageManager: " + ex.Message);
                    }
                }
                if (listWorkflows.SelectedItem is Workflow workflow)
                {
                    try
                    {
                        Project p = workflow.Project() as Project;
                        var f = new PackageManager(p);
                        if (RobotInstance.instance.Window is MainWindow main) f.Owner = main;
                        f.ShowDialog();
                        if (f.NeedsReload)
                        {
                            await p.InstallDependencies(true);
                            WFToolbox.Instance.InitializeActivitiesToolbox();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        System.Windows.MessageBox.Show("ButtonOpenPackageManager: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("OpenProject", "ButtonEditXAML");
        }
    }
}
