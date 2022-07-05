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
    [System.ComponentModel.Designer(typeof(InvokeRemoteOpenRPADesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.invokeremoterpaworkflow.png")]
    [LocalizedToolboxTooltip("activity_invokeremoteopenrpa_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_invokeremoteopenrpa", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_invokeremoteopenrpa_helpurl", typeof(Resources.strings))]
    public class InvokeRemoteOpenRPA : NativeActivity
    {
        public InvokeRemoteOpenRPA()
        {
            var builder = new System.Activities.Presentation.Metadata.AttributeTableBuilder();
            builder.AddCustomAttributes(typeof(InvokeRemoteOpenRPA), "Arguments",
                new EditorAttribute(typeof(OpenRPA.Interfaces.Activities.ArgumentCollectionEditor),
                typeof(System.Activities.Presentation.PropertyEditing.PropertyValueEditor)));
            System.Activities.Presentation.Metadata.MetadataStore.AddAttributeTable(builder.CreateTable());
            Arguments = new Dictionary<string, Argument>();
        }
        [RequiredArgument, LocalizedDisplayName("activity_workflow", typeof(Resources.strings)), LocalizedDescription("activity_workflow_help", typeof(Resources.strings))]
        public InArgument<string> workflow { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_target", typeof(Resources.strings)), LocalizedDescription("activity_target_help", typeof(Resources.strings))]
        public InArgument<string> target { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_waitforcompleted", typeof(Resources.strings)), LocalizedDescription("activity_waitforcompleted_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForCompleted { get; set; } = true;
        public InArgument<int> Expiration { get; set; }
        [LocalizedDisplayName("activity_arguments", typeof(Resources.strings)), LocalizedDescription("activity_arguments_help", typeof(Resources.strings)), Browsable(true), Category("Input")]
        public Dictionary<string, Argument> Arguments { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_killifrunning", typeof(Resources.strings)), LocalizedDescription("activity_killifrunning_help", typeof(Resources.strings))]
        public InArgument<bool> KillIfRunning { get; set; } = true;
        protected override void Execute(NativeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            var killifrunning = KillIfRunning.Get(context);
            string bookmarkname = null;
            bool waitforcompleted = WaitForCompleted.Get(context);
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
                                if (value != null) _payload[v.DisplayName] = ((System.Data.DataTable)value).ToJArray();
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
                    }
                    else { _payload[a.Key] = null; }
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
                    int expiration = Expiration.Get(context);
                    IDictionary<string, object> _robotcommand = new System.Dynamic.ExpandoObject();
                    _robotcommand["workflowid"] = workflow.Get(context);
                    _robotcommand["killexisting"] = killifrunning;
                    _robotcommand["command"] = "invoke";
                    _robotcommand.Add("data", _payload);
                    var result = global.webSocketClient.QueueMessage(target.Get(context), _robotcommand, RobotInstance.instance.robotqueue, bookmarkname, expiration);
                    result.Wait(5000);
                }
            }
            catch (Exception ex)
            {
                var i = WorkflowInstance.Instances.Where(x => x.InstanceId == WorkflowInstanceId).FirstOrDefault();
                if (i != null)
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
            // keep bookmark, incase workflow dies, and need to pickup more data when started again
            // context.RemoveBookmark(bookmark.Name);
            var command = Newtonsoft.Json.JsonConvert.DeserializeObject<Interfaces.mq.RobotCommand>(obj.ToString());
            if (command.data == null) return;
            if (command.command == "invokefailed" || command.command == "error")
            {
                if (string.IsNullOrEmpty(command.data.ToString())) throw new Exception("Invoke failed");
                Exception ex = null;
                try
                {
                    ex = Newtonsoft.Json.JsonConvert.DeserializeObject<Exception>(command.data.ToString());
                }
                catch (Exception)
                {
                }
                if (ex != null) throw ex;
                throw new Exception(command.data.ToString());
            }
            if (command.command == "timeout")
            {
                throw new Exception("request timed out, no robot picked up the message in a timely fashion");
            }
            if (string.IsNullOrEmpty(command.data.ToString())) return;
            var payload = JObject.Parse(command.data.ToString());
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
                            JToken t = payload[a.Key];
                            var testtest = t.Value<string>();
                            System.Reflection.MethodInfo method = typeof(JToken).GetMethod(nameof(JToken.Value)); // typeof(JToken).GetMethod(nameof(JToken.Value));
                            System.Reflection.MethodInfo generic = method.MakeGenericMethod(Arguments[a.Key].ArgumentType);
                            var value = generic.Invoke(t, new object[] { });
                            Arguments[a.Key].Set(context, value);
                        }
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