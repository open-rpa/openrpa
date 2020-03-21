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
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class InvokeOpenRPA : NativeActivity
    {
        [RequiredArgument]
        public string workflow { get; set; }
        public InArgument<bool> WaitForCompleted { get; set; } = true;
        protected override void Execute(NativeActivityContext context)
        {
            bool waitforcompleted = WaitForCompleted.Get(context);
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            // IDictionary<string, object> _payload = new System.Dynamic.ExpandoObject();
            var param = new Dictionary<string, object>();
            var vars = context.DataContext.GetProperties();
            foreach (dynamic v in vars)
            {
                var value = v.GetValue(context.DataContext);
                if (value != null)
                {
                    //_payload.Add(v.DisplayName, value);
                    try
                    {
                        var test = new { value = value };
                        if (value.GetType() == typeof(System.Data.DataTable)) continue;
                        if (value.GetType() == typeof(System.Data.DataView)) continue;
                        if (value.GetType() == typeof(System.Data.DataRowView)) continue;
                        //
                        var asjson = JObject.FromObject(test);
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
            try
            {
                var workflow = MainWindow.instance.GetWorkflowByIDOrRelativeFilename(this.workflow);
                IWorkflowInstance instance = null;
                Views.WFDesigner designer = null;
                GenericTools.RunUI(() =>
                {
                    designer = MainWindow.instance.GetWorkflowDesignerByIDOrRelativeFilename(this.workflow) as Views.WFDesigner;
                    if (designer != null)
                    {
                        designer.BreakpointLocations = null;
                        instance = workflow.CreateInstance(param, null, null, designer.OnIdle, designer.OnVisualTracking);
                    }
                    else
                    {
                        instance = workflow.CreateInstance(param, null, null, MainWindow.instance.IdleOrComplete,  null);
                    }
                    instance.caller = WorkflowInstanceId;
                });
                Log.Verbose("InvokeOpenRPA: Run Instance ID " + instance._id);
                if (waitforcompleted) context.CreateBookmark(instance._id, new BookmarkCallback(OnBookmarkCallback));
                //GenericTools.RunUI(() =>
                //{
                //    if (designer != null)
                //    {
                //        designer.Run(MainWindow.instance.VisualTracking, MainWindow.instance.SlowMotion, instance);
                //    }
                //    else
                //    {
                //        instance.Run();
                //    }
                //});
                if (designer != null)
                {
                    designer.Run(designer.VisualTracking, designer.SlowMotion, instance);
                }
                else
                {
                    instance.Run();
                }
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
                // keep bookmark, incase workflow dies, and need to pickup data after being restarted 
                // context.RemoveBookmark(bookmark.Name);
                var instance = obj as WorkflowInstance;
                if (instance == null) throw new Exception("Bookmark returned a non WorkflowInstance");
                if (instance.Exception != null) throw instance.Exception;
                if (instance.hasError) throw new Exception(instance.errormessage);
                foreach (var prop in instance.Parameters)
                {
                    var myVar = context.DataContext.GetProperties().Find(prop.Key, true);
                    if (myVar != null)
                    {
                        //var myValue = myVar.GetValue(context.DataContext);
                        myVar.SetValue(context.DataContext, prop.Value);
                    }
                    else
                    {
                        Log.Debug("Recived property " + prop.Key + " but no variable exists to save the value in " + prop.Value);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }

            //var payload = JObject.Parse(obj.ToString());
            //List<string> keys = payload.Properties().Select(p => p.Name).ToList();
            //foreach (var key in keys)
            //{
            //    var myVar = context.DataContext.GetProperties().Find(key, true);
            //    if (myVar != null)
            //    {
            //        //var myValue = myVar.GetValue(context.DataContext);
            //        myVar.SetValue(context.DataContext, payload[key].ToString());
            //    }
            //    else
            //    {
            //        Log.Debug("Recived property " + key + " but no variable exists to save the value in " + payload[key]);
            //    }
            //    //action.setvariable(key, payload[key]);

            //}
        }
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}