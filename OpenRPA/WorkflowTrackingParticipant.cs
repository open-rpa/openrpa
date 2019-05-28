using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Tracking;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    class WorkflowTrackingParticipant : TrackingParticipant
    {
        public delegate void VisualTrackingHandler(WorkflowInstance Instance, string ActivityId, string State);
        public event VisualTrackingHandler OnVisualTracking;
        public WorkflowTrackingParticipant()
        {
            TrackingProfile = new TrackingProfile()
            {
                Name = "CustomTrackingProfile",
                Queries =
                        {
                            new CustomTrackingQuery()
                            {
                                Name = "*",
                                ActivityName = "*"
                            },
                            new WorkflowInstanceQuery()
                            {
                                // Limit workflow instance tracking records for started and completed workflow states
                                // States = { WorkflowInstanceStates.Started, WorkflowInstanceStates.Completed },
                                // States = { "*" }
                            },
                            new ActivityStateQuery()
                            {
                                // Subscribe for track records from all activities for all states
                                ActivityName = "*",
                                States = { "*" },
                                // Extract workflow variables and arguments as a part of the activity tracking record
                                // VariableName = "*" allows for extraction of all variables in the scope
                                // of the activity
                                Variables = { "*" },
                                Arguments = { "*" }
                            },
                            new ActivityScheduledQuery()
                            {
                                ActivityName = "*",
                                ChildActivityName = "*",
                            }
                        }
            };
        }
        protected override void Track(TrackingRecord trackRecord, TimeSpan timeStamp)
        {
            string State = "unknown";
            Guid InstanceId = trackRecord.InstanceId;
            ActivityStateRecord activityStateRecord = trackRecord as ActivityStateRecord;
            ActivityScheduledRecord activityScheduledRecord = trackRecord as ActivityScheduledRecord;
            //if (activityStateRecord != null || activityScheduledRecord != null)
            if (activityScheduledRecord != null)
            {
                string ActivityId = null;
                string ChildActivityId = null;
                if (activityStateRecord != null)
                {
                    ActivityId = activityStateRecord.Activity.Id;
                    State = activityStateRecord.State.ToLower();
                }
                if (activityScheduledRecord != null)
                {
                    State = "Scheduled";
                    if (activityScheduledRecord.Activity != null) ActivityId = activityScheduledRecord.Activity.Id;
                    if (activityScheduledRecord.Child != null) ChildActivityId = activityScheduledRecord.Child.Id;
                }
                if (string.IsNullOrEmpty(ActivityId)) return;
                
                var Instance = WorkflowInstance.Instances.Where(x => x.InstanceId == InstanceId.ToString()).FirstOrDefault();
                if (Instance == null || Instance.wfApp == null) return;
                var wfApp = Instance.wfApp;
                if (Instance.Variables == null) Instance.Variables = new Dictionary<string, ValueType>();
                if (activityStateRecord != null)
                {
                    foreach (var v in Instance.Variables.ToList())
                    {
                        if (!activityStateRecord.Variables.ContainsKey(v.Key)) Instance.Variables.Remove(v.Key);
                    }
                    foreach (var v in activityStateRecord.Variables)
                    {
                        if (Instance.Variables.ContainsKey(v.Key))
                        {
                            Instance.Variables[v.Key].value = v.Value;
                        }
                    }
                }
                var executor = typeof(System.Activities.Hosting.WorkflowInstance).GetField("executor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(wfApp);
                var instanceMapField = executor.GetType().GetField("instanceMap", BindingFlags.NonPublic | BindingFlags.Instance);

                // get SerializedProgramMapping to have InstanceMap get filled, needed by SerializedProgramMapping
                var SerializedProgramMapping = executor.GetType().GetProperty("SerializedProgramMapping", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
                ActivityInstance activityInstance = executor.GetType().GetField("rootInstance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor) as ActivityInstance;
                
                // Sometimes we can find the ActivityInstance in rootInstance
                ActivityInstance result = findActivityInstance(executor, activityInstance, ActivityId);

                // But more often, we find it in InstanceMapping
                var instanceMap = instanceMapField.GetValue(executor);
                if (instanceMap != null && result == null)
                {
                    var _list = SerializedProgramMapping.GetType().GetProperty("InstanceMapping", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SerializedProgramMapping);
                    foreach (System.Collections.DictionaryEntry kvp in (System.Collections.IDictionary)_list)
                    {
                        var a = kvp.Key as Activity;
                        if (a == null) continue;
                        if (result == null && a.Id == ActivityId)
                        {
                            result = findActivityInstance(kvp.Value, ActivityId);
                        }
                    }
                }
                if (result != null)
                {
                    WorkflowDataContext context = null;
                    var cs = typeof(WorkflowDataContext).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
                    ConstructorInfo c = cs.First();

                    try
                    {
                        object o = c.Invoke(new Object[] { executor, result, true });
                        context = o as WorkflowDataContext;
                        var vars = context.GetProperties();
                        foreach (dynamic v in vars)
                        {
                            var value = v.GetValue(context);
                            if (Instance.Variables.ContainsKey(v.DisplayName))
                            {
                                Instance.Variables[v.DisplayName] = new ValueType(v.PropertyType, value);
                            }
                            else
                            {
                                Instance.Variables.Add(v.DisplayName, new ValueType(v.PropertyType, value));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.Message);
                    }
                }
                
                OnVisualTracking?.Invoke(Instance, (ChildActivityId==null?ActivityId: ChildActivityId), State);
            } else
            {
                // Log.Debug(trackRecord.ToString());
            }
        }
        private ActivityInstance findActivityInstance(object executor, ActivityInstance activityInstance, string ActivityId)
        {
            object list = null;
            int list_count = 0;
            try
            {
                if (activityInstance.Activity.Id == ActivityId) return activityInstance;
                list = typeof(ActivityInstance).GetField("childList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(activityInstance);
                if (list == null)
                {
                    return null;
                }
                list_count = (int)list.GetType().GetProperties().Where(x => x.Name == "Count").First().GetValue(list);
                if (list_count == 0)
                {
                    return null;
                }
                for (var i = 0; i < list_count; i++)
                {
                    var e = list.GetType().GetProperties().Where(x => x.Name == "Item").First().GetValue(list, new Object[] { i }) as ActivityInstance;
                    if (e.Activity.Id == ActivityId) return e;
                    var res = findActivityInstance(e, ActivityId);
                    if (res != null) return res;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
        private ActivityInstance findActivityInstance(object list, string ActivityId)
        {
            //int list_count = 0;
            try
            {
                //log(String.Format("found: {0}", activityInstance.Id));
                if (list == null)
                {
                    return null;
                }
                int list_count = 0;
                var pcount = list.GetType().GetProperties().Where(x => x.Name == "Count").FirstOrDefault();
                if (pcount != null)
                {
                    list_count = (int)pcount.GetValue(list);
                }
                if (list_count == 0)
                {
                    return null;
                }
                for (var i = 0; i < list_count; i++)
                {
                    var e = list.GetType().GetProperties().Where(x => x.Name == "Item").First().GetValue(list, new Object[] { i }) as ActivityInstance;
                    if (e != null)
                    {
                        if (e.Activity.Id == ActivityId) return e;
                        var res = findActivityInstance(e, ActivityId);
                        if (res != null) return res;
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
    }
}