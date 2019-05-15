using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.Services;
using System.Activities.Presentation.Toolbox;
using System.Activities.Presentation.View;
using System.Activities.Statements;
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
    /// Interaction logic for WFDesigner.xaml
    /// </summary>
    public partial class WFDesigner : UserControl, System.ComponentModel.INotifyPropertyChanged
    {
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
        private ToolboxControl _wfToolbox;
        public WFDesigner()
        {
            InitializeComponent();
            InitializeActivitiesToolbox();
        }
        public readonly ClosableTab tab;
        public WFDesigner(ClosableTab tab, Workflow workflow, Type[] extratypes)
        {
            this.tab = tab;
            InitializeComponent();
            InitializeActivitiesToolbox();
            Workflow = workflow;
            Workflow.idleOrComplete += onIdleOrComplete;
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

            if (!string.IsNullOrEmpty(workflow.Xaml))
            {
                wfDesigner.Text = workflow.Xaml;
                wfDesigner.Load();
                //wfDesigner.Load(workflow.Filename);
            }
            else
            {
                Activity wf = new System.Activities.Statements.Sequence { };
                var ab = new ActivityBuilder();
                ab.Name = workflow.name;
                ab.Implementation = wf;
                AddVBNamespaceSettings(ab, typeof(Action),
                    typeof(Microsoft.VisualBasic.Collection),
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
            HasChanged = false;
            wfDesigner.ModelChanged += (sender, e) =>
            {
                HasChanged = true;
                onChanged?.Invoke(this);
            };

            WfDesignerBorder.Child = wfDesigner.View;
            WfPropertyBorder.Child = wfDesigner.PropertyInspectorView;

            OutputMessages = MainWindow.tracing.OutputMessages;
            TraceMessages = MainWindow.tracing.TraceMessages;


            var modelItem = wfDesigner.Context.Services.GetService<ModelService>().Root;
            workflow.name = modelItem.GetValue<string>("Name");
            tab.Title = workflow.name;

            wfDesigner.Context.Items.Subscribe<Selection>(new SubscribeContextCallback<Selection>(SelectionChanged));

            WeakEventManager<System.ComponentModel.INotifyPropertyChanged, System.ComponentModel.PropertyChangedEventArgs>.
                AddHandler(MainWindow.tracing, "PropertyChanged", traceOnPropertyChanged);
        }

        private void traceOnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "OutputMessages")
                OutputMessages = MainWindow.tracing.OutputMessages;
            if (e.PropertyName == "TraceMessages")
                TraceMessages = MainWindow.tracing.TraceMessages;
        }
        private string _OutputMessages = "";
        public string OutputMessages
        {
            get { return _OutputMessages; }
            set
            {
                _OutputMessages = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("OutputMessages"));
            }
        }
        private string _TraceMessages = "";
        public string TraceMessages
        {
            get { return _TraceMessages; }
            set
            {
                _TraceMessages = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("TraceMessages"));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void onIdleOrComplete(Workflow workflow, WorkflowInstance instance)
        {
            onChanged?.Invoke(this);
        }

        public async Task Save()
        {
            wfDesigner.Flush();
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

        private void SelectionChanged(Selection item)
        {
            var selection = item;
            selectedActivity = selection.PrimarySelection;
            if (selectedActivity == null) return;
            //SelectedVariableName = selectedActivity.GetCurrentValue().ToString();
        }
        private void InitializeActivitiesToolbox()
        {
            try
            {
                _wfToolbox = new ToolboxControl();

                // get all loaded assemblies
                IEnumerable<System.Reflection.Assembly> appAssemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.GetName().Name)
                    .Where(a => a.GetName().Name != "System.ServiceModel.Activities");

                // check if assemblies contain activities
                int activitiesCount = 0;
                foreach (System.Reflection.Assembly activityLibrary in appAssemblies)
                {
                    try
                    {

                        string[] excludeActivities = { "AddValidationError", "AndAlso", "AssertValidation", "CreateBookmarkScope", "DeleteBookmarkScope", "DynamicActivity",
                            "CancellationScope", "CompensableActivity", "Compensate", "Confirm", "GetChildSubtree", "GetParentChain", "GetWorkflowTree", "Add`3",  "And`3", "As`2", "Cast`2",
                        "Cast`2", "ArgumentValue`1", "ArrayItemReference`1", "ArrayItemValue`1", "Assign`1", "Constraint`1","CSharpReference`1", "CSharpValue`1", "DelegateArgumentReference`1",
                            "DelegateArgumentValue`1", "Divide`3", "DynamicActivity`1", "Equal`3", "FieldReference`2", "FieldValue`2", "ForEach`1", "InvokeAction", "InvokeDelegate",
                        "ArgumentReference`1", "VariableReference`1", "VariableValue`1", "VisualBasicReference`1", "VisualBasicValue`1", "InvokeMethod`1" };

                        var wfToolboxCategory = new ToolboxCategory(activityLibrary.GetName().Name);
                        var actvities = from
                                            activityType in activityLibrary.GetExportedTypes()
                                        where
                                            (activityType.IsSubclassOf(typeof(Activity))
                                            || activityType.IsSubclassOf(typeof(NativeActivity))
                                            || activityType.IsSubclassOf(typeof(DynamicActivity))
                                            || activityType.IsSubclassOf(typeof(ActivityWithResult))
                                            || activityType.IsSubclassOf(typeof(AsyncCodeActivity))
                                            || activityType.IsSubclassOf(typeof(CodeActivity))
                                            || activityType.GetInterfaces().Contains(typeof(IActivityTemplateFactory))
                                            )
                                            && activityType.IsVisible
                                            && activityType.IsPublic
                                            && !activityType.IsNested
                                            && !activityType.IsAbstract
                                            && (activityType.GetConstructor(Type.EmptyTypes) != null)
                                            && !excludeActivities.Contains(activityType.Name)
                                            && !activityType.Name.StartsWith("InvokeAction`")
                                            && !activityType.Name.StartsWith("InvokeFunc`")
                                            && !activityType.Name.StartsWith("Subtract`")
                                            && !activityType.Name.StartsWith("GreaterThan`")
                                            && !activityType.Name.StartsWith("GreaterThanOrEqual`")
                                            && !activityType.Name.StartsWith("LessThan`")
                                            && !activityType.Name.StartsWith("LessThanOrEqual`")
                                            && !activityType.Name.StartsWith("Literal`")
                                            && !activityType.Name.StartsWith("MultidimensionalArrayItemReference`")
                                            && !activityType.Name.StartsWith("Multiply`")
                                            && !activityType.Name.StartsWith("New`")
                                            && !activityType.Name.StartsWith("NewArray`")
                                            && !activityType.Name.StartsWith("Or`")
                                            && !activityType.Name.StartsWith("OrElse")
                                            && !activityType.Name.EndsWith("`2")
                                            && !activityType.Name.EndsWith("`3")
                                        orderby
                                            activityType.Name
                                        select
                                            new ToolboxItemWrapper(activityType, activityType.Name.Replace("`1", ""));
                        actvities.ToList().ForEach(wfToolboxCategory.Add);

                        if (wfToolboxCategory.Tools.Count > 0)
                        {
                            _wfToolbox.Categories.Add(wfToolboxCategory);
                            activitiesCount += wfToolboxCategory.Tools.Count;
                            //if(wfToolboxCategory.CategoryName == "System.Activities")
                            //{
                            //    wfToolboxCategory.Tools.Add(new ToolboxItemWrapper(typeof(System.Activities.Core.Presentation.Factories.ForEachWithBodyFactory<>), "ForEach"));
                            //    wfToolboxCategory.Tools.Add(new ToolboxItemWrapper(typeof(System.Activities.Core.Presentation.Factories.ParallelForEachWithBodyFactory<>), "ParallelForEach"));
                            //}
                        }
                    }
                    catch (Exception)
                    {
                    }

                }
                //fixed ForEach
                //_wfToolbox.Categories.Add(
                //       new ToolboxCategory
                //       {
                //           CategoryName = "CustomForEach",
                //           Tools = {
                //                new ToolboxItemWrapper(typeof(System.Activities.Core.Presentation.Factories.ForEachWithBodyFactory<>)),
                //                new ToolboxItemWrapper(typeof(System.Activities.Core.Presentation.Factories.ParallelForEachWithBodyFactory<>))
                //           }
                //       }
                //);

                //LabelStatusBar.Content = String.Format("Loaded Activities: {0}", activitiesCount.ToString());
                WfToolboxBorder.Child = _wfToolbox;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show("InitializeActivitiesToolbox: " + ex.Message);
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
        }
    }
}
