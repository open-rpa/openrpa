using Newtonsoft.Json.Linq;
using OpenRPA.ExpressionEditor;
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

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for WFDesigner.xaml
    /// </summary>
    public partial class WFDesigner : UserControl, System.ComponentModel.INotifyPropertyChanged, IDesigner
    {
        public DelegateCommand DockAsDocumentCommand = new DelegateCommand((e) => { }, (e) => false);
        public DelegateCommand AutoHideCommand { get; set; }= new DelegateCommand((e) => { }, (e) => false);
        public bool CanClose{ get; set; } = true;
        public bool CanHide { get; set; } = false;
        public Dictionary<ModelItem, System.Activities.Debugger.SourceLocation> _modelLocationMapping = new Dictionary<ModelItem, System.Activities.Debugger.SourceLocation>();
        public Dictionary<string, System.Activities.Debugger.SourceLocation> _sourceLocationMapping = new Dictionary<string, System.Activities.Debugger.SourceLocation>();
        public Dictionary<string, Activity> _activityIdMapping = new Dictionary<string, Activity>();
        public Dictionary<Activity, System.Activities.Debugger.SourceLocation> _activitysourceLocationMapping = new Dictionary<Activity, System.Activities.Debugger.SourceLocation>();
        public Dictionary<string, ModelItem> _activityIdModelItemMapping = new Dictionary<string, ModelItem>();
        private string SelectedVariableName = null;
        private Selection selection = null;
        private MenuItem comment;
        private MenuItem uncomment;
        // public static ICommand CmdOutComment = new RoutedCommand("CmdOutComment", typeof(WFDesigner));
        public bool BreakPointhit { get; set; }
        public bool Singlestep { get; set; }
        public bool SlowMotion { get; set; }
        public bool Minimize { get; set; } = true;
        public bool VisualTracking { get; set; }
        public bool isRunnning {
            get
            {
                foreach(var i in WorkflowInstance.Instances)
                {
                    if(!string.IsNullOrEmpty(Workflow._id) && i.WorkflowId == Workflow._id)
                    {
                        if(i.state != "completed" && i.state != "aborted" && i.state != "failed")
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
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public System.Threading.AutoResetEvent resumeRuntimeFromHost { get; set; }
        public System.Activities.Activity lastinserted { get; set; }
        public System.Activities.Presentation.Model.ModelItem lastinsertedmodel { get; set; }
        public Action<WFDesigner> onChanged { get; set; }
        public WorkflowDesigner wfDesigner { get; private set; }
        public Workflow Workflow { get; private set; }
        public bool HasChanged { get; private set; }
        public ModelItem selectedActivity { get; private set; }
        public Project Project
        {
            get
            {
                return Workflow.Project;
            }
        }
        private void onCancel()
        {
            GenericTools.RunUI(() =>
            {
                if (tab == null) return;
                if (!tab.IsSelected) return;
                foreach (var i in WorkflowInstance.Instances)
                {
                    if (i.WorkflowId == Workflow._id && !i.isCompleted)
                    {
                        i.Abort("User canceled workflow with cancel key");
                    }
                }
                if (resumeRuntimeFromHost != null) resumeRuntimeFromHost.Set();

            });
        }
        private void OnKeyUp(Input.InputEventArgs e)
        {
            GenericTools.RunUI(() => {
                if (tab == null) return;
                if (!tab.IsSelected) return;
                if (e.Key == Input.KeyboardKey.F10 || e.Key == Input.KeyboardKey.F11)
                {
                    if (!isRunnning)
                    {
                        if (currentprocessid == 0) currentprocessid = System.Diagnostics.Process.GetCurrentProcess().Id;
                        var element = AutomationHelper.GetFromFocusedElement();
                        if (element.ProcessId != currentprocessid) return;
                    }
                    Singlestep = true;
                    // if (e.Key == Input.KeyboardKey.F11) { StepInto = true; }
                    if (BreakPointhit)
                    {
                        if (resumeRuntimeFromHost != null) resumeRuntimeFromHost.Set();
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
            if(!isRunnning)
            {
                if (currentprocessid == 0) currentprocessid = System.Diagnostics.Process.GetCurrentProcess().Id;
                var element = AutomationHelper.GetFromFocusedElement();
                if (element.ProcessId != currentprocessid) return;
            }
            if (e.Key == Key.F5)
            {
                if (BreakPointhit)
                {
                    Singlestep = false;
                    BreakPointhit = false;
                    resumeRuntimeFromHost.Set();
                    return;
                }
                try
                {
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
                return wfDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.ReadOnlyState>().IsReadOnly;
            }
            set
            {
                wfDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.ReadOnlyState>().IsReadOnly = value;
            }
        }
        private WFDesigner()
        {
            InitializeComponent();
        }
        private static object _lock = new object();
        public void ReloadDesigner()
        {
            lock(_lock)
            { 
            LoadDesigner();
            }
        }
        public void LoadDesigner()
        {
            wfDesigner = new WorkflowDesigner();
            DesignerConfigurationService configService = wfDesigner.Context.Services.GetRequiredService<DesignerConfigurationService>();
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

            wfDesigner.Context.Services.Publish<IExpressionEditorService>(new EditorService(this));
            if (!string.IsNullOrEmpty(Workflow.Xaml))
            {
                wfDesigner.Text = Workflow.Xaml;
                wfDesigner.Load();
            }
            else
            {
                Activity wf = new System.Activities.Statements.Sequence { };
                var ab = new ActivityBuilder();
                ab.Name = Workflow.name;
                ab.Implementation = wf;
                
                // typeof(Microsoft.VisualBasic.Collection),

                AddVBNamespaceSettings(ab, typeof(Action),
                    typeof(System.Xml.XmlNode),
                    typeof(OpenRPA.Workflow),
                    typeof(OpenRPA.UIElement),
                    typeof(System.Data.DataSet),
                    typeof(System.Linq.Enumerable)
                    );
                AddVBNamespaceSettings(ab, extratypes);

                //if (workflow.language == entity.workflowLanguage.CSharp)
                //{
                //    System.Activities.Presentation.Expressions.ExpressionActivityEditor.SetExpressionActivityEditor(ab, "C#");
                //}
                wfDesigner.Load(ab);
            }
            if (global.isConnected)
            {
                ReadOnly = !Workflow.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.update);
            }

            HasChanged = false;
            wfDesigner.ModelChanged += (sender, e) =>
            {
                if (!HasChanged)
                {
                    // _modelLocationMapping.Clear();
                    //_sourceLocationMapping.Clear();
                    //_activityIdMapping.Clear();
                    //_activitysourceLocationMapping.Clear();
                    //_activityIdModelItemMapping.Clear();
                }
                HasChanged = true;

                onChanged?.Invoke(this);
            };
            wfDesigner.Context.Items.Subscribe(new SubscribeContextCallback<Selection>(SelectionChanged));
            wfDesigner.View.Dispatcher.UnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(UnhandledException);
            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
            modelService.ModelChanged -= new EventHandler<ModelChangedEventArgs>(ModelChanged);
            modelService.ModelChanged += new EventHandler<ModelChangedEventArgs>(ModelChanged);
            wfDesigner.ContextMenu.Items.Add(comment);
            var ms = wfDesigner.Context.Services.GetService<ModelService>();

            try
            {
                if (ms != null)
                {
                    var modelItem = ms.Root;
                    Workflow.name = modelItem.GetValue<string>("Name");
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }

            // WfDesignerBorder.Child = wfDesigner.View;
            Properties = wfDesigner.PropertyInspectorView;

        }
        private Type[] extratypes = null;
        public WFDesigner(Xceed.Wpf.AvalonDock.Layout.LayoutDocument tab, Workflow workflow, Type[] extratypes)
        {
            InitializeComponent();
            this.extratypes = extratypes;
            DataContext = this;
            this.tab = tab;
            //toolbox = InitializeActivitiesToolbox();
            //// WfToolboxBorder.Child = toolbox;
            Workflow = workflow;
            Input.InputDriver.Instance.onCancel += onCancel;
            if (tab != null)
            {
                tab.Title = workflow.name;
            }
            //WeakEventManager<System.ComponentModel.INotifyPropertyChanged, System.ComponentModel.PropertyChangedEventArgs>.
            //    AddHandler(MainWindow.tracing, "PropertyChanged", traceOnPropertyChanged);
            if(isRunnning)
            {
                ReadOnly = true;
            }
            comment = new MenuItem() { Header = "Comment out" };
            uncomment = new MenuItem() { Header = "Uncomment" };
            comment.Click += onComment;
            uncomment.Click += onUncomment;

            LoadDesigner();
        }
        private void UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception.ToString());
        }
        //private void traceOnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == "OutputMessages")
        //        OutputMessages = MainWindow.tracing.OutputMessages;
        //    if (e.PropertyName == "TraceMessages")
        //        TraceMessages = MainWindow.tracing.TraceMessages;
        //}
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        public async Task Save()
        {
            try
            {
                parseparameters();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            wfDesigner.Flush();
            //if (_activityIdMapping.Count == 0)
            //{
            //    int failCounter = 0;
            //    while (_activityIdMapping.Count == 0 && failCounter < 1)
            //    {
            //        InitializeStateEnvironment(true);
            //        System.Threading.Thread.Sleep(500);
            //        failCounter++;
            //    }
            //}

            var modelItem = wfDesigner.Context.Services.GetService<ModelService>().Root;
            Workflow.name = modelItem.GetValue<string>("Name");
            Workflow.Xaml = wfDesigner.Text;
            await Workflow.Save();
            if (HasChanged)
            {
                HasChanged = false;
                onChanged?.Invoke(this);
            }
        }
        public KeyedCollection<string, DynamicActivityProperty> GetParameters()
        {
            ActivityBuilder ab2;

            using (var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(Workflow.Xaml)))
            {
                ab2 = System.Xaml.XamlServices.Load(
                    System.Activities.XamlIntegration.ActivityXamlServices.CreateBuilderReader(
                    new System.Xaml.XamlXmlReader(stream))) as ActivityBuilder;
            }
            return ab2.Properties;
        }
        public void parseparameters()
        {
            Workflow.Serializable = true;
            Workflow.Parameters.Clear();
            if(!string.IsNullOrEmpty(Workflow.Xaml))
            {
                var parameters = GetParameters();
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
                ModelTreeManager mtm = wfDesigner.Context.Services.GetService<ModelTreeManager>();
                bool canIdle = false;
                foreach (ModelItem item in this.GetWorkflowActivities(null))
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
                                Log.Activity(string.Format("Activity: '{0}' Can induce idle, need to check if workflow is serializable", ToString()));
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
                    foreach (ModelItem item in this.GetWorkflowActivities(null))
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
        public List<ModelItem> GetWorkflowActivities(ModelItem startingPoint = null)
        {
            List<ModelItem> list = new List<ModelItem>();

            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
            list = modelService.Find(modelService.Root, typeof(Activity)).ToList<ModelItem>();

            list.AddRange(modelService.Find(modelService.Root, (Predicate<Type>)(type => (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(FlowSwitch<>))))));
            list.AddRange(modelService.Find(modelService.Root, typeof(FlowDecision)));
            return list;
        }
        private static List<ModelItem> GetWorkflowActivities(WorkflowDesigner wfDesigner, ModelItem startingPoint = null)
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
            selectedActivity = selection.PrimarySelection;
            if (selectedActivity == null) return;
            SelectedVariableName = selectedActivity.GetCurrentValue().ToString();

            try
            {
                if (wfDesigner.ContextMenu.Items.Contains(comment)) wfDesigner.ContextMenu.Items.Remove(comment);
                if (wfDesigner.ContextMenu.Items.Contains(uncomment)) wfDesigner.ContextMenu.Items.Remove(uncomment);
                var lastSequence = GetActivitiesScope(selectedActivity.Parent);
                if (lastSequence == null) lastSequence = GetActivitiesScope(selectedActivity);
                if (lastSequence == null) return;
                if (selectedActivity.ItemType == typeof(Activities.CommentOut))
                {
                    wfDesigner.ContextMenu.Items.Add(uncomment);
                }
                else if (lastSequence.ItemType != typeof(Flowchart))
                {
                    if (selection.SelectionCount > 1)
                    {
                        if (lastSequence.Properties["Nodes"] == null)
                        {
                            wfDesigner.ContextMenu.Items.Add(comment);
                        }
                    }
                    else
                    {
                        wfDesigner.ContextMenu.Items.Add(comment);
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
        public void AddVBNamespaceSettings(object rootObject, params Type[] types)
        {
            var vbsettings = Microsoft.VisualBasic.Activities.VisualBasic.GetSettings(rootObject);
            if (vbsettings == null)
            {
                vbsettings = new Microsoft.VisualBasic.Activities.VisualBasicSettings();
            }


            foreach (Type t in types)
            {
                vbsettings.ImportReferences.Add(
                    new Microsoft.VisualBasic.Activities.VisualBasicImportReference
                    {
                        Assembly = t.Assembly.GetName().Name,
                        Import = t.Namespace
                    });
            }

            Microsoft.VisualBasic.Activities.VisualBasic.SetSettings(rootObject, vbsettings);
        }
        public ModelItem addActivity(Activity a)
        {
            ModelItem newItem = null;
            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
            using (ModelEditingScope editingScope = modelService.Root.BeginEdit("Implementation"))
            {
                var lastSequence = GetSequence(selectedActivity);
                if (lastSequence == null && selectedActivity != null) lastSequence = GetActivitiesScope(selectedActivity.Parent);
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
                        if (Activities[i].Equals(selectedActivity))
                        {
                            insertAt = (i + 1);
                        }
                    }
                    if (lastSequence.Properties["Activities"] != null)
                    {
                        newItem = Activities.Insert(insertAt, a);
                    }
                    else
                    {
                        FlowStep step = new FlowStep();
                        step.Action = a;
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
                selectedActivity = newItem;
                newItem.Focus(20);
                Selection.SelectOnly(wfDesigner.Context, newItem);
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

            while (parent != null && parent.Properties["Variables"] == null)
            {
                parent = parent.Parent;
            }
            return parent;
        }
        private ModelItem GetActivitiesScope(ModelItem from)
        {
            ModelItem parent = from;

            while (parent != null && parent.Properties["Activities"] == null && parent.Properties["Nodes"] == null)
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
            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
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
            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
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
            if (selectedActivity == null) throw new Exception("Cannot get variable when no activity has been selected");
            var seq = GetVariableScope(selectedActivity);
            if (seq == null) throw new Exception("Cannot add variables to root activity!");
            Variable<T> result = null;
            result = GetVariableModel<T>(Name, selectedActivity);
            if (result == null)
            {
                ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();
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
            var debugView = wfDesigner.DebugManagerView;

            var nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            var debuggerServiceType = typeof(System.Activities.Presentation.Debug.DebuggerService);
            var ensureMappingMethodName = "EnsureSourceLocationUpdated";
            var ensureMappingMethod = debuggerServiceType.GetMethod(ensureMappingMethodName, nonPublicInstance);
            var res = ensureMappingMethod.Invoke(debugView, new object[0]);
        }
        private Dictionary<Activity, System.Activities.Debugger.SourceLocation> CreateSourceLocationMapping(ModelService modelService)
        {
            var debugView = wfDesigner.DebugManagerView;

            var nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            var debuggerServiceType = typeof(System.Activities.Presentation.Debug.DebuggerService);
            var mappingFieldName = "instanceToSourceLocationMapping";
            var mappingField = debuggerServiceType.GetField(mappingFieldName, nonPublicInstance);
            if (mappingField == null)
                throw new MissingFieldException(debuggerServiceType.FullName, mappingFieldName);

            var rootActivity = modelService.Root.GetCurrentValue() as Activity;
            if (rootActivity == null)
            {
                wfDesigner.Flush();
                System.Activities.XamlIntegration.ActivityXamlServicesSettings activitySettings = new System.Activities.XamlIntegration.ActivityXamlServicesSettings
                {
                    CompileExpressions = true
                };
                var xamlReaderSettings = new System.Xaml.XamlXmlReaderSettings { LocalAssembly = typeof(WFDesigner).Assembly };
                var xamlReader = new System.Xaml.XamlXmlReader(new System.IO.StringReader(wfDesigner.Text), xamlReaderSettings);
                rootActivity = System.Activities.XamlIntegration.ActivityXamlServices.Load(xamlReader, activitySettings);
            }
            WorkflowInspectionServices.CacheMetadata(rootActivity);

            EnsureSourceLocationUpdated();
            var mapping = (Dictionary<object, System.Activities.Debugger.SourceLocation>)mappingField.GetValue(debugView);
            var result = new Dictionary<Activity, System.Activities.Debugger.SourceLocation>();
            foreach (var m in mapping)
            {
                try
                {
                    var a = m.Key as Activity;
                    if (a != null) result.Add(a, m.Value);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
            }
            return result;
        }
        private System.Activities.Debugger.SourceLocation GetSourceLocationFromModelItem(ModelItem modelItem)
        {
            var debugView = wfDesigner.DebugManagerView;
            var nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            var debuggerServiceType = typeof(System.Activities.Presentation.Debug.DebuggerService);
            var ensureMappingMethodName = "GetSourceLocationFromModelItem";
            var ensureMappingMethod = debuggerServiceType.GetMethod(ensureMappingMethodName, nonPublicInstance);
            var res = ensureMappingMethod.Invoke(debugView, new object[] { modelItem });
            return res as System.Activities.Debugger.SourceLocation;
        }
        public void SetDebugLocation(System.Activities.Debugger.SourceLocation location)
        {
            wfDesigner.DebugManagerView.CurrentLocation = location;
        }
        public void NavigateTo(ModelItem item)
        {
            var validation = wfDesigner.Context.Services.GetService<System.Activities.Presentation.Validation.ValidationService>();
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
        public void InitializeStateEnvironment22222(bool Toggle)
        {
            GenericTools.RunUI(() =>
            {
                try
                {


                    //object rootInstance = WorkflowHelper.GetRootInstance(wfDesigner);
                    //Dictionary<object, SourceLocation> sourceLocationMapping = new Dictionary<object, SourceLocation>();
                    //Dictionary<object, SourceLocation> designerSourceLocationMapping = new Dictionary<object, SourceLocation>();

                    //if (rootInstance != null)
                    //{
                    //    Activity documentRootElement = WorkflowHelper.GetRootWorkflowElement(rootInstance);

                    //    //SourceLocationProvider.CollectMapping(
                    //    //    WorkflowHelper.GetRootRuntimeWorkflowElement(this.WorkflowActivity),
                    //    //    documentRootElement, sourceLocationMapping,
                    //    //    wfDesigner.Context.Items.GetValue<WorkflowFileItem>().LoadedFile);
                    //    SourceLocationProvider.CollectMapping(
                    //        WorkflowHelper.GetRootRuntimeWorkflowElement(documentRootElement),
                    //        documentRootElement, sourceLocationMapping,
                    //        wfDesigner.Context.Items.GetValue<WorkflowFileItem>().LoadedFile);

                    //    SourceLocationProvider.CollectMapping(
                    //        documentRootElement,
                    //        documentRootElement,
                    //        designerSourceLocationMapping,
                    //       wfDesigner.Context.Items.GetValue<WorkflowFileItem>().LoadedFile);

                    //}
                    var modelService = wfDesigner.Context.Services.GetService<ModelService>();
                    IEnumerable<ModelItem> wfElements = modelService.Find(modelService.Root, typeof(Activity)).Union(modelService.Find(modelService.Root, typeof(System.Activities.Debugger.State)));
                    var map = CreateSourceLocationMapping(modelService);
                    //_sourceLocationMapping.Clear();
                    //_activitysourceLocationMapping.Clear();
                    //_activityIdModelItemMapping.Clear();
                    // _modelLocationMapping.Clear();

                    foreach (var modelItem in wfElements)
                    {
                        var loc = GetSourceLocationFromModelItem(modelItem);
                        var activity = modelItem.GetCurrentValue() as Activity;
                        var id = activity.Id;
                        if(string.IsNullOrEmpty(id))
                        {
                            //Selection.SelectOnly(wfDesigner.Context, modelItem);
                            //        if (wfDesigner.DebugManagerView.SelectedLocation != null)
                            //        {
                            //            _modelLocationMapping[modelItem] = wfDesigner.DebugManagerView.SelectedLocation;
                            //            _activitysourceLocationMapping[activity] = wfDesigner.DebugManagerView.SelectedLocation;
                            //            _sourceLocationMapping[activity.Id] = wfDesigner.DebugManagerView.SelectedLocation;
                            //            _activityIdMapping[activity.Id] = activity;
                            //            _activityIdModelItemMapping[activity.Id] = modelItem;
                        }
                        if (string.IsNullOrEmpty(id)) continue;
                        //_activitysourceLocationMapping.Add(activity, loc);
                        //_sourceLocationMapping.Add(id, loc);
                        //_activityIdMapping.Add(id, activity);
                        //_activityIdModelItemMapping.Add(id, modelItem);
                    }

                    //foreach (var kp in designerSourceLocationMapping)
                    //{
                    //    var activity = kp.Key as Activity;
                    //    _activitysourceLocationMapping.Add(activity, kp.Value);
                    //    if (map.ContainsKey(activity))
                    //    {
                    //        // _modelLocationMapping[modelItem] = map[activity];
                    //        _activitysourceLocationMapping[activity] = map[activity];
                    //        _sourceLocationMapping[activity.Id] = map[activity];
                    //        _activityIdMapping[activity.Id] = activity;

                    //        Log.Debug(string.Format("Failed mapping '{0}' / '{1}' ", activity.Id, activity.DisplayName));
                    //    }
                    //}
                    //if (wfDesigner.DebugManagerView != null)
                    //{
                    //    ((System.Activities.Presentation.Debug.DebuggerService)wfDesigner.DebugManagerView).UpdateSourceLocations(designerSourceLocationMapping);
                    //}
                    //                    public Dictionary<ModelItem, System.Activities.Debugger.SourceLocation> _modelLocationMapping = new Dictionary<ModelItem, System.Activities.Debugger.SourceLocation>();
                    //public Dictionary<string, System.Activities.Debugger.SourceLocation> _sourceLocationMapping = new Dictionary<string, System.Activities.Debugger.SourceLocation>();
                    //public Dictionary<string, Activity> _activityIdMapping = new Dictionary<string, Activity>();
                    //public Dictionary<Activity, System.Activities.Debugger.SourceLocation> _activitysourceLocationMapping = new Dictionary<Activity, System.Activities.Debugger.SourceLocation>();
                    //public Dictionary<string, ModelItem> _activityIdModelItemMapping = new Dictionary<string, ModelItem>();


                    //Log.Debug("****** Create activity Map");
                    //var modelService = wfDesigner.Context.Services.GetService<ModelService>();
                    //try
                    //{
                    //    wfDesigner.Flush();
                    //    using (var ms = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(wfDesigner.Text)))
                    //    {
                    //        var _workflowToRun = System.Activities.XamlIntegration.ActivityXamlServices.Load(ms) as DynamicActivity;
                    //        WorkflowInspectionServices.CacheMetadata(_workflowToRun);
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    Log.Debug("InitializeStateEnvironment: " + ex.Message);
                    //}
                    //_sourceLocationMapping.Clear();
                    //_activitysourceLocationMapping.Clear();
                    //_modelLocationMapping.Clear();
                    //IEnumerable<ModelItem> wfElements = modelService.Find(modelService.Root, typeof(Activity)).Union(modelService.Find(modelService.Root, typeof(System.Activities.Debugger.State)));
                    //ModelItem lastItem = null;
                    //var map = CreateSourceLocationMapping(modelService);



                    //foreach (var modelItem in wfElements)
                    //{
                    //    NavigateTo(modelItem);
                    //    // optimize, should we just use GetSourceLocationFromModelItem or continue using "select and get location" ?
                    //    var loc = GetSourceLocationFromModelItem(modelItem);

                    //    //if (modelItem.ItemType.BaseType == typeof(Literal)) continue;
                    //    if (modelItem.ItemType.Name.StartsWith("Literal")) continue;
                    //    if (modelItem.ItemType.Name.StartsWith("VisualBasicValue")) continue;
                    //    if (modelItem.ItemType.Name.StartsWith("VisualBasicReference")) continue;



                    //    var activity = modelItem.GetCurrentValue() as Activity;
                    //    if (activity == null || activity.Id == null)
                    //    {
                    //        var state = modelItem.GetCurrentValue() as System.Activities.Debugger.State;
                    //        var property = typeof(System.Activities.Debugger.State).GetProperty("InternalState", BindingFlags.Instance | BindingFlags.NonPublic);
                    //        if (state != null && property != null)
                    //        {
                    //            activity = property.GetValue(state) as Activity;
                    //        }
                    //    }
                    //    if (activity == null || activity.Id == null)
                    //    {
                    //        Log.Verbose("Debug!");
                    //    }
                    //    if (activity != null && activity.Id != null && !_sourceLocationMapping.ContainsKey(activity.Id))
                    //    {
                    //        Selection.SelectOnly(wfDesigner.Context, modelItem);

                    //        if (wfDesigner.DebugManagerView.SelectedLocation != null)
                    //        {
                    //            _modelLocationMapping[modelItem] = wfDesigner.DebugManagerView.SelectedLocation;
                    //            _activitysourceLocationMapping[activity] = wfDesigner.DebugManagerView.SelectedLocation;
                    //            _sourceLocationMapping[activity.Id] = wfDesigner.DebugManagerView.SelectedLocation;
                    //            _activityIdMapping[activity.Id] = activity;
                    //            _activityIdModelItemMapping[activity.Id] = modelItem;
                    //        }
                    //        else
                    //        {
                    //            var t = wfDesigner.DebugManagerView.SelectedLocation;
                    //            if (map.ContainsKey(activity))
                    //            {
                    //                _modelLocationMapping[modelItem] = map[activity];
                    //                _activitysourceLocationMapping[activity] = map[activity];
                    //                _sourceLocationMapping[activity.Id] = map[activity];
                    //                _activityIdMapping[activity.Id] = activity;

                    //                Log.Debug(string.Format("Failed mapping '{0}' / '{1}' ", activity.Id, activity.DisplayName));
                    //            }
                    //            else
                    //            {
                    //                Log.Debug(string.Format("Failed mapping '{0}' / '{1}' ", activity.Id, activity.DisplayName));
                    //            }
                    //        }
                    //    }
                    //    lastItem = modelItem;
                    //}
                    //if (lastItem != null && Toggle == true) Selection.Toggle(wfDesigner.Context, lastItem);
                    //Log.Debug("****** Activity Map completed");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }
        public void InitializeStateEnvironment()
        {
            GenericTools.RunUI(() =>
            {
                try
                {
                    var modelService = wfDesigner.Context.Services.GetService<ModelService>();
                    IEnumerable<ModelItem> wfElements = modelService.Find(modelService.Root, typeof(Activity)).Union(modelService.Find(modelService.Root, typeof(System.Activities.Debugger.State)));
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
                        var id = activity.Id;
                        if (string.IsNullOrEmpty(id)) continue;
                        if(_sourceLocationMapping.ContainsKey(id)) continue;
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
            var debugManagerView = wfDesigner.DebugManagerView;
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
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void onVisualTracking(WorkflowInstance Instance, string ActivityId, string ChildActivityId, string State)
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

                InitializeStateEnvironment();
                if (!_sourceLocationMapping.ContainsKey(ChildActivityId)) return;
                location = _sourceLocationMapping[ChildActivityId];
                if (location == null) return;
                if (!BreakPointhit)
                {
                    BreakPointhit = wfDesigner.DebugManagerView.GetBreakpointLocations().ContainsKey(location);
                }
                ModelItem model = _activityIdModelItemMapping[ChildActivityId];
                if (VisualTracking || BreakPointhit || Singlestep)
                {
                    GenericTools.RunUI(() =>
                    {
                        GenericTools.restore();
                        NavigateTo(model);
                        SetDebugLocation(location);

                    });
                }
                if (BreakPointhit || Singlestep)
                {
                    using (resumeRuntimeFromHost = new System.Threading.AutoResetEvent(false))
                    {
                        BreakPointhit = true;
                        showVariables(Instance.Variables);
                        GenericTools.restore();
                        resumeRuntimeFromHost.WaitOne();
                    }
                    resumeRuntimeFromHost = null;
                }
            }
            catch (Exception ex) 
            {
                Log.Error(ex.ToString());
            }
        }
        internal void onIdle(WorkflowInstance instance, EventArgs e)
        {
            if (!string.IsNullOrEmpty(instance.queuename) && !string.IsNullOrEmpty(instance.correlationId))
            {
                RobotCommand command = new RobotCommand();
                var data = JObject.FromObject(instance.Parameters);
                command.command = "invoke" + instance.state;
                command.workflowid = instance.WorkflowId;
                command.data = data;
                if ((instance.state == "failed" || instance.state == "aborted") && instance.Exception != null)
                {
                    command.data = JObject.FromObject(instance.Exception);
                }
                _ = global.webSocketClient.QueueMessage(instance.queuename, command, instance.correlationId);
                onChanged?.Invoke(this);
            }
            if (instance.state == "idle" && Singlestep == true)
            {
                GenericTools.minimize(GenericTools.mainWindow);
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
                    Properties = wfDesigner.PropertyInspectorView;
                });
            }
            if (instance.state != "idle")
            {
                GenericTools.RunUI(() =>
                {
                    Properties = wfDesigner.PropertyInspectorView;
                    if (global.isConnected)
                    {
                        ReadOnly = !Workflow.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.update);
                    }
                    else
                    {
                        ReadOnly = false;
                    }
                });

                BreakPointhit = false; Singlestep = false;
                if (string.IsNullOrEmpty(instance.queuename) && string.IsNullOrEmpty(instance.correlationId))
                {
                    GenericTools.restore(GenericTools.mainWindow);
                    if(instance.state != "completed")
                    {
                        System.Activities.Debugger.SourceLocation location;
                        if(instance.errorsource!=null && _sourceLocationMapping.ContainsKey(instance.errorsource))
                        {
                            GenericTools.RunUI(() =>
                            {
                                location = _sourceLocationMapping[instance.errorsource];
                                SetDebugLocation(location);
                                if(_activityIdModelItemMapping.ContainsKey(instance.errorsource))
                                {
                                    ModelItem model = _activityIdModelItemMapping[instance.errorsource];
                                    NavigateTo(model);
                                }
                            });
                        }
                    }
                }
                //string message = "#*****************************#" + Environment.NewLine;
                //if (instance.runWatch != null)
                //{
                //    message += ("# " + instance.Workflow.name + " " + instance.state + " in " + string.Format("{0:mm\\:ss\\.fff}", instance.runWatch.Elapsed));
                //}
                //else
                //{
                //    message += ("# " + instance.Workflow.name + " " + instance.state);
                //}
                //if (!string.IsNullOrEmpty(instance.errormessage)) message += (Environment.NewLine + "# " + instance.errormessage);
                string message = "";
                if (instance.runWatch != null)
                {
                    message += (instance.Workflow.name + " " + instance.state + " in " + string.Format("{0:mm\\:ss\\.fff}", instance.runWatch.Elapsed));
                }
                else
                {
                    message += (instance.Workflow.name + " " + instance.state);
                }
                if (!string.IsNullOrEmpty(instance.errormessage)) message += (Environment.NewLine + "# " + instance.errormessage);
                Log.Output(message);


                onChanged?.Invoke(this);
            }
            if (instance.hasError || instance.isCompleted)
            {
                foreach (var wi in WorkflowInstance.Instances)
                {
                    if (wi.isCompleted) continue;
                    if (wi.Bookmarks == null) continue;
                    foreach (var b in wi.Bookmarks)
                    {
                        if (b.Key == instance._id)
                        {
                            wi.ResumeBookmark(b.Key, instance);
                        }
                    }
                }
            }
        }
        public  void Run(bool VisualTracking, bool SlowMotion, WorkflowInstance instance)
        {
            this.VisualTracking = VisualTracking; this.SlowMotion = SlowMotion;
            if (BreakPointhit)
            {
                Singlestep = false;
                BreakPointhit = false;
                if (!VisualTracking && Minimize) GenericTools.minimize(GenericTools.mainWindow);
                if(resumeRuntimeFromHost!=null) resumeRuntimeFromHost.Set();
                return;
            }
            wfDesigner.Flush();
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
            if(instance == null)
            {
                var param = new Dictionary<string, object>();
                instance = Workflow.CreateInstance(param, null, null, onIdle, onVisualTracking);
            }
            ReadOnly = true;
            if (!VisualTracking && Minimize) GenericTools.minimize(GenericTools.mainWindow);
            instance.Run();
        }
        private void showVariables(IDictionary<string, ValueType> Variables)
        {
            GenericTools.RunUI(() =>
            {
                var form = new showVariables();
                form.variables = new System.Collections.ObjectModel.ObservableCollection<variable>();
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
            ModelService modelService = wfDesigner.Context.Services.GetService<ModelService>();

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
                        renameVariable(SelectedVariableName, variableName);
                        //DesignerView.ToggleVariableDesignerCommand.Execute(null);
                        //} else if (model.ItemType.BaseType == typeof(KeyedCollection<string, DynamicActivityProperty>))
                    }
                    else if (model.ItemType == typeof(DynamicActivityProperty))
                    {
                        if (e.ModelChangeInfo.PropertyName != "Name") return;
#pragma warning disable 0618
                        ModelProperty property = e.PropertiesChanged.ElementAt<ModelProperty>(0);
#pragma warning restore 0618
                        string variableName = property.ComputedValue.ToString();
                        renameVariable(SelectedVariableName, variableName);
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
        void renameVariable(string variableName, string newName)
        {
            if (string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(newName)) return;
            //if (selectedActivity == null) return;
            foreach (ModelItem item in this.GetWorkflowActivities(null))
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
                                str2 = str2 + newName;
                            }
                            else
                            {
                                str2 = str2 + str3;
                            }
                        }
                        item.Properties["ExpressionText"].SetValue(str2);
                    }
                }
            }
        }

        public void onUncomment(object sender, RoutedEventArgs e)
        {
            var thisselection = selection;
            var comment = selectedActivity;
            var currentSequence = selectedActivity.Properties["Body"].Value;
            var newSequence = GetActivitiesScope(selectedActivity.Parent.Parent);
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

                FlowStep step = new FlowStep();
                step.Action = new Sequence();
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
            }
        }
        private void onComment(object sender, RoutedEventArgs e)
        {
            var thisselection = selection;
            //var movethis = selectedActivity;
            var lastSequence = GetActivitiesScope(selectedActivity.Parent);
            if (lastSequence == null) lastSequence = GetActivitiesScope(selectedActivity);
            ModelItemCollection Activities = null;
            if (lastSequence.Properties["Activities"] != null)
            {
                Activities = lastSequence.Properties["Activities"].Collection;
            }
            else
            {
                Activities = lastSequence.Properties["Nodes"].Collection;
            }

            if (thisselection.SelectionCount > 1)
            {
                if (lastSequence.Properties["Nodes"] != null) return;
                var co = new Activities.CommentOut();
                co.Body = new Sequence();
                addActivity(co);
                var newActivities = selectedActivity.Properties["Body"].Value.Properties["Activities"].Collection;
                foreach (var sel in thisselection.SelectedObjects)
                {
                    Activities.Remove(sel);
                    var index = newActivities.Count;
                    Log.Debug("insert at " + index);
                    newActivities.Insert(0, sel);
                    //newActivities.Add(sel);
                }
            }
            else
            {
                var parentparent = thisselection.PrimarySelection.Parent.Parent;
                var parent = thisselection.PrimarySelection.Parent;

                if (parentparent == lastSequence)
                {
                    var co = new Activities.CommentOut();
                    addActivity(co);
                    Activities.Remove(thisselection.PrimarySelection);
                    selectedActivity.Properties["Body"].SetValue(thisselection.PrimarySelection);
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
                                selectedActivity = parentparent.Properties["Body"].SetValue(aa);
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
                            var comment = addActivity(co);
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


    }
}
