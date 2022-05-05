using Newtonsoft.Json.Linq;
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
            try
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public DelegateCommand DockAsDocumentCommand = new DelegateCommand((e) => { }, (e) => false);
        public DelegateCommand AutoHideCommand { get; set; } = new DelegateCommand((e) => { }, (e) => false);
        public bool CanClose { get; set; } = false;
        public bool CanHide { get; set; } = false;
        public event Action<IWorkflow> onOpenWorkflow;
        public event Action onSelectedItemChanged;
        private IMainWindow main = null;
        public List<System.Globalization.CultureInfo> Cultures { get; set; }
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
        public ICommand AddWorkItemQueueCommand { get { return new RelayCommand<object>(OnAddWorkItemQueue, CanAddWorkItemQueue); } }
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
        public ICommand GetServerVersionCommand
        {
            get
            {
                return new RelayCommand<object>(OnGetServerVersion, CanGetServerVersion);
            }
        }
        public ICommand SerializableCommand
        {
            get
            {
                return new RelayCommand<object>(OnSerializable, CanSerializable);
            }
        }
        public ICommand BackgroundCommand
        {
            get
            {
                return new RelayCommand<object>(OnBackground, CanBackground);
            }
        }
        public ICommand DisableCachingCommand
        {
            get
            {
                return new RelayCommand<object>(OnDisableCaching, CanDisableCaching);
            }
        }
        internal bool CanGetServerVersion(object _item)
        {
            try
            {
                if (RobotInstance.instance.Window is AgentWindow) return false;
                if (global.webSocketClient == null || !global.webSocketClient.isConnected) return false;
                if (main.SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null)
                    {
                        return false;
                    }
                    if (view.listWorkflows.SelectedValue is Workflow workflow)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        internal async void OnGetServerVersion(object _item)
        {
            if (main.SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (!(view.listWorkflows.SelectedValue is Workflow workflow)) return;
                var server_workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{'_id': '" + workflow._id + "'}", null, top: 1);
                if (server_workflows.Length > 0)
                {
                    await server_workflows[0].Save();
                }
            }
        }
        internal bool CanSerializable(object _item)
        {
            try
            {
                if (RobotInstance.instance.Window is AgentWindow) return false;
                if (main.SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (view.listWorkflows.SelectedValue is Workflow f) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        internal async void OnSerializable(object _item)
        {
            if (main.SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (view.listWorkflows.SelectedValue is Workflow f)
                {
                    await f.Save();
                }
            }
        }
        internal bool CanBackground(object _item)
        {
            try
            {
                if (RobotInstance.instance.Window is AgentWindow) return false;
                if (main.SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (view.listWorkflows.SelectedValue is Workflow f)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        internal async void OnBackground(object _item)
        {
            if (main.SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (view.listWorkflows.SelectedValue is Workflow f)
                {
                    await f.Save();
                }
            }
        }
        internal bool CanDisableCaching(object _item)
        {
            try
            {
                if (RobotInstance.instance.Window is AgentWindow) return false;
                if (global.webSocketClient == null || !global.webSocketClient.isConnected) return false;
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
        public System.Collections.ObjectModel.ObservableCollection<IProject> Projects
        {
            get
            {
                return RobotInstance.instance.Projects;
            }
        }
        public static OpenProject Instance;
        public static bool isUpdating = false;
        public static bool isUpdateProjectsList = false;
        public OpenProject(IMainWindow main)
        {
            Cultures = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.InstalledWin32Cultures).ToList();
            Instance = this;
            Log.FunctionIndent("OpenProject", "OpenProject");
            try
            {
                InitializeComponent();
                // https://stackoverflow.com/questions/50922465/how-to-sort-the-child-nodes-of-treeview
                listWorkflows.Items.SortDescriptions.Add(new SortDescription("name", ListSortDirection.Ascending));

                if (RobotInstance.instance.Window is AgentWindow) EditXAMLPanel.Visibility = Visibility.Hidden;
                if (RobotInstance.instance.Window is AgentWindow) PackageManagerPanel.Visibility = Visibility.Hidden;
                this.main = main;
                DataContext = this;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("OpenProject", "OpenProject");
        }
        public static bool isFiltering = false;
        public static bool ReFilter = false;
        async private void ListWorkflows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Log.FunctionIndent("OpenProject", "ListWorkflows_MouseDoubleClick");
            try
            {
                if (listWorkflows.SelectedItem is Workflow f)
                {
                    onOpenWorkflow?.Invoke(f);
                    return;
                }
                if (listWorkflows.SelectedItem is Detector d)
                {
                    if (main is MainWindow m)
                    {
                        var op = await m.OpenDetectors();
                        if (op != null)
                        {
                            foreach (IDetectorPlugin dd in op.lidtDetectors.Items)
                            {
                                if (dd.Entity._id == d._id) op.lidtDetectors.SelectedItem = dd;
                            }

                        }
                    }
                    return;
                }
                if (listWorkflows.SelectedItem is OpenRPA.WorkitemQueue wiq)
                {
                    if (main is MainWindow m)
                    {
                        var op = m.OpenWorkItemQueues();
                        if (op != null)
                        {
                            foreach (OpenRPA.WorkitemQueue _wiq in op.listWorkItemQueues.Items)
                            {
                                if (_wiq._id == wiq._id) op.listWorkItemQueues.SelectedItem = _wiq;
                            }

                        }
                    }
                    return;
                }
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
            //NotifyPropertyChanged("Workflow");
            //NotifyPropertyChanged("Project");
            NotifyPropertyChanged("IsWorkflowSelected");
            NotifyPropertyChanged("IsWorkflowOrProjectSelected");
            // NotifyPropertyChanged("Projects");
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
                        if(project.isDirty)
                        {
                            await project.Save();
                        }
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
                        if (p.isDirty)
                        {
                            await p.Save();
                        }
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
        private void StartDrag(MouseEventArgs e)
        {
            IsDragging = true;
            DependencyObject dependencyObject = listWorkflows.InputHitTest(e.GetPosition(listWorkflows)) as DependencyObject;
            if (dependencyObject is TextBlock)
            {
                if (listWorkflows.SelectedValue is Workflow wf)
                {
                    if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DragDrop.DoDragDrop(listWorkflows, wf, DragDropEffects.Copy);
                    }
                    else
                    {
                        DragDrop.DoDragDrop(listWorkflows, wf, DragDropEffects.Move);
                    }
                    e.Handled = true;
                }
                if (listWorkflows.SelectedValue is Detector d)
                {
                    if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DragDrop.DoDragDrop(listWorkflows, d, DragDropEffects.Copy);
                    }
                    else
                    {
                        DragDrop.DoDragDrop(listWorkflows, d, DragDropEffects.Move);
                    }
                    e.Handled = true;
                }
                // DataObject data = new DataObject(System.Windows.DataFormats.Text.ToString(), "abcd");
                // DragDropEffects de = DragDrop.DoDragDrop(tvi, data, DragDropEffects.Move);
            }
            IsDragging = false;
            return;
        }
        Point _startPoint;
        bool IsDragging = false;
        private void listWorkflows_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }
        private void listWorkflows_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !IsDragging)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    StartDrag(e);
                }
            }
        }
        private async void listWorkflows_Drop(object sender, DragEventArgs e)
        {
            try
            {
                Workflow wf = e.Data.GetData(typeof(Workflow)) as Workflow;
                Detector d = e.Data.GetData(typeof(Detector)) as Detector;
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                TreeViewItem target = null;
                DependencyObject k = VisualTreeHelper.HitTest(listWorkflows, e.GetPosition(listWorkflows)).VisualHit;
                while (k != null)
                {
                    if (k is TreeViewItem treeNode)
                    {
                        target = treeNode;

                    }
                    k = VisualTreeHelper.GetParent(k);
                }
                if (target == null) return;
                Project p = null;
                if (target.DataContext is Workflow targetwf)
                {
                    p = targetwf.Project() as Project;
                }
                if (target.DataContext is Project targetp)
                {
                    p = targetp;
                }
                if (d != null)
                {
                    e.Handled = true;
                    if (target != null)
                    {
                        if (p != null && p._id != d.projectid)
                        {
                            if (e.Effects == DragDropEffects.Copy)
                            {
                                d = JObject.FromObject(d).ToObject<Detector>();
                                d._id = null;
                            }
                            d.projectid = p._id;
                            d.isDirty = true;
                            d._acl = p._acl;
                            await d.Save();
                            d.Start(true);
                        }
                    }
                }
                if (wf != null)
                {
                    wf = RobotInstance.instance.Workflows.FindById(wf._id) as Workflow;

                    e.Handled = true;
                    if (target != null)
                    {
                        if (p != null && p._id != wf.projectid)
                        {
                            if (string.IsNullOrEmpty(wf.Filename)) wf.Filename = wf.UniqueFilename();
                            var nameexists = p.Workflows.Where(x => x.name == wf.name).ToList();
                            var filenameexists = p.Workflows.Where(x => x.Filename == wf.Filename).ToList();
                            if (nameexists.Count() == 1)
                            {
                                var messageBoxResult = MessageBox.Show("Project " + p.name + " already contains a workflow with name " + Workflow.name +
                                ", do you wish to overwrite this workflow (Yes)? or keep them seperate (no)", "Workflow already exists", MessageBoxButton.YesNo);
                                if (messageBoxResult == MessageBoxResult.Yes)
                                {
                                    nameexists[0].Xaml = wf.Xaml;
                                    nameexists[0].ParseParameters();
                                    if (filenameexists.Count() > 0) wf.Filename = wf.UniqueFilename();
                                    await nameexists[0].Save();
                                    // await wf.Delete();
                                }
                                else
                                {
                                    if (e.Effects == DragDropEffects.Copy)
                                    {
                                        wf = JObject.FromObject(wf).ToObject<Workflow>();
                                        wf._id = null;
                                        wf.projectid = p._id;
                                        wf.Filename = wf.UniqueFilename();
                                    }
                                    wf.isDirty = true;
                                    wf.Filename = wf.UniqueFilename();
                                    wf.projectid = p._id;
                                    wf._acl = p._acl;
                                    await wf.Save();
                                    await wf.UpdateImagePermissions();
                                }

                            }
                            else if (filenameexists.Count() == 1)
                            {
                                var messageBoxResult = MessageBox.Show("Project " + p.name + " already contains a workflow with filename " + Workflow.Filename + " with name \"" + Workflow.name + "\" " +
                                ", do you wish to overwrite this workflow (Yes)? or keep them seperate (no)", "Workflow already exists", MessageBoxButton.YesNo);
                                if (messageBoxResult == MessageBoxResult.Yes)
                                {
                                    filenameexists[0].Xaml = wf.Xaml;
                                    filenameexists[0].ParseParameters();
                                    filenameexists[0].Filename = filenameexists[0].UniqueFilename();
                                    await filenameexists[0].Save();
                                    // await wf.Delete();
                                }
                                else
                                {
                                    if (e.Effects == DragDropEffects.Copy)
                                    {
                                        wf = JObject.FromObject(wf).ToObject<Workflow>();
                                        wf._id = null;
                                    }
                                    wf.projectid = p._id;
                                    wf._acl = p._acl;
                                    wf.Filename = wf.UniqueFilename();
                                    wf.isDirty = true;
                                    await wf.Save();
                                    await wf.UpdateImagePermissions();
                                }
                            }
                            else
                            {
                                if (e.Effects == DragDropEffects.Copy)
                                {
                                    wf = JObject.FromObject(wf).ToObject<Workflow>();
                                    wf._id = null;
                                    wf.projectid = p._id;
                                    wf.UniqueFilename();
                                }
                                wf.projectid = p._id;
                                wf._acl = p._acl;
                                await wf.Save();
                                await wf.UpdateImagePermissions();
                            }
                        }
                    }
                }
                if (files != null && files.Length > 0)
                {
                    e.Handled = true;
                    foreach (var filename in files)
                    {
                        if (!System.IO.File.Exists(filename)) continue;
                        if (System.IO.Path.GetExtension(filename) == ".xaml")
                        {
                            var name = System.IO.Path.GetFileNameWithoutExtension(filename);
                            Workflow workflow = await Workflow.Create(p, name);
                            workflow.Xaml = System.IO.File.ReadAllText(filename);
                            wf._acl = p._acl;
                            await workflow.Save();
                            onOpenWorkflow?.Invoke(workflow);
                        }
                        if (System.IO.Path.GetExtension(filename) == ".json")
                        {
                            var json = System.IO.File.ReadAllText(filename);
                            Detector _d = Newtonsoft.Json.JsonConvert.DeserializeObject<Detector>(json);
                            _d._acl = p._acl;
                            var exists = RobotInstance.instance.dbDetectors.FindById(_d._id);
                            if (exists != null) { _d._id = null; } else { _d.isLocalOnly = true; }
                            _d.projectid = p._id;
                            await _d.Save();
                            _d.Start(true);
                        }
                    }
                }
                IsDragging = false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        internal bool CanAddWorkItemQueue(object _item)
        {
            try
            {
                if (RobotInstance.instance.Window is AgentWindow) return false;
                //if (global.webSocketClient == null || !global.webSocketClient.isConnected) return false;
                if (main.SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null)
                    {
                        return false;
                    }
                    if (view.listWorkflows.SelectedValue is Project p)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        internal async void OnAddWorkItemQueue(object _item)
        {
            if (main.SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (view.listWorkflows.SelectedValue is Project p)
                {
                    try
                    {
                        var dia = new OpenRPA.Views.WorkitemQueue();
                        dia.item = new OpenRPA.WorkitemQueue();
                        dia.ShowDialog();
                        if (dia.DialogResult == true)
                        {
                            dia.item._acl = p._acl;
                            dia.item.projectid = p._id;
                            await dia.item.Save();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        MessageBox.Show(ex.Message);
                    }

                }
            }
        }
    }
}
