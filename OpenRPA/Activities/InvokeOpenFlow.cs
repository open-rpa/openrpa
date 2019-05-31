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
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class InvokeOpenFlows : NativeActivity
    {
        [RequiredArgument]
        public string workflow { get; set; }

        protected override async void Execute(NativeActivityContext context)
        {
            string id = null;
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
                        var test = new { value = value};
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
            }
            try
            {
                id = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
                context.CreateBookmark(id, new BookmarkCallback(OnBookmarkCallback));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
            try
            {
                if(!string.IsNullOrEmpty(id))
                {
                    var result = await global.webSocketClient.QueueMessage(workflow, _payload, id);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            context.RemoveBookmark(bookmark.Name);
            var payload = JObject.Parse(obj.ToString());
            List<string> keys = payload.Properties().Select(p => p.Name).ToList();
            foreach (var key in keys)
            {
                var myVar = context.DataContext.GetProperties().Find(key, true);
                if (myVar != null)
                {
                    //var myValue = myVar.GetValue(context.DataContext);
                    myVar.SetValue(context.DataContext, payload[key].ToString());
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
    }
}