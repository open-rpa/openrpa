using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Debugger;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.Services;
using System.Activities.Presentation.Toolbox;
using System.Activities.Presentation.View;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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
using System.Xml;

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for WFDesigner.xaml
    /// </summary>
    public partial class WFDesigner : UserControl, System.ComponentModel.INotifyPropertyChanged, IDesigner
    {
        public DelegateCommand DockAsDocumentCommand = new DelegateCommand((e) => { }, (e) => false);
        public DelegateCommand AutoHideCommand { get; set; } = new DelegateCommand((e) => { }, (e) => false);
        public bool CanClose { get; set; } = true;
        public bool CanHide { get; set; } = false;
        public Dictionary<ModelItem, SourceLocation> _modelLocationMapping = new Dictionary<ModelItem, SourceLocation>();
        public Dictionary<string, SourceLocation> _sourceLocationMapping = new Dictionary<string, SourceLocation>();
        public Dictionary<string, Activity> _activityIdMapping = new Dictionary<string, Activity>();
        public Dictionary<Activity, SourceLocation> _activitysourceLocationMapping = new Dictionary<Activity, SourceLocation>();
        public Dictionary<string, ModelItem> _activityIdModelItemMapping = new Dictionary<string, ModelItem>();
        private string SelectedVariableName = null;
        private Selection selection = null;
        private readonly MenuItem runthis;
        private readonly MenuItem runFromHere;
        private readonly MenuItem comment;
        private readonly MenuItem uncomment;
        public bool BreakPointhit { get; set; }
        public bool Singlestep { get; set; }
        public bool SlowMotion { get; set; }
        public bool VisualTracking { get; set; }
        public bool IsRunnning
        {
            get
            {
                foreach (var i in WorkflowInstance.Instances.ToList())
                {
                    if (!string.IsNullOrEmpty(Workflow._id) && i.WorkflowId == Workflow._id)
                    {
                        if (i.state != "completed" && i.state != "aborted" && i.state != "failed")
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        private object _Properties;
        public object Properties
        {
            get
            {
                return _Properties;
            }
            set
            {
                _Properties = value;
                NotifyPropertyChanged("Properties");
            }
        }
        public bool IsSelected
        {
            get
            {
                return tab.IsSelected;
            }
            set
            {
                if (tab.IsSelected) return;
                tab.IsSelected = true;
            }
        }
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public System.Threading.AutoResetEvent ResumeRuntimeFromHost { get; set; }
        public Activity Lastinserted { get; set; }
        public ModelItem Lastinsertedmodel { get; set; }
        public Action<WFDesigner> OnChanged { get; set; }
        public WorkflowDesigner WorkflowDesigner { get; private set; }
        public Workflow Workflow { get; private set; }
        public bool HasChanged { get; set; }
        public void forceHasChanged(bool value) { HasChanged = value; }
        public ModelItem SelectedActivity { get; private set; }
        private void OnCancel()
        {
            //GenericTools.RunUI(() =>
            //{
            //    if (tab == null) return;
            //    if (!tab.IsSelected) return;
            //    foreach (var i in WorkflowInstance.Instances)
            //    {
            //        if (i.WorkflowId == Workflow._id && !i.isCompleted)
            //        {
            //            i.Abort("User canceled workflow with cancel key");
            //        }
            //    }
            //    if (ResumeRuntimeFromHost != null) ResumeRuntimeFromHost.Set();

            //});
            foreach (var i in WorkflowInstance.Instances.ToList())
            {
                if (i.WorkflowId == Workflow._id && !i.isCompleted)
                {
                    i.Abort("User canceled workflow with cancel key");
                }
            }
        }
        private void OnKeyUp(Input.InputEventArgs e)
        {
            GenericTools.RunUI(() =>
            {
                if (tab == null) return;
                if (!tab.IsSelected) return;
                if (e.Key == Input.KeyboardKey.F10 || e.Key == Input.KeyboardKey.F11)
                {
                    if (currentprocessid == 0) currentprocessid = System.Diagnostics.Process.GetCurrentProcess().Id;
                    var element = AutomationHelper.GetFromFocusedElement();
                    if (element.ProcessId != currentprocessid) return;
                    //if (!IsRunnning)
                    //{
                    //}
                    if (e.AltKey || e.CtrlKey || e.ShiftKey || e.WinKey) return;
                    if (Workflow.Activity() == null) return;
                    Singlestep = true;
                    // if (e.Key == Input.KeyboardKey.F11) { StepInto = true; }
                    if (BreakPointhit)
                    {
                        if (ResumeRuntimeFromHost != null) ResumeRuntimeFromHost.Set();
                        return;
                    }
                    else
                    {
                        Run(VisualTracking, SlowMotion, null);
                    }
                }
            });
        }
        private int currentprocessid = 0;
        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (!IsRunnning)
            {
                if (currentprocessid == 0) currentprocessid = System.Diagnostics.Process.GetCurrentProcess().Id;
                var element = AutomationHelper.GetFromFocusedElement();
                if (element.ProcessId != currentprocessid) return;
            }
            if (e.Key == Key.F2)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (Config.local.minimize) GenericTools.Minimize();
                        System.Threading.Thread.Sleep(2000);
                        GenericTools.RunUI(() =>
                        {
                            try
                            {
                                MainWindow.instance.OnRecord(null);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
            if (e.Key == Key.F5)
            {
                if (BreakPointhit)
                {
                    SetDebugLocation(null);
                    Properties = WorkflowDesigner.PropertyInspectorView;
                    Singlestep = false;
                    BreakPointhit = false;
                    ResumeRuntimeFromHost.Set();
                    return;
                }
                try
                {
                    if (Workflow.Activity() == null) return;
                    Run(VisualTracking, SlowMotion, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("UserControl_KeyUp " + ex.Message);
                }
            }
            if (e.Key == Key.F9)
            {
                ToggleBreakpoint();
            }
        }
        public readonly Xceed.Wpf.AvalonDock.Layout.LayoutDocument tab;
        public bool ReadOnly
        {
            get
            {
                try
                {
                    return WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.ReadOnlyState>().IsReadOnly;
                }
                catch (Exception ex)
                {
                    Log.Error("WFDesigner:ReadOnly: " + ex.ToString());
                    return false;
                }
            }
            set
            {
                try
                {
                    WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.ReadOnlyState>().IsReadOnly = value;
                }
                catch (Exception ex)
                {
                    Log.Error("WFDesigner:Set.ReadOnly: " + ex.ToString());
                }
            }
        }
        private WFDesigner()
        {
            InitializeComponent();
        }
        private static readonly object _lock = new object();
        public void SetHasChanged()
        {
            if (HasChanged) return;
            HasChanged = true;
            OnChanged?.Invoke(this);
        }
        private readonly Type[] extratypes = null;
        public WFDesigner(Xceed.Wpf.AvalonDock.Layout.LayoutDocument tab, Workflow workflow, Type[] extratypes)
        {
            if(!string.IsNullOrEmpty(workflow.culture))
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(workflow.culture);
            }
            InitializeComponent();
            Log.Verbose("Open " + workflow.name + " version " + workflow._version);
            this.extratypes = extratypes;
            DataContext = this;
            this.tab = tab;
            //toolbox = InitializeActivitiesToolbox();
            //// WfToolboxBorder.Child = toolbox;
            Workflow = workflow;
            Input.InputDriver.Instance.onCancel += OnCancel;
            if (tab != null)
            {
                tab.Title = workflow.name;
            }

            comment = new MenuItem() { Header = OpenRPA.Resources.strings.designer_menu_commentout };
            uncomment = new MenuItem() { Header = OpenRPA.Resources.strings.designer_menu_uncomment };
            runthis = new MenuItem() { Header = OpenRPA.Resources.strings.designer_menu_runthis };
            runFromHere = new MenuItem() { Header = OpenRPA.Resources.strings.designer_menu_runfromhere };
            comment.Click += OnComment;
            uncomment.Click += OnUncomment;
            runthis.Click += OnRunthis;
            runFromHere.Click += OnRunFromHere;

            WorkflowDesigner = new WorkflowDesigner();
            DesignerConfigurationService configService = WorkflowDesigner.Context.Services.GetRequiredService<DesignerConfigurationService>();
            configService.TargetFrameworkName = new System.Runtime.Versioning.FrameworkName(".NETFramework", new Version(4, 5));
            configService.AnnotationEnabled = true;
            configService.AutoConnectEnabled = true;
            configService.AutoSplitEnabled = true;
            configService.AutoSurroundWithSequenceEnabled = true;
            configService.BackgroundValidationEnabled = true;
            configService.MultipleItemsContextMenuEnabled = true;
            configService.MultipleItemsDragDropEnabled = true;
            configService.NamespaceConversionEnabled = true;
            configService.PanModeEnabled = true;
            configService.RubberBandSelectionEnabled = true;
            configService.LoadingFromUntrustedSourceEnabled = false;

            if (IsRunnning)
            {
                ReadOnly = true;
            }
            WorkflowDesigner.Context.Services.Publish<IExpressionEditorService>(new CodeEditor.EditorService(this));
            if (!string.IsNullOrEmpty(Workflow.Xaml))
            {
                WorkflowDesigner.Text = Workflow.Xaml;
                WorkflowDesigner.Load();
            }
            else
            {
                Activity wf = new System.Activities.Statements.Sequence { };
                var ab = new ActivityBuilder
                {
                    Name = Workflow.name.Replace(" ", "_"),
                    Implementation = wf
                };
                WFHelper.AddVBNamespaceSettings(ab, new string[] { }, typeof(Action),
                    typeof(System.Xml.XmlNode),
                    typeof(OpenRPA.Workflow),
                    typeof(OpenRPA.UIElement),
                    typeof(System.Data.DataSet),
                    typeof(System.Linq.Enumerable),
                    typeof(Microsoft.VisualBasic.Collection)
                    );
                WFHelper.AddVBNamespaceSettings(ab, new string[] { }, extratypes);

                WorkflowDesigner.Load(ab);
            }
            if (global.isConnected)
            {
                ReadOnly = !Workflow.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.update);
            }
            HasChanged = false;
            WorkflowDesigner.ModelChanged += (sender, e) =>
            {
                SetHasChanged();
            };
            WorkflowDesigner.Context.Items.Subscribe(new SubscribeContextCallback<Selection>(SelectionChanged));
            WorkflowDesigner.View.Dispatcher.UnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(UnhandledException);
            Properties = WorkflowDesigner.PropertyInspectorView;
            ModelService modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
            if (modelService == null) return;
            modelService.ModelChanged -= new EventHandler<ModelChangedEventArgs>(ModelChanged);
            modelService.ModelChanged += new EventHandler<ModelChangedEventArgs>(ModelChanged);
#if DEBUG
            WorkflowDesigner.ContextMenu.Items.Add(runthis);
            WorkflowDesigner.ContextMenu.Items.Add(runFromHere);
#endif
            WorkflowDesigner.ContextMenu.Items.Add(comment);
            try
            {
                if (modelService != null)
                {
                    var modelItem = modelService.Root;
                    Workflow.name = modelItem.GetValue<string>("Name").Replace("_", " ");
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
            NotifyPropertyChanged("View");
        }
        private void UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception.ToString());
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        public async Task<bool> SaveAsync()
        {
            try
            {
                var imagepath = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "images");
                if (!System.IO.Directory.Exists(imagepath)) System.IO.Directory.CreateDirectory(imagepath);
                WorkflowDesigner.Flush();
                if (global.isConnected)
                {
                    if (!string.IsNullOrEmpty(Workflow._id))
                    {
                        var exists = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow', _id: '" + Workflow._id + "'}", top: 1);
                        if (exists.Length > 0 && Workflow.current_version != exists[0]._version)
                        {
                            var messageBoxResult = MessageBox.Show(Workflow.name + " has a newer version " + exists[0]._version + " , that has been updated by " + exists[0]._modifiedby +
                                ", do you still wish to overwrite the workflow with your version " + Workflow.current_version + "?", "Workflow has been updated by someone else", MessageBoxButton.YesNo);
                            if (messageBoxResult != MessageBoxResult.Yes)
                            {
                                Workflow.current_version = exists[0]._version;
                                return false;
                            }
                        }
                    }

                    var modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
                    var usedimages = new List<string>();
                    using (ModelEditingScope editingScope = modelService.Root.BeginEdit("Implementation"))
                    {
                        foreach (ModelItem item in GetWorkflowActivities())
                        {
                            ModelProperty property = item.Properties["Image"];
                            if ((property != null) && (property.Value != null) && !string.IsNullOrEmpty(Workflow._id))
                            {
                                string image = item.Properties["Image"].Value.ToString();
                                if (!System.Text.RegularExpressions.Regex.Match(image, "[a-f0-9]{24}").Success)
                                {
                                    try
                                    {
                                        var metadata = new OpenRPA.Interfaces.entity.metadata
                                        {
                                            // metadata.AddRight(global.webSocketClient.user, null);
                                            _acl = Workflow._acl,
                                            workflow = Workflow._id
                                        };
                                        var imageid = GenericTools.YoutubeLikeId();
                                        var tempfilename = System.IO.Path.Combine(System.IO.Path.GetTempPath(), imageid + ".png");
                                        using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(image)))
                                        {
                                            using (var b = new System.Drawing.Bitmap(ms))
                                            {
                                                try
                                                {
                                                    b.Save(tempfilename, System.Drawing.Imaging.ImageFormat.Png);
                                                }
                                                catch (Exception)
                                                {
                                                    throw;
                                                }
                                            }
                                        }
                                        string id = await global.webSocketClient.UploadFile(tempfilename, "", metadata);
                                        var filename = System.IO.Path.Combine(imagepath, id + ".png");
                                        System.IO.File.Copy(tempfilename, filename, true);
                                        System.IO.File.Delete(tempfilename);
                                        item.Properties["Image"].SetValue(id);
                                        usedimages.Add(id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error("WFDesigner.SaveAsync: " + ex.Message);
                                    }
                                }
                                else
                                {
                                    usedimages.Add(image);
                                }
                            }
                        }
                        editingScope.Complete();
                    }
                    WorkflowDesigner.Flush();
                    if (!string.IsNullOrEmpty(Workflow._id))
                    {
                        var files = await global.webSocketClient.Query<Interfaces.entity.metadata>("files", "{\"metadata.workflow\": \"" + Workflow._id + "\"}");
                        var unusedfiles = files.Where(x => !usedimages.Contains(x._id)).ToList();
                        foreach (var f in unusedfiles)
                        {
                            await global.webSocketClient.DeleteOne("files", f._id);
                            var imagefilepath = System.IO.Path.Combine(imagepath, f._id + ".png");
                            // if (System.IO.File.Exists(imagefilepath)) System.IO.File.Delete(imagefilepath);
                            if (System.IO.File.Exists(imagefilepath))
                            {
                                System.IO.File.Delete(imagefilepath);
                            }
                            else
                            {
                                Log.Error("Failed locating " + f._id + ".png");
                            }
                        }
                    }
                }

                try
                {
                    Workflow.Xaml = WorkflowDesigner.Text;
                    Parseparameters(Workflow);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return false;
                }
                WorkflowDesigner.Flush();
                var modelItem = WorkflowDesigner.Context.Services.GetService<ModelService>().Root;
                Workflow.name = modelItem.GetValue<string>("Name").Replace("_", " ");

                Workflow.Xaml = WorkflowDesigner.Text;
                var _hasChanged = HasChanged;
                HasChanged = false;
                RobotInstance.instance.dbWorkflows.Update(Workflow);
                await Workflow.Save();
                if (_hasChanged)
                {
                    HasChanged = false;
                    OnChanged?.Invoke(this);
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }
        public void RenameWorkflow(string name)
        {
            var modelItem = WorkflowDesigner.Context.Services.GetService<ModelService>().Root;
            ModelProperty property = modelItem.Properties["Name"];
            property.SetValue(name.Replace(" ", "_"));
            tab.IsSelected = true;
        }
        public bool Save()
        {
            bool result = GenericTools.RunUIAsync(SaveAsync).Result;
            return result;
        }
        public KeyedCollection<string, System.Activities.DynamicActivityProperty> GetParameters()
        {
            return GetParameters(Workflow.culture, Workflow.Xaml);
        }
        public static KeyedCollection<string, System.Activities.DynamicActivityProperty> GetParameters(string culture, string Xaml)
        {
            System.Activities.ActivityBuilder ab2;
            if (!string.IsNullOrEmpty(culture))
            {
                try
                {
                    ab2 = Task.Run(() =>
                    {
                        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);
                        using (var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(Xaml)))
                        {
                            return System.Xaml.XamlServices.Load(
                                System.Activities.XamlIntegration.ActivityXamlServices.CreateBuilderReader(
                                new System.Xaml.XamlXmlReader(stream))) as System.Activities.ActivityBuilder;
                        }
                    }).Result;
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null) ex = ex.InnerException;
                    throw ex;
                }
            }
            else
            {
                using (var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(Xaml)))
                {
                    ab2 = System.Xaml.XamlServices.Load(
                        System.Activities.XamlIntegration.ActivityXamlServices.CreateBuilderReader(
                        new System.Xaml.XamlXmlReader(stream))) as System.Activities.ActivityBuilder;
                }
            }
            return ab2.Properties;
        }
        public void Parseparameters(Workflow Workflow)
        {
            // Workflow.Serializable = true;
            Workflow.Parameters.Clear();
            if (!string.IsNullOrEmpty(Workflow.Xaml))
            {
                var parameters = GetParameters(Workflow.culture, Workflow.Xaml);
                foreach (var prop in parameters)
                {
                    var par = new workflowparameter() { name = prop.Name };
                    par.type = prop.Type.GenericTypeArguments[0].FullName;
                    string baseTypeName = prop.Type.BaseType.FullName;
                    if (baseTypeName == "System.Activities.InArgument")
                    {
                        par.direction = workflowparameterdirection.@in;
                    }
                    if (baseTypeName == "System.Activities.InOutArgument")
                    {
                        par.direction = workflowparameterdirection.inout;
                    }
                    if (baseTypeName == "System.Activities.OutArgument")
                    {
                        par.direction = workflowparameterdirection.@out;
                    }
                    if (!prop.Type.GenericTypeArguments[0].IsSerializable2())
                    {
                        Log.Activity(string.Format("Name: {0}, Type: {1} is not serializable, therefor saving state will not be supported", prop.Name, prop.Type));
                        Workflow.Serializable = false;
                    }
                    Log.Activity(string.Format("Name: '{0}', Type: {1}", prop.Name, prop.Type));
                    Workflow.Parameters.Add(par);
                }
            }
            if (Workflow.Serializable == true)
            {
                bool canIdle = false;
                foreach (ModelItem item in GetWorkflowActivities())
                {
                    try
                    {
                        var a = item.GetCurrentValue();
                        var prop = a.GetType().GetProperty("CanInduceIdle", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (prop != null)
                        {
                            var CanInduceIdle = (bool)prop.GetValue(a);
                            if (CanInduceIdle == true)
                            {
                                Log.Activity(string.Format("Activity: '{0}' Can induce idle, need to check if workflow is serializable", Workflow.ProjectAndName));
                                canIdle = true;
                            }
                        }

                        //var i = item.GetCurrentValue() as Activity;
                        //var i2 = i;
                        //ModelItemImpl i = item.instan as System.Activities.Presentation.Model.ModelItemImpl;
                        //item.can

                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
                if (canIdle == true)
                {
                    foreach (ModelItem item in this.GetWorkflowActivities())
                    {
                        var vars = item.Properties["Variables"];
                        if (vars != null && vars.Collection != null)
                        {
                            foreach (var v in vars.Collection)
                            {
                                try
                                {
                                    //string baseTypeName = v.ItemType.GenericTypeArguments[0].BaseType.FullName;
                                    string baseTypeName = v.ItemType.GenericTypeArguments[0].FullName;
                                    if (!v.ItemType.GenericTypeArguments[0].IsSerializable2())
                                    {
                                        var _v = v.GetCurrentValue();
                                        var prop = _v.GetType().GetProperty("Name");
                                        if (prop != null)
                                        {
                                            Log.Activity(string.Format("Variable name: '{0}', Type: {1} is not serializable", (string)prop.GetValue(_v), baseTypeName));
                                        }
                                        else
                                        {
                                            Log.Activity(string.Format("TypeName: '{0}', Type: {1} is not serializable", v.ItemType.GenericTypeArguments[0].Name, baseTypeName));
                                        }
                                        Workflow.Serializable = false;
                                        //throw new NotSerializable("All properties on a workflow needs to be serializable '" + prop.Name + "'");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Debug(ex.ToString());
                                }
                            }
                        }

                    }
                }
            }
        }
        public List<ModelItem> GetWorkflowActivities()
        {
            List<ModelItem> list = new List<ModelItem>();

            ModelService modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
            list = modelService.Find(modelService.Root, typeof(Activity)).ToList<ModelItem>();

            list.AddRange(modelService.Find(modelService.Root, (Predicate<Type>)(type => (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(FlowSwitch<>))))));
            list.AddRange(modelService.Find(modelService.Root, typeof(FlowDecision)));
            return list;
        }
        private static List<ModelItem> GetWorkflowActivities(WorkflowDesigner wfDesigner)
        {
            List<ModelItem> list = new List<ModelItem>();

            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
            list = modelService.Find(modelService.Root, typeof(Activity)).ToList<ModelItem>();

            list.AddRange(modelService.Find(modelService.Root, (Predicate<Type>)(type => (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(FlowSwitch<>))))));
            list.AddRange(modelService.Find(modelService.Root, typeof(FlowDecision)));
            return list;
        }
        private void SelectionChanged(Selection item)
        {
            selection = item;
            SelectedActivity = selection.PrimarySelection;
            if (SelectedActivity == null) return;
            SelectedVariableName = SelectedActivity.GetCurrentValue().ToString();

            try
            {
                if (WorkflowDesigner.ContextMenu.Items.Contains(comment)) WorkflowDesigner.ContextMenu.Items.Remove(comment);
                if (WorkflowDesigner.ContextMenu.Items.Contains(uncomment)) WorkflowDesigner.ContextMenu.Items.Remove(uncomment);
                var lastSequence = GetActivitiesScope(SelectedActivity.Parent);
                if (lastSequence == null) lastSequence = GetActivitiesScope(SelectedActivity);
                if (lastSequence == null) return;
                if (SelectedActivity.ItemType == typeof(Activities.CommentOut))
                {
                    WorkflowDesigner.ContextMenu.Items.Add(uncomment);
                }
                else if (lastSequence.ItemType != typeof(Flowchart))
                {
                    if (selection.SelectionCount > 1)
                    {
                        if (lastSequence.Properties["Nodes"] == null)
                        {
                            WorkflowDesigner.ContextMenu.Items.Add(comment);
                        }
                    }
                    else
                    {
                        WorkflowDesigner.ContextMenu.Items.Add(comment);
                    }
                }
                else if (lastSequence.ItemType != typeof(Flowchart))
                {
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public bool IsSequenceSelected
        {
            get
            {
                var lastSequence = GetSequence(SelectedActivity);
                if (lastSequence != null) return true;
                return false;
            }
        }
        IWorkflow IDesigner.Workflow { get => Workflow; set { } }

        List<Activity> recording = null;
        List<IPlugin> recordingplugins = null;
        public void BeginRecording()
        {
            recording = new List<Activity>();
            recordingplugins = new List<IPlugin>();
        }
        public ModelItem AddRecordingActivity(Activity a, IPlugin plugin)
        {
            if (plugin != null)
            {
                WFHelper.AddVBNamespaceSettings(WorkflowDesigner, new Type[] { plugin.GetType() });
                Type t = plugin.GetType();
                WFHelper.DynamicAssemblyMonitor(WorkflowDesigner, t.Assembly.GetName().Name, t.Assembly, true);
            }
            //DynamicAssemblyMonitor(t.Assembly.GetName().Name, t.Assembly, true);
            if (Config.local.recording_add_to_designer)
            {
                return AddActivity(a);
            }
            if (recording == null) BeginRecording();
            recording.Add(a);
            if (!recordingplugins.Contains(plugin)) recordingplugins.Add(plugin);
            return null;
        }
        public void EndRecording()
        {
            if (recording == null) return;
            ModelService modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
            foreach (var plugin in recordingplugins)
            {
                WFHelper.AddVBNamespaceSettings(WorkflowDesigner, new Type[] { plugin.GetType() });
                Type t = plugin.GetType();
                WFHelper.DynamicAssemblyMonitor(WorkflowDesigner, t.Assembly.GetName().Name, t.Assembly, true);
            }
            foreach (var a in recording)
            {
                var model = AddActivity(a);
                if (a is Activities.TypeText)
                {
                    ((Activities.TypeText)a).UpdateModel(model);
                }
            }
            recording = null;
        }
        public ModelItem AddActivity(Activity a)
        {
            ModelItem newItem = null;
            ModelService modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
            using (ModelEditingScope editingScope = modelService.Root.BeginEdit("Implementation"))
            {
                var lastSequence = GetSequence(SelectedActivity);
                if (lastSequence == null && SelectedActivity != null) lastSequence = GetActivitiesScope(SelectedActivity.Parent);
                if (lastSequence != null)
                {
                    ModelItemCollection Activities = null;
                    if (lastSequence.Properties["Activities"] != null)
                    {
                        Activities = lastSequence.Properties["Activities"].Collection;
                    }
                    else
                    {
                        Activities = lastSequence.Properties["Nodes"].Collection;
                    }

                    var insertAt = Activities.Count;
                    for (var i = 0; i < Activities.Count; i++)
                    {
                        if (Activities[i].Equals(SelectedActivity))
                        {
                            insertAt = (i + 1);
                        }
                    }
                    if (lastSequence.Properties["Activities"] != null)
                    {
                        if (string.IsNullOrEmpty(a.DisplayName)) a.DisplayName = "Activity";
                        newItem = Activities.Insert(insertAt, a);
                    }
                    else
                    {
                        FlowStep step = new FlowStep
                        {
                            Action = a
                        };
                        newItem = Activities.Insert(insertAt, step);
                    }
                    //Selection.Select(wfDesigner.Context, selectedActivity);
                    //ModelItemExtensions.Focus(selectedActivity);
                }
                editingScope.Complete();
                //WorkflowInspectionServices.CacheMetadata(a);
            }
            if (newItem != null)
            {
                SelectedActivity = newItem;
                newItem.Focus(20);
                Selection.SelectOnly(WorkflowDesigner.Context, newItem);
            }
            return newItem;
        }
        private ModelItem GetSequence(ModelItem from)
        {
            ModelItem parent = from;
            while (parent != null && !parent.ItemType.Equals(typeof(Sequence)))
            {
                parent = parent.Parent;
            }
            return parent;
        }
        private ModelItem GetVariableScope(ModelItem from)
        {
            ModelItem parent = from;

            while (parent != null && (parent.Properties["Variables"] == null || parent.ItemType.IsSubclassOf(typeof(BreakableLoop))))
            {
                parent = parent.Parent;
            }
            return parent;
        }
        private ModelItem GetActivitiesScope(ModelItem from)
        {
            ModelItem parent = from;

            while (parent != null && parent.Properties["Activities"] == null && parent.Properties["Handler"] == null && parent.Properties["Nodes"] == null)
            {
                parent = parent.Parent;
            }
            return parent;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            Input.InputDriver.Instance.OnKeyUp += OnKeyUp;
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Input.InputDriver.Instance.OnKeyUp -= OnKeyUp;
        }
        public Argument GetArgument(string Name, bool add, Type type)
        {
            ModelService modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
            ModelItemCollection args = modelService.Root.Properties["Properties"].Collection;

            foreach (var _v in args)
            {
                var nameprop = (string)_v.Properties["Name"].ComputedValue;
                if (Name == nameprop) return _v.GetCurrentValue() as Argument;
            }
            if (add)
            {
                Argument myArg = Argument.Create(type, ArgumentDirection.InOut);
                args.Add(myArg);
                return myArg;
            }
            return null;
        }
        public DynamicActivityProperty GetArgumentOf<T>(string Name, bool add)
        {
            ModelService modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
            ModelItemCollection args = modelService.Root.Properties["Properties"].Collection;

            foreach (var _v in args)
            {
                var nameprop = (string)_v.Properties["Name"].ComputedValue;
                if (Name == nameprop) return _v.GetCurrentValue() as DynamicActivityProperty;
            }
            if (add)
            {
                args.Add(new DynamicActivityProperty
                {
                    Name = Name,
                    Type = typeof(OutArgument<T>),
                    Value = new OutArgument<T>() // new OutArgument<T>(myPara) // uses myPara.ToString() for default expression
                });
            }
            return null;
        }
        public Variable GetVariable(string Name, Type type)
        {
            try
            {
                MethodInfo method = typeof(WFDesigner).GetMethod("GetVariableOf");
                MethodInfo generic = method.MakeGenericMethod(type);
                var res = generic.Invoke(this, new object[] { Name });
                return (Variable)res;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        public Variable<T> GetVariableOf<T>(string Name)
        {
            if (SelectedActivity == null) throw new Exception("Cannot get variable when no activity has been selected");
            var seq = GetVariableScope(SelectedActivity);
            if (seq == null) throw new Exception("Cannot add variables to root activity!");
            Variable<T> result = GetVariableModel<T>(Name, SelectedActivity);
            if (result == null)
            {
                ModelService modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
                using (ModelEditingScope editingScope = modelService.Root.BeginEdit("Implementation"))
                {
                    var Variables = seq.Properties["Variables"].Collection;
                    result = new Variable<T>() { Name = Name };
                    Variables.Add(result);
                    editingScope.Complete();
                }
            }
            return result;
        }
        public Variable<T> GetVariableModel<T>(string Name, ModelItem model)
        {
            Variable<T> result = null;

            if (model.Properties["Variables"] != null)
            {
                var Variables = model.Properties["Variables"].Collection;
                foreach (var _v in Variables)
                {
                    var nameprop = (string)_v.Properties["Name"].ComputedValue;
                    if (Name == nameprop) return _v.GetCurrentValue() as Variable<T>;
                }
            }
            if (model.Parent != null)
            {
                result = GetVariableModel<T>(Name, model.Parent);
            }
            return result;
        }
        private void EnsureSourceLocationUpdated()
        {
            var debugView = WorkflowDesigner.DebugManagerView;

            var nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            var debuggerServiceType = typeof(System.Activities.Presentation.Debug.DebuggerService);
            var ensureMappingMethodName = "EnsureSourceLocationUpdated";
            var ensureMappingMethod = debuggerServiceType.GetMethod(ensureMappingMethodName, nonPublicInstance);
            _ = ensureMappingMethod.Invoke(debugView, new object[0]);
        }
        private Dictionary<Activity, SourceLocation> CreateSourceLocationMapping(ModelService modelService)
        {
            var debugView = WorkflowDesigner.DebugManagerView;

            var nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            var debuggerServiceType = typeof(System.Activities.Presentation.Debug.DebuggerService);
            var mappingFieldName = "instanceToSourceLocationMapping";
            var mappingField = debuggerServiceType.GetField(mappingFieldName, nonPublicInstance);
            if (mappingField == null)
                throw new MissingFieldException(debuggerServiceType.FullName, mappingFieldName);

            if (!(modelService.Root.GetCurrentValue() is Activity rootActivity))
            {
                WorkflowDesigner.Flush();
                System.Activities.XamlIntegration.ActivityXamlServicesSettings activitySettings = new System.Activities.XamlIntegration.ActivityXamlServicesSettings
                {
                    CompileExpressions = true
                };
                if (!string.IsNullOrEmpty(Workflow.culture))
                {
                    try
                    {
                        rootActivity = Task.Run(() =>
                        {
                            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(Workflow.culture);
                            var xamlReaderSettings = new System.Xaml.XamlXmlReaderSettings { LocalAssembly = typeof(WFDesigner).Assembly };
                            var xamlReader = new System.Xaml.XamlXmlReader(new System.IO.StringReader(WorkflowDesigner.Text), xamlReaderSettings);
                            return System.Activities.XamlIntegration.ActivityXamlServices.Load(xamlReader, activitySettings);

                        }).Result;
                    }
                    catch (Exception ex)
                    {
                        while (ex.InnerException != null) ex = ex.InnerException;
                        throw ex;
                    }
                }
                else
                {
                    var xamlReaderSettings = new System.Xaml.XamlXmlReaderSettings { LocalAssembly = typeof(WFDesigner).Assembly };
                    var xamlReader = new System.Xaml.XamlXmlReader(new System.IO.StringReader(WorkflowDesigner.Text), xamlReaderSettings);
                    rootActivity = System.Activities.XamlIntegration.ActivityXamlServices.Load(xamlReader, activitySettings);
                }

            }
            WorkflowInspectionServices.CacheMetadata(rootActivity);

            EnsureSourceLocationUpdated();
            var mapping = (Dictionary<object, System.Activities.Debugger.SourceLocation>)mappingField.GetValue(debugView);
            var result = new Dictionary<Activity, System.Activities.Debugger.SourceLocation>();
            foreach (var m in mapping)
            {
                try
                {
                    if (m.Key is Activity a) result.Add(a, m.Value);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
            }
            return result;
        }
        private SourceLocation GetSourceLocationFromModelItem(ModelItem modelItem)
        {
            var debugView = WorkflowDesigner.DebugManagerView;
            var nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            var debuggerServiceType = typeof(System.Activities.Presentation.Debug.DebuggerService);
            var ensureMappingMethodName = "GetSourceLocationFromModelItem";
            var ensureMappingMethod = debuggerServiceType.GetMethod(ensureMappingMethodName, nonPublicInstance);
            var res = ensureMappingMethod.Invoke(debugView, new object[] { modelItem });
            return res as System.Activities.Debugger.SourceLocation;
        }
        public void SetDebugLocation(SourceLocation location)
        {
            WorkflowDesigner.DebugManagerView.CurrentLocation = location;
        }
        public void NavigateTo(ModelItem item)
        {
            var validation = WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Validation.ValidationService>();
            //private ModelSearchServiceImpl modelSearchService;

            var modelSearchService = typeof(System.Activities.Presentation.Validation.ValidationService).GetField("modelSearchService", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(validation);
            //this.modelSearchService.NavigateTo(itemToFocus);
            var methods = modelSearchService.GetType().GetMethods().Where(x => x.Name == "NavigateTo");
            foreach (var methodInfo in methods)
            {
                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length == 1)
                {
                    if (parameters[0].Name == "itemToFocus")
                    {
                        methodInfo.Invoke(modelSearchService, new Object[] { item });
                    }
                }
            }

        }
        public void InitializeStateEnvironment()
        {
            Log.Debug("InitializeStateEnvironment");
            GenericTools.RunUI(() =>
            {
                try
                {
                    var modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
                    //IEnumerable<ModelItem> wfElements = modelService.Find(modelService.Root, typeof(Activity)).
                    //Union(modelService.Find(modelService.Root, typeof(System.Activities.Debugger.State)));
                    var wfElements = modelService.Find(modelService.Root, typeof(Activity)).
Union(modelService.Find(modelService.Root, typeof(System.Activities.Debugger.State))).ToList();
                    wfElements.Add(modelService.Root);


                    var map = CreateSourceLocationMapping(modelService);
                    _sourceLocationMapping.Clear();
                    _activityIdMapping.Clear();
                    _activitysourceLocationMapping.Clear();
                    _activityIdModelItemMapping.Clear();
                    _modelLocationMapping.Clear();

                    foreach (var modelItem in wfElements)
                    {
                        var loc = GetSourceLocationFromModelItem(modelItem);
                        var activity = modelItem.GetCurrentValue() as Activity;
                        if (activity == null)
                        {
                            var builder = modelItem.GetCurrentValue() as ActivityBuilder;
                            continue;
                        }
                        var id = activity.Id;
                        if (string.IsNullOrEmpty(id)) continue;
                        if (_sourceLocationMapping.ContainsKey(id)) continue;
                        _activitysourceLocationMapping.Add(activity, loc);
                        _sourceLocationMapping.Add(id, loc);
                        _activityIdMapping.Add(id, activity);
                        _activityIdModelItemMapping.Add(id, modelItem);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }
        public void ToggleBreakpoint()
        {
            var debugManagerView = WorkflowDesigner.DebugManagerView;
            var selectedLocation = debugManagerView.SelectedLocation;
            try
            {
                if (selectedLocation != null)
                {
                    if (debugManagerView.GetBreakpointLocations().ContainsKey(selectedLocation))
                    {
                        debugManagerView.DeleteBreakpoint(selectedLocation);
                    }
                    else
                    {
                        debugManagerView.InsertBreakpoint(selectedLocation, System.Activities.Presentation.Debug.BreakpointTypes.Bounded | System.Activities.Presentation.Debug.BreakpointTypes.Enabled);
                    }
                }
                BreakpointLocations = WorkflowDesigner.DebugManagerView.GetBreakpointLocations();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public IDictionary<SourceLocation, System.Activities.Presentation.Debug.BreakpointTypes> BreakpointLocations { get; set; }
        public void OnVisualTracking(IWorkflowInstance Instance, string ActivityId, string ChildActivityId, string State)
        {
            try
            {
                if (SlowMotion) System.Threading.Thread.Sleep(500);

                //if (_activityIdMapping == null || ActivityId == "1") return;
                //if (!_activityIdMapping.ContainsKey(ActivityId))
                //{
                //    // Log.Debug("Failed locating ActivityId : " + ActivityId);
                //    return;
                //}
                //if (!_sourceLocationMapping.ContainsKey(ActivityId)) return;
                //if (!_sourceLocationMapping.ContainsKey(ChildActivityId)) return;


                System.Activities.Debugger.SourceLocation location;

                //location = _sourceLocationMapping[ActivityId];
                //BreakPointhit = wfDesigner.DebugManagerView.GetBreakpointLocations().ContainsKey(location);

                if (!_sourceLocationMapping.ContainsKey(ChildActivityId) || !_sourceLocationMapping.ContainsKey(ActivityId))
                {
                    InitializeStateEnvironment();
                }

                // InitializeStateEnvironment();
                if (!_sourceLocationMapping.ContainsKey(ChildActivityId)) return;
                location = _sourceLocationMapping[ChildActivityId];
                if (location == null) return;
                if (!BreakPointhit)
                {
                    if (BreakpointLocations == null) BreakpointLocations = WorkflowDesigner.DebugManagerView.GetBreakpointLocations();
                    BreakPointhit = BreakpointLocations.ContainsKey(location);
                }
                ModelItem model = _activityIdModelItemMapping[ChildActivityId];
                if (VisualTracking || BreakPointhit || Singlestep)
                {
                    GenericTools.RunUI(() =>
                    {
                        GenericTools.Restore();
                        NavigateTo(model);
                        SetDebugLocation(location);

                    });
                }
                if (BreakPointhit || Singlestep)
                {
                    using (ResumeRuntimeFromHost = new System.Threading.AutoResetEvent(false))
                    {
                        BreakPointhit = true;
                        ShowVariables(Instance.Variables);
                        GenericTools.Restore();
                        ResumeRuntimeFromHost.WaitOne();
                    }
                    ResumeRuntimeFromHost = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void IdleOrComplete(IWorkflowInstance instance, EventArgs e)
        {
            if (instance == null) return;
            try
            {
                if (!string.IsNullOrEmpty(instance.queuename) && !string.IsNullOrEmpty(instance.correlationId))
                {
                    Interfaces.mq.RobotCommand command = new Interfaces.mq.RobotCommand();
                    JObject data = null;
                    if (instance.Parameters != null) data = JObject.FromObject(instance.Parameters);
                    command.command = "invoke" + instance.state;
                    command.workflowid = instance.WorkflowId;
                    command.data = data;
                    if ((instance.state == "failed" || instance.state == "aborted") && instance.Exception != null)
                    {
                        command.data = JObject.FromObject(instance.Exception);
                    }
                    Task.Run(async () =>
                    {
                        try
                        {
                            await global.webSocketClient.QueueMessage(instance.queuename, command, null, instance.correlationId, 0);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex.Message);
                        }
                    });
                    OnChanged?.Invoke(this);
                }
                if (instance.state == "idle" && Singlestep == true)
                {
                    GenericTools.Minimize();
                    //GenericTools.RunUI(() =>
                    //{
                    //    SetDebugLocation(null);
                    //    Properties = wfDesigner.PropertyInspectorView;
                    //});
                }
                if (instance.state == "completed")
                {
                    GenericTools.RunUI(() =>
                    {
                        SetDebugLocation(null);
                        Properties = WorkflowDesigner.PropertyInspectorView;
                    });
                }
                if ((string.IsNullOrEmpty(instance.queuename) && string.IsNullOrEmpty(instance.correlationId)) && string.IsNullOrEmpty(instance.caller) && instance.isCompleted && Config.local.minimize)
                {
                    GenericTools.RunUI(() =>
                    {
                        GenericTools.Restore();
                    });
                }

                if (instance.state != "idle")
                {
                    GenericTools.RunUI(() =>
                    {
                        Properties = WorkflowDesigner.PropertyInspectorView;
                        if (global.isConnected)
                        {
                            ReadOnly = !Workflow.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.update);
                        }
                        else
                        {
                            ReadOnly = false;
                        }
                        CommandManager.InvalidateRequerySuggested();
                    });

                    BreakPointhit = false; Singlestep = false;
                    bool isRemote = true;
                    if (string.IsNullOrEmpty(instance.queuename) && string.IsNullOrEmpty(instance.correlationId))
                    {
                        isRemote = false;
                        if (instance.state != "completed")
                        {
                            System.Activities.Debugger.SourceLocation location;
                            if (instance.errorsource != null && !_sourceLocationMapping.ContainsKey(instance.errorsource))
                            {
                                InitializeStateEnvironment();
                            }

                            if (instance.errorsource != null && _sourceLocationMapping.ContainsKey(instance.errorsource))
                            {
                                GenericTools.RunUI(() =>
                                {
                                    location = _sourceLocationMapping[instance.errorsource];
                                    SetDebugLocation(location);
                                    if (_activityIdModelItemMapping.ContainsKey(instance.errorsource))
                                    {
                                        ModelItem model = _activityIdModelItemMapping[instance.errorsource];
                                        NavigateTo(model);
                                    }
                                });
                            }
                            else
                            {
                                GenericTools.RunUI(() =>
                                {
                                    SetDebugLocation(null);
                                });

                            }
                        }
                    }
                    string message = ("MISSING WORKFLOW!!!! " + instance.state);
                    if(instance.Workflow != null ) message = (instance.Workflow.name + " " + instance.state);
                    if (!string.IsNullOrEmpty(instance.errorsource))
                    {
                        message += " at " + instance.errorsource;
                    }
                    if (instance.runWatch != null)
                    {
                        message += (" in " + string.Format("{0:mm\\:ss\\.fff}", instance.runWatch.Elapsed));
                    }
                    if (!string.IsNullOrEmpty(instance.errormessage)) message += (Environment.NewLine + "# " + instance.errormessage);
                    Log.Output(message);
                    if (instance.hasError || instance.isCompleted)
                    {
                        if ((Config.local.notify_on_workflow_end && !isRemote) || (Config.local.notify_on_workflow_remote_end && isRemote))
                        {
                            if (instance.state == "completed")
                            {
                                // App.notifyIcon.ShowBalloonTip(1000, instance.Workflow.name + " " + instance.state, message, System.Windows.Forms.ToolTipIcon.Info);
                                App.notifyIcon.ShowBalloonTip(1000, "", message, System.Windows.Forms.ToolTipIcon.Info);
                            }
                            else
                            {
                                // App.notifyIcon.ShowBalloonTip(1000, instance.Workflow.name + " " + instance.state, message, System.Windows.Forms.ToolTipIcon.Error);
                                App.notifyIcon.ShowBalloonTip(1000, "", message, System.Windows.Forms.ToolTipIcon.Error);
                            }
                        }
                    }
                    OnChanged?.Invoke(this);
                }
                if (instance.hasError || instance.isCompleted)
                {
                    _ = Task.Run(() =>
                    {
                        var sw = new System.Diagnostics.Stopwatch(); sw.Start();
                        while (sw.Elapsed < TimeSpan.FromSeconds(1))
                        {
                            if (System.Threading.Monitor.TryEnter(WorkflowInstance.Instances, 1000))
                            {
                                try
                                {
                                    foreach (var wi in WorkflowInstance.Instances.ToList())
                                    {
                                        if (wi.isCompleted) continue;
                                        if (wi.Bookmarks == null) continue;
                                        foreach (var b in wi.Bookmarks)
                                        {
                                            if (b.Key == instance._id)
                                            {
                                                wi.ResumeBookmark(b.Key, instance, true);
                                                return;
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    System.Threading.Monitor.Exit(WorkflowInstance.Instances);
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
            }
        }
        public void Run(bool VisualTracking, bool SlowMotion, IWorkflowInstance instance)
        {
            GenericTools.RunUI(() =>
           {
               this.VisualTracking = VisualTracking; this.SlowMotion = SlowMotion;
               if (BreakPointhit)
               {
                   SetDebugLocation(null);
                   Properties = WorkflowDesigner.PropertyInspectorView;
                   Singlestep = false;
                   BreakPointhit = false;
                   if (!VisualTracking && Config.local.minimize) GenericTools.Minimize();
                   if (ResumeRuntimeFromHost != null) ResumeRuntimeFromHost.Set();
                   return;
               }
               WorkflowDesigner.Flush();
               // InitializeStateEnvironment();
               if (global.isConnected)
               {
                   if (!Workflow.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.invoke))
                   {
                       Log.Error("Access denied, " + global.webSocketClient.user.username + " does not have invoke permission");
                       return;
                   }
               }
               //GenericTools.RunUI(() =>
               //{
               //    if (_activityIdMapping.Count == 0)
               //    {
               //        int failCounter = 0;
               //        while (_activityIdMapping.Count == 0 && failCounter < 3)
               //        {
               //            System.Windows.Forms.Application.DoEvents();
               //            InitializeStateEnvironment(true);
               //            System.Threading.Thread.Sleep(500);
               //            failCounter++;
               //        }
               //    }
               //    if (_activityIdMapping.Count == 0)
               //    {
               //        _ = Save();
               //        // ReloadDesigner();
               //    }
               //    if (_activityIdMapping.Count == 0)
               //    {
               //        int failCounter = 0;
               //        while (_activityIdMapping.Count == 0 && failCounter < 3)
               //        {
               //            System.Windows.Forms.Application.DoEvents();
               //            InitializeStateEnvironment(true);
               //            System.Threading.Thread.Sleep(500);
               //            failCounter++;
               //        }
               //    }

               //});
               //if (_activityIdMapping.Count == 0)
               //{
               //    Log.Error("Failed mapping activites!!!!!");
               //    throw new Exception("Failed mapping activites!!!!!");
               //}
               if (instance == null)
               {
                   var param = new Dictionary<string, object>();
                   BreakpointLocations = WorkflowDesigner.DebugManagerView.GetBreakpointLocations();
                   if (SlowMotion || VisualTracking || BreakpointLocations.Count > 0 || Singlestep == true)
                   {
                       instance = Workflow.CreateInstance(param, null, null, IdleOrComplete, OnVisualTracking);
                   }
                   else
                   {
                       instance = Workflow.CreateInstance(param, null, null, IdleOrComplete, null);
                   }
               }
               ReadOnly = true;
               if (!VisualTracking && Config.local.minimize) GenericTools.Minimize();
           });
            if (instance != null) instance.Run();
        }
        private void ShowVariables(IDictionary<string, WorkflowInstanceValueType> Variables)
        {
            GenericTools.RunUI(() =>
            {
                var form = new showVariables
                {
                    variables = new System.Collections.ObjectModel.ObservableCollection<variable>()
                };
                Variables?.ForEach(x =>
                {
                    form.addVariable(x.Key, x.Value.value, x.Value.type);
                });
                Properties = form;
            });
        }
        public override string ToString()
        {
            return Workflow.name;
        }
        private void ModelChanged(object sender, ModelChangedEventArgs e)
        {
            if (e.ModelChangeInfo != null)
            {
                var model = e.ModelChangeInfo.Subject;
                if (model != null)
                {
                    if (model.ItemType.BaseType == typeof(Variable))
                    {
                        if (e.ModelChangeInfo.PropertyName != "Name") return;
#pragma warning disable 0618
                        ModelProperty property = e.PropertiesChanged.ElementAt<ModelProperty>(0);
#pragma warning restore 0618
                        string variableName = property.ComputedValue.ToString();
                        RenameVariable(SelectedVariableName, variableName);
                        //DesignerView.ToggleVariableDesignerCommand.Execute(null);
                        //} else if (model.ItemType.BaseType == typeof(KeyedCollection<string, DynamicActivityProperty>))
                    }
                    else if (model.ItemType == typeof(System.Activities.ActivityBuilder))
                    {
                        var name = model.GetValue<string>("Name");
                        if (name != null && name.Contains(" "))
                        {
                            name = name.Replace(" ", "_");
                            model.Properties["Name"].SetValue(name);
                        }
                    }
                    else if (model.ItemType == typeof(DynamicActivityProperty))
                    {
                        if (e.ModelChangeInfo.PropertyName != "Name") return;
#pragma warning disable 0618
                        ModelProperty property = e.PropertiesChanged.ElementAt<ModelProperty>(0);
#pragma warning restore 0618
                        string variableName = property.ComputedValue.ToString();
                        RenameVariable(SelectedVariableName, variableName);
                    }
                    //else if (e.ModelChangeInfo.ModelChangeType == ModelChangeType.CollectionItemAdded)
                    //{
                    //    var a = model.GetCurrentValue() as Activity;
                    //    var map = CreateSourceLocationMapping(modelService);
                    //    if(a != null && map.ContainsKey(a))
                    //    {
                    //        ShowDebug(map[a]);
                    //    }
                    //    else
                    //    {
                    //        ShowDebug(map.Last().Value);
                    //    }

                    //    Selection.SelectOnly(wfDesigner.Context, model);
                    //    ModelItemExtensions.Focus(model);
                    //}
                }
            }
        }
        void RenameVariable(string variableName, string newName)
        {
            if (string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(newName)) return;
            //if (selectedActivity == null) return;
            foreach (ModelItem item in this.GetWorkflowActivities())
            {
                ModelProperty property = item.Properties["ExpressionText"];
                if ((property != null) && (property.Value != null))
                {
                    string input = item.Properties["ExpressionText"].Value.ToString();
                    if (input.Contains(variableName))
                    {
                        string str2 = string.Empty;
                        foreach (string str3 in System.Text.RegularExpressions.Regex.Split(input, @"(\.)|(=)|(\+)|(-)|(\*)|(<)|(>)|(=)|(&)|(\s)|(\()|(\))"))
                        {
                            if (str3 == variableName)
                            {
                                str2 += newName;
                            }
                            else
                            {
                                str2 += str3;
                            }
                        }
                        item.Properties["ExpressionText"].SetValue(str2);
                    }
                }
            }
        }
        public void OnUncomment(object sender, RoutedEventArgs e)
        {
            var thisselection = selection;
            var comment = SelectedActivity;
            var currentSequence = SelectedActivity.Properties["Body"].Value;
            if (currentSequence == null) return;
            //var newSequence = GetActivitiesScope(SelectedActivity.Parent.Parent);
            var newSequence = GetActivitiesScope(SelectedActivity.Parent);
            ModelItemCollection currentActivities = null;
            if (currentSequence.Properties["Activities"] != null)
            {
                currentActivities = currentSequence.Properties["Activities"].Collection;
            }
            else if (currentSequence.Properties["Nodes"] != null)
            {
                currentActivities = currentSequence.Properties["Nodes"].Collection;
            }
            ModelItemCollection newActivities = null;
            if (newSequence.Properties["Activities"] != null)
            {
                newActivities = newSequence.Properties["Activities"].Collection;
            }
            else if (newSequence.Properties["Nodes"] != null)
            {
                newActivities = newSequence.Properties["Nodes"].Collection;
                var next = thisselection.PrimarySelection.Parent.Properties["Next"];

                newActivities.Remove(thisselection.PrimarySelection.Parent);

                FlowStep step = new FlowStep
                {
                    Action = new Sequence()
                };
                var newStep = newActivities.Add(step);
                newStep.Properties["Action"].SetValue(comment.Properties["Body"].Value);
                newStep.Properties["Next"].SetValue(next.Value);

                if (newSequence.Properties["StartNode"].Value == thisselection.PrimarySelection.Parent)
                {
                    newSequence.Properties["StartNode"].SetValue(newStep);
                }
                foreach (var node in newActivities)
                {
                    if (node.Properties["Next"] != null && node.Properties["Next"].Value != null)
                    {
                        if (node.Properties["Next"].Value == thisselection.PrimarySelection.Parent)
                        {
                            node.Properties["Next"].SetValue(newStep);
                        }
                    }
                }
                return;
            }

            if (currentActivities != null && newActivities != null)
            {
                var index = newActivities.IndexOf(comment);
                foreach (var sel in currentActivities.ToList())
                {
                    currentActivities.Remove(sel);
                    index++;
                    newActivities.Insert(index, sel);
                    //newActivities.Add(sel);
                }
                newActivities.Remove(comment);
            }
            if (currentActivities != null && currentActivities.Count == 1 && comment.Parent.Properties["Handler"] != null)
            {
                var handler = comment.Parent.Properties["Handler"];
                handler.SetValue(currentActivities.First());
            }
            else if (currentActivities == null && newActivities != null)
            {
                var index = newActivities.IndexOf(comment);
                var movethis = comment.Properties["Body"].Value;
                newActivities.Insert(index, movethis);
                newActivities.Remove(comment);
            }
            else if (currentActivities == null && newActivities == null)
            {
                if (newSequence.Properties["Body"] != null)
                {
                    var body = newSequence.Properties["Body"];
                    var handler = body.Value.Properties["Handler"];
                    handler.SetValue(handler.Value.Properties["Body"].Value);
                }
                else if (newSequence.Properties["Handler"] != null)
                {
                    var handler = newSequence.Properties["Handler"];
                    handler.SetValue(currentSequence);
                }
            }
            else if (currentSequence != null && comment.Parent != null && comment.Parent.Properties["Handler"] != null)
            {
                var handler = comment.Parent.Properties["Handler"];
                handler.SetValue(currentSequence);
            }
        }
        private void OnComment(object sender, RoutedEventArgs e)
        {
            var thisselection = selection;
            var pri = thisselection.PrimarySelection;
            if (pri == null) return;
            //var movethis = selectedActivity;

            var lastSequence = GetActivitiesScope(SelectedActivity.Parent);
            if (lastSequence == null) lastSequence = GetActivitiesScope(SelectedActivity);
            ModelItemCollection Activities = null;
            if (lastSequence.Properties["Activities"] != null)
            {
                Activities = lastSequence.Properties["Activities"].Collection;
            }
            else if (lastSequence.Properties["Nodes"] != null)
            {
                Activities = lastSequence.Properties["Nodes"].Collection;
            }

            if (SelectedActivity.ItemType == typeof(Sequence))
            {
                var parent = SelectedActivity.Parent;
                if (SelectedActivity.Parent.ItemType == typeof(ActivityBuilder)) return;
                if (parent.Properties["Activities"] != null)
                {
                    Activities = parent.Properties["Activities"].Collection;
                }
                var co = new Activities.CommentOut
                {
                    Body = SelectedActivity.GetCurrentValue() as Activity
                };
                if (Activities == null)
                {
                    var item = thisselection.PrimarySelection.Parent.Properties["Handler"].SetValue(co);
                }
                else
                {
                    Activities.Remove(SelectedActivity);
                    Activities.Add(co);
                }
            }
            else if (thisselection.SelectionCount > 1 || thisselection.PrimarySelection.ItemType == typeof(Sequence))
            {
                if (lastSequence.Properties["Nodes"] != null) return;
                var co = new Activities.CommentOut
                {
                    Body = new Sequence()
                };
                if (Activities == null)
                {
                    var item = thisselection.PrimarySelection.Parent.Properties["Handler"].SetValue(co);
                    var newActivities = item.Properties["Body"].Value.Properties["Activities"].Collection;
                    foreach (var sel in thisselection.SelectedObjects)
                    {
                        if (Activities != null) Activities.Remove(sel);
                        var index = newActivities.Count;
                        Log.Debug("insert at " + index);
                        newActivities.Insert(0, sel);
                        //newActivities.Add(sel);
                    }
                }
                else
                {
                    AddActivity(co);
                    var newActivities = SelectedActivity.Properties["Body"].Value.Properties["Activities"].Collection;
                    foreach (var sel in thisselection.SelectedObjects)
                    {
                        if (Activities != null) Activities.Remove(sel);
                        var index = newActivities.Count;
                        Log.Debug("insert at " + index);
                        newActivities.Insert(0, sel);
                        //newActivities.Add(sel);
                    }
                }

            }
            else
            {
                var parentparent = thisselection.PrimarySelection.Parent.Parent;
                var parent = thisselection.PrimarySelection.Parent;

                if (parentparent == lastSequence)
                {
                    var co = new Activities.CommentOut();
                    AddActivity(co);
                    Activities.Remove(thisselection.PrimarySelection);
                    SelectedActivity.Properties["Body"].SetValue(thisselection.PrimarySelection);
                }
                else
                {
                    try
                    {
                        if (parentparent.Properties["Body"] != null)
                        {
                            var body = parentparent.Properties["Body"];
                            if (body.Value == null)
                            {
                                var aa = (dynamic)Activator.CreateInstance(body.PropertyType);
                                //aa.Handler = new CommentOut();
                                SelectedActivity = parentparent.Properties["Body"].SetValue(aa);
                            }
                            var handler = body.Value.Properties["Handler"];
                            var comment = handler.SetValue(new Activities.CommentOut());
                            comment.Properties["Body"].SetValue(thisselection.PrimarySelection);

                            //p.Properties["Body"].Value.Properties["Handler"].Value.Properties["Body"].SetValue(thisselection.PrimarySelection);
                        }
                        else if (parent.Properties["Action"] != null)
                        {
                            var next = thisselection.PrimarySelection.Parent.Properties["Next"];
                            var co = new Activities.CommentOut();
                            var comment = AddActivity(co);
                            Activities.Remove(thisselection.PrimarySelection.Parent);

                            if (lastSequence.Properties["StartNode"].Value == thisselection.PrimarySelection.Parent)
                            {
                                lastSequence.Properties["StartNode"].SetValue(comment);
                            }
                            foreach (var node in Activities)
                            {
                                if (node.Properties["Next"] != null && node.Properties["Next"].Value != null)
                                {
                                    if (node.Properties["Next"].Value == thisselection.PrimarySelection.Parent)
                                    {
                                        node.Properties["Next"].SetValue(comment);
                                    }
                                }
                            }


                            if (comment.Properties["Body"] != null)
                            {
                                comment.Properties["Body"].SetValue(thisselection.PrimarySelection);
                            }
                            else if (comment.Properties["Action"] != null)
                            {
                                comment.Properties["Action"].Value.Properties["Body"].SetValue(thisselection.PrimarySelection);
                                comment.Properties["Next"].SetValue(next.Value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
        }
        private void OnRunthis(object sender, RoutedEventArgs e)
        {
            var thisselection = selection;
            if (selection == null) return;
            if (selection.SelectedObjects.Count() == 0) return;
            var modelitem = selection.SelectedObjects.ElementAt(0);
            var p = modelitem.Properties["Id"];
            var id = (string)p.ComputedValue;

            GenericTools.RunUI(() =>
            {
                this.VisualTracking = VisualTracking; this.SlowMotion = SlowMotion;
                if (BreakPointhit)
                {
                    Singlestep = false;
                    BreakPointhit = false;
                    if (!VisualTracking && Config.local.minimize) GenericTools.Minimize();
                    if (ResumeRuntimeFromHost != null) ResumeRuntimeFromHost.Set();
                    return;
                }
                WorkflowDesigner.Flush();

                if (global.isConnected)
                {
                    if (!Workflow.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.invoke))
                    {
                        Log.Error("Access denied, " + global.webSocketClient.user.username + " does not have invoke permission");
                        return;
                    }
                }
                WorkflowInstance instance = null;
                if (instance == null)
                {
                    var param = new Dictionary<string, object>();
                    BreakpointLocations = WorkflowDesigner.DebugManagerView.GetBreakpointLocations();
                    if (SlowMotion || VisualTracking || BreakpointLocations.Count > 0)
                    {
                        instance = Workflow.CreateInstance(param, null, null, IdleOrComplete, OnVisualTracking) as WorkflowInstance;
                    }
                    else
                    {
                        instance = Workflow.CreateInstance(param, null, null, IdleOrComplete, null) as WorkflowInstance;
                    }
                }
                ReadOnly = true;
                if (!VisualTracking && Config.local.minimize) GenericTools.Minimize();

                if (!_activityIdMapping.ContainsKey(id))
                {
                    InitializeStateEnvironment();
                }
                // if (instance != null) instance.Run();
                if (_activityIdMapping.ContainsKey(id))
                {
                    var a = _activityIdMapping[id];
                    var root = _activityIdMapping["1.1"];
                    //var modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
                    //var rootModel = modelService.Root;
                    //instance.Run(root, id);
                    instance.RunThis(root, a);
                }
                else
                {
                    Log.Error("Failed finding activity " + id + ", try and close and reopen the designer");
                }

            });
        }
        private void OnRunFromHere(object sender, RoutedEventArgs e)
        {
            var thisselection = selection;
            if (selection == null) return;
            if (selection.SelectedObjects.Count() == 0) return;
            var modelitem = selection.SelectedObjects.ElementAt(0);
            var p = modelitem.Properties["Id"];
            var id = (string)p.ComputedValue;

            GenericTools.RunUI(() =>
            {
                this.VisualTracking = VisualTracking; this.SlowMotion = SlowMotion;
                if (BreakPointhit)
                {
                    Singlestep = false;
                    BreakPointhit = false;
                    if (!VisualTracking && Config.local.minimize) GenericTools.Minimize();
                    if (ResumeRuntimeFromHost != null) ResumeRuntimeFromHost.Set();
                    return;
                }
                WorkflowDesigner.Flush();
                InitializeStateEnvironment();
                if (global.isConnected)
                {
                    if (!Workflow.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.invoke))
                    {
                        Log.Error("Access denied, " + global.webSocketClient.user.username + " does not have invoke permission");
                        return;
                    }
                }
                WorkflowInstance instance = null;
                if (instance == null)
                {
                    var param = new Dictionary<string, object>();
                    BreakpointLocations = WorkflowDesigner.DebugManagerView.GetBreakpointLocations();
                    if (SlowMotion || VisualTracking || BreakpointLocations.Count > 0)
                    {
                        instance = Workflow.CreateInstance(param, null, null, IdleOrComplete, OnVisualTracking) as WorkflowInstance;
                    }
                    else
                    {
                        instance = Workflow.CreateInstance(param, null, null, IdleOrComplete, null) as WorkflowInstance;
                    }
                }
                ReadOnly = true;
                if (!VisualTracking && Config.local.minimize) GenericTools.Minimize();

                // if (instance != null) instance.Run();

                var a = _activityIdMapping[id];
                var root = _activityIdMapping["1.1"];
                //var modelService = WorkflowDesigner.Context.Services.GetService<ModelService>();
                //var rootModel = modelService.Root;
                //instance.Run(root, id);
                instance.RunFromHere(root, a);

            });
        }
        public static async Task<string> LoadImages(string xaml)
        {
            WorkflowDesigner wfDesigner;
            wfDesigner = new WorkflowDesigner();
            wfDesigner.Context.Services.GetService<DesignerConfigurationService>().TargetFrameworkName = new System.Runtime.Versioning.FrameworkName(".NETFramework", new Version(4, 5));
            wfDesigner.Context.Services.GetService<DesignerConfigurationService>().LoadingFromUntrustedSourceEnabled = true;
            new DesignerMetadata().Register();
            wfDesigner.Text = xaml;
            wfDesigner.Load();
            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
            using (ModelEditingScope editingScope = modelService.Root.BeginEdit("Implementation"))
            {
                foreach (ModelItem item in GetWorkflowActivities(wfDesigner))
                {
                    ModelProperty property = item.Properties["Image"];
                    if ((property != null) && (property.Value != null))
                    {
                        string image = item.Properties["Image"].Value.ToString();
                        if (System.Text.RegularExpressions.Regex.Match(image, "[a-f0-9]{24}").Success)
                        {
                            using (var b = await Interfaces.Image.Util.LoadBitmap(image))
                            {
                                image = Interfaces.Image.Util.Bitmap2Base64(b);
                            }
                            item.Properties["Image"].SetValue(image);
                        }
                    }
                }
                editingScope.Complete();
            }
            wfDesigner.Flush();
            return wfDesigner.Text;
        }
        public static string SetWorkflowName(string xaml, string name)
        {
            WorkflowDesigner wfDesigner;
            wfDesigner = new WorkflowDesigner();
            wfDesigner.Context.Services.GetService<DesignerConfigurationService>().TargetFrameworkName = new System.Runtime.Versioning.FrameworkName(".NETFramework", new Version(4, 5));
            wfDesigner.Context.Services.GetService<DesignerConfigurationService>().LoadingFromUntrustedSourceEnabled = true;
            new DesignerMetadata().Register();
            wfDesigner.Text = xaml;
            wfDesigner.Load();
            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
            using (ModelEditingScope editingScope = modelService.Root.BeginEdit("Implementation"))
            {
                var modelItem = wfDesigner.Context.Services.GetService<ModelService>().Root;
                modelItem.Properties["Name"].SetValue(name.Replace("_", " "));
                editingScope.Complete();
            }
            wfDesigner.Flush();
            return wfDesigner.Text;
        }
    }
}
