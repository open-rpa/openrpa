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

namespace OpenRPA.OpenFlowDB
{
    [System.ComponentModel.Designer(typeof(AssignOpenFlowDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.assignopenflow.png")]
    [LocalizedToolboxTooltip("activity_assignopenflow_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_assignopenflow", typeof(Resources.strings))]
    public class AssignOpenFlow : NativeActivity
    {
        [RequiredArgument]
        public string targetid { get; set; }
        [RequiredArgument]
        public string workflowid { get; set; }
        public InArgument<bool> InitialRun { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_waitforcompleted", typeof(Resources.strings)), LocalizedDescription("activity_ignoreerrors_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForCompleted { get; set; } = true;
        protected override void Execute(NativeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            bool waitforcompleted = WaitForCompleted.Get(context);
            string bookmarkname = null;
            bool initialrun = InitialRun.Get(context);
            IDictionary<string, object> _payload = new System.Dynamic.ExpandoObject();
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
                        _payload[v.DisplayName] = value;
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    _payload[v.DisplayName] = value;
                }
            }
            try
            {
                bookmarkname = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
                if (waitforcompleted) context.CreateBookmark(bookmarkname, new BookmarkCallback(OnBookmarkCallback));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
            try
            {
                if (!string.IsNullOrEmpty(bookmarkname))
                {
                    var result = global.webSocketClient.CreateWorkflowInstance(workflowid, global.webSocketClient.user._id, targetid, _payload, initialrun, bookmarkname);
                    result.Wait(5000);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            // context.RemoveBookmark(bookmark.Name);
            bool waitforcompleted = WaitForCompleted.Get(context);
            if (!waitforcompleted) return;
            var _msg = JObject.Parse(obj.ToString());
            JObject payload = _msg; // Backward compatible with older version of openflow
            if (_msg.ContainsKey("payload")) payload = _msg.Value<JObject>("payload");
            var state = _msg["state"].ToString();
            if (!string.IsNullOrEmpty(state))
            {
                if (state == "idle")
                {
                    Log.Output("Workflow out node set to idle, so also going idle again.");
                    context.CreateBookmark(bookmark.Name, new BookmarkCallback(OnBookmarkCallback));
                    return;
                }
                else if (state == "failed")
                {
                    var message = "Invoke OpenFlow Workflow failed";
                    if (_msg.ContainsKey("error")) message = _msg["error"].ToString();
                    if (_msg.ContainsKey("_error")) message = _msg["_error"].ToString();
                    if (payload.ContainsKey("error")) message = payload["error"].ToString();
                    if (payload.ContainsKey("_error")) message = payload["_error"].ToString();
                    if (string.IsNullOrEmpty(message)) message = "Invoke OpenFlow Workflow failed";
                    throw new Exception(message);
                }
            }

            List<string> keys = payload.Properties().Select(p => p.Name).ToList();
            foreach (var key in keys)
            {
                var myVar = context.DataContext.GetProperties().Find(key, true);
                if (myVar != null)
                {
                    if(myVar.PropertyType.Name == "JArray")
                    {
                        var json = payload[key].ToString();
                        var jobj = JArray.Parse(json);
                        myVar.SetValue(context.DataContext, jobj);
                    } else if (myVar.PropertyType.Name == "JObject")
                    {
                        var json = payload[key].ToString();
                        var jobj = JObject.Parse(json);
                        myVar.SetValue(context.DataContext, jobj);
                    }
                    else
                    {
                        myVar.SetValue(context.DataContext, payload[key].ToString());
                    }
                    //var myValue = myVar.GetValue(context.DataContext);

                }
                else
                {
                    Log.Debug("Recived property " + key + " but no variable exists to save the value in " + payload[key]);
                }
                //action.setvariable(key, payload[key]);

            }
        }
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
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