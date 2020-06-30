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
    [System.ComponentModel.Designer(typeof(InvokeOpenFlowDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.invokezeniverseworkflow.png")]
    [LocalizedToolboxTooltip("activity_invokeopenflow_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_invokeopenflow", typeof(Resources.strings))]
    public class InvokeOpenFlow : NativeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_workflow", typeof(Resources.strings)), LocalizedDescription("activity_workflow_help", typeof(Resources.strings))]
        public string workflow { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_waitforcompleted", typeof(Resources.strings)), LocalizedDescription("activity_ignoreerrors_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForCompleted { get; set; } = true;
        [Category("Input")]
        public Dictionary<string, Argument> Arguments { get; set; } = new Dictionary<string, Argument>();
        protected override async void Execute(NativeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            bool waitforcompleted = WaitForCompleted.Get(context);
            string bookmarkname = null;
            IDictionary<string, object> _payload = new System.Dynamic.ExpandoObject();
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
                            var test = new { value = value };

                            if (value.GetType() == typeof(System.Data.DataView)) continue;
                            if (value.GetType() == typeof(System.Data.DataRowView)) continue;

                            if (value.GetType() == typeof(System.Data.DataTable))
                            {
                                if(value != null) _payload[v.DisplayName] = ((System.Data.DataTable)value).ToJArray();
                            } 
                            else
                            {
                                var asjson = JObject.FromObject(test);
                                _payload[v.DisplayName] = value;

                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        _payload[v.DisplayName] = null;
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
                        if (value.GetType() == typeof(System.Data.DataView)) continue;
                        if (value.GetType() == typeof(System.Data.DataRowView)) continue;

                        if (value.GetType() == typeof(System.Data.DataTable))
                        {
                            if (value != null) _payload[a.Key] = ((System.Data.DataTable)value).ToJArray();
                        }
                        else
                        {
                            _payload[a.Key] = a.Value;
                        }
                    } else { _payload[a.Key] = null; }

                }
            }
            try
                {
                bookmarkname = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
                if(waitforcompleted) context.CreateBookmark(bookmarkname, new BookmarkCallback(OnBookmarkCallback));
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
                    var result = await global.webSocketClient.QueueMessage(workflow, _payload, RobotInstance.instance.robotqueue, bookmarkname);
                }
            }
            catch (Exception ex)
            {
                var i = WorkflowInstance.Instances.Where(x => x.InstanceId == WorkflowInstanceId).FirstOrDefault();
                if(i != null)
                {
                    i.Abort(ex.Message);
                }
                //context.RemoveBookmark(bookmarkname);
                Log.Error(ex.ToString());
            }
        }
        void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            bool waitforcompleted = WaitForCompleted.Get(context);
            if (!waitforcompleted) return;
            // context.RemoveBookmark(bookmark.Name);
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
            if (Arguments == null || Arguments.Count == 0)
            {
                foreach (var key in keys)
                {
                    var myVar = context.DataContext.GetProperties().Find(key, true);
                    if (myVar != null)
                    {
                        if (myVar.PropertyType.Name == "DataTable")
                        {
                            var json = payload[key].ToString();
                            if (!string.IsNullOrEmpty(json))
                            {
                                var jarray = JArray.Parse(json);
                                myVar.SetValue(context.DataContext, jarray.ToDataTable());
                            } 
                            else
                            {
                                myVar.SetValue(context.DataContext, null);
                            }
                        }
                        else if (myVar.PropertyType.Name == "JArray")
                        {
                            var json = payload[key].ToString();
                            var jobj = JArray.Parse(json);
                            myVar.SetValue(context.DataContext, jobj);
                        }
                        else if (myVar.PropertyType.Name == "JObject")
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
                }
            }
            else
            {
                Dictionary<string, object> arguments = (from argument in Arguments
                                                        where argument.Value.Direction != ArgumentDirection.In
                                                        select argument).ToDictionary((KeyValuePair<string, Argument> argument) => argument.Key, (KeyValuePair<string, Argument> argument) => argument.Value.Get(context));
                foreach (var a in arguments)
                {
                    if (keys.Contains(a.Key))
                    {
                        if (Arguments[a.Key].ArgumentType == typeof(System.Data.DataTable))
                        {
                            try
                            {
                                var json = payload[a.Key].ToString();
                                if (!string.IsNullOrEmpty(json))
                                {
                                    var jarray = JArray.Parse(json);
                                    Arguments[a.Key].Set(context, jarray.ToDataTable());
                                }
                                else
                                {
                                    Arguments[a.Key].Set(context, null);
                                }
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                        else 
                        {
                            var method = typeof(JObject).GetMethod(nameof(JToken.Value));
                            var generic = method.MakeGenericMethod(Arguments[a.Key].ArgumentType);
                            var t = payload[a.Key];
                        }
                    } 
                    else
                    {
                        try
                        {
                            if (Arguments[a.Key].ArgumentType.IsValueType)
                            {
                                Arguments[a.Key].Set(context, Activator.CreateInstance(Arguments[a.Key].ArgumentType));
                            } else
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