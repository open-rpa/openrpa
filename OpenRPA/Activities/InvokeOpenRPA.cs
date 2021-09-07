using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(InvokeOpenRPADesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.invokerpaworkflow.png")]
    [LocalizedToolboxTooltip("activity_invokeopenrpa_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_invokeopenrpa", typeof(Resources.strings))]
    public class InvokeOpenRPA : NativeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_workflow", typeof(Resources.strings)), LocalizedDescription("activity_workflow_help", typeof(Resources.strings))]
        public InArgument<string> workflow { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_waitforcompleted", typeof(Resources.strings)), LocalizedDescription("activity_waitforcompleted_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForCompleted { get; set; } = true;
        [RequiredArgument, LocalizedDisplayName("activity_killifrunning", typeof(Resources.strings)), LocalizedDescription("activity_killifrunning_help", typeof(Resources.strings))]
        public InArgument<bool> KillIfRunning { get; set; } = true;
        [Category("Input")]
        public Dictionary<string, Argument> Arguments { get; set; } = new Dictionary<string, Argument>();
        protected override void Execute(NativeActivityContext context)
        {
            bool waitforcompleted = WaitForCompleted.Get(context);
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            // IDictionary<string, object> _payload = new System.Dynamic.ExpandoObject();
            var param = new Dictionary<string, object>();
            var killifrunning = KillIfRunning.Get(context);
            if (Arguments == null || Arguments.Count == 0)
            {
                var vars = context.DataContext.GetProperties();
                foreach (dynamic v in vars)
                {
                    var value = v.GetValue(context.DataContext);
                    if (value != null)
                    {
                        //_payload.Add(v.DisplayName, value);
                        try
                        {
                            //if (value.GetType() == typeof(System.Data.DataView)) continue;
                            //if (value.GetType() == typeof(System.Data.DataRowView)) continue;
                            //if (value.GetType() == typeof(System.Data.DataTable))
                            //{
                            //    if (value != null) param[v.DisplayName] = ((System.Data.DataTable)value).ToJArray();
                            //}
                            //else
                            //{
                            //    var asjson = JObject.FromObject(test);
                            //    param[v.DisplayName] = value;
                            //}
                            //
                            //var test = new { value };
                            //var asjson = JObject.FromObject(test);
                            param[v.DisplayName] = value;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        param[v.DisplayName] = value;
                    }
                }
            }
            else
            {
                Dictionary<string, object> arguments = (from argument in Arguments
                                                        where argument.Value.Direction != ArgumentDirection.Out
                                                        select argument).ToDictionary((KeyValuePair<string, Argument> argument) => argument.Key, (KeyValuePair<string, Argument> argument) => argument.Value.Get(context));
                foreach (var a in arguments)
                {
                    var value = a.Value;
                    if (value != null)
                    {
                        //if (value.GetType() == typeof(System.Data.DataView)) continue;
                        //if (value.GetType() == typeof(System.Data.DataRowView)) continue;
                        //if (value.GetType() == typeof(System.Data.DataTable))
                        //{
                        //    if (value != null) param[a.Key] = ((System.Data.DataTable)value).ToJArray();
                        //}
                        //else
                        //{
                        //    param[a.Key] = a.Value;
                        //}
                        param[a.Key] = a.Value;
                    }
                    else
                    {
                        param[a.Key] = null;
                    }
                }
            }
            Exception error = null;
            try
            {
                var Instance = WorkflowInstance.Instances.Where(x => x.InstanceId == context.WorkflowInstanceId.ToString()).FirstOrDefault();

                // , string SpanId, string ParentSpanId
                var workflowid = this.workflow.Get(context);
                var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(workflowid);
                if (workflow == null) throw new ArgumentException("Failed locating workflow " + workflowid);

                if (killifrunning)
                    foreach (var i in global.OpenRPAClient.WorkflowInstances.ToList())
                    {
                        if (!i.isCompleted && i.Workflow._id == workflow._id)
                        {
                            i.Abort("Killed by KillIfRunning from " + Instance.Workflow.name);
                        }
                    }
                IWorkflowInstance instance = null;
                Views.WFDesigner designer = null;

                GenericTools.RunUI(() =>
                {
                    try
                    {
                        designer = RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(this.workflow.Get(context)) as Views.WFDesigner;
                        if (designer != null)
                        {
                            designer.BreakpointLocations = null;
                            instance = workflow.CreateInstance(param, null, null, designer.IdleOrComplete, designer.OnVisualTracking);
                        }
                        else
                        {
                            instance = workflow.CreateInstance(param, null, null, RobotInstance.instance.Window.IdleOrComplete, null);
                        }
                        instance.caller = WorkflowInstanceId;
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                });
                if (error != null) throw error;
                if (instance != null)
                {
                    Log.Verbose("InvokeOpenRPA: Run Instance ID " + instance._id);
                    if (waitforcompleted)
                    {
                        context.CreateBookmark(instance._id, new BookmarkCallback(OnBookmarkCallback));
                        if (instance.Bookmarks == null) instance.Bookmarks = new Dictionary<string, object>();
                        instance.Bookmarks.Add(instance._id, null);
                        //((WorkflowInstance)instance).wfApp.Persist();
                    }
                }
                GenericTools.RunUI(() =>
                {
                    if (designer != null & instance != null)
                    {
                        designer.Run(designer.VisualTracking, designer.SlowMotion, instance);
                    }
                    else if (instance != null)
                    {
                        instance.Run();
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            try
            {
                bool waitforcompleted = WaitForCompleted.Get(context);
                if (!waitforcompleted) return;
                // keep bookmark, incase workflow dies, and need to pickup data after being restarted 
                // context.RemoveBookmark(bookmark.Name);
                var instance = obj as WorkflowInstance;
                if (instance == null) throw new Exception("Bookmark returned a non WorkflowInstance");
                var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(this.workflow.Get(context));
                var name = "The invoked workflow failed with ";
                if (workflow != null && !string.IsNullOrEmpty(workflow.name)) name = workflow.name;
                if (workflow != null && !string.IsNullOrEmpty(workflow.ProjectAndName)) name = workflow.ProjectAndName;

                if (instance.Exception != null) throw new Exception(name + " failed with " + instance.Exception.Message, instance.Exception);
                if (instance.hasError) throw new Exception(name + " failed with " + instance.errormessage);

                if (Arguments == null || Arguments.Count == 0)
                {
                    foreach (var prop in instance.Parameters)
                    {
                        var myVar = context.DataContext.GetProperties().Find(prop.Key, true);
                        if (myVar != null)
                        {
                            myVar.SetValue(context.DataContext, prop.Value);
                            //if (myVar.PropertyType.Name == "DataTable")
                            //{
                            //    var json = prop.ToString();
                            //    if(!string.IsNullOrEmpty(json))
                            //    {
                            //        var jarray = JArray.Parse(json);
                            //        myVar.SetValue(context.DataContext, jarray.ToDataTable());
                            //    } 
                            //    else
                            //    {
                            //        myVar.SetValue(context.DataContext, null);
                            //    }
                            //}
                            //else
                            //{
                            //    //var myValue = myVar.GetValue(context.DataContext);
                            //    myVar.SetValue(context.DataContext, prop.Value);
                            //}
                        }
                        else
                        {
                            Log.Debug("Recived property " + prop.Key + " but no variable exists to save the value in " + prop.Value);
                        }
                    }
                }
                else
                {
                    Dictionary<string, object> arguments = (from argument in Arguments
                                                            where argument.Value.Direction != ArgumentDirection.In
                                                            select argument).ToDictionary((KeyValuePair<string, Argument> argument) => argument.Key, (KeyValuePair<string, Argument> argument) => argument.Value.Get(context));
                    foreach (var a in arguments)
                    {
                        if (instance.Parameters.ContainsKey(a.Key))
                        {
                            Arguments[a.Key].Set(context, instance.Parameters[a.Key]);

                        }
                        else
                        {
                            try
                            {
                                if (Arguments[a.Key].ArgumentType.IsValueType)
                                {
                                    Arguments[a.Key].Set(context, Activator.CreateInstance(Arguments[a.Key].ArgumentType));
                                }
                                else
                                {
                                    Arguments[a.Key].Set(context, null);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error setting " + a.Key + ": " + ex.Message);
                            }
                        }

                    }

                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}