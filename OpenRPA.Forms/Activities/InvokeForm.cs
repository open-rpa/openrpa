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
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Forge.Forms.FormBuilding;

namespace OpenRPA.Forms.Activities
{
    [System.ComponentModel.Designer(typeof(InvokeFormDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.form30.png")]
    [LocalizedToolboxTooltip("activity_invokeform_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_invokeform", typeof(Resources.strings))]
    public class InvokeForm : AsyncTaskCodeActivity<FormResult>
    {
        [RequiredArgument]
        public InArgument<string> Form { get; set; }
        public InArgument<bool> EnableSkin { get; set; }
        protected async override Task<FormResult> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var xmlString = Form.Get(context);
            FormResult result = null;
            string json = "";
            var enableSkin = EnableSkin.Get(context);
            // var enableSkin = false;

            var definition = FormBuilder.Default.GetDefinition(xmlString, freeze: false);
            List<string> fields = GenericTools.MainWindow.Dispatcher.Invoke<List<string>>(() =>
            {
                List<string> _fields = new List<string>();
                foreach (DataFormField f in definition.GetElements().Where(x => x is DataFormField))
                {
                    _fields.Add(f.Key);
                }
                return _fields;
            });
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
                        if(fields.Contains(v.DisplayName))
                        {
                            var test = new { value = value };
                            if (value.GetType() == typeof(System.Data.DataTable)) continue;
                            if (value.GetType() == typeof(System.Data.DataView)) continue;
                            if (value.GetType() == typeof(System.Data.DataRowView)) continue;
                            //
                            var asjson = JObject.FromObject(test);
                            param[v.DisplayName] = value;
                        }
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

            Exception LastError = null;
            var res = GenericTools.MainWindow.Dispatcher.Invoke<FormResult>(() =>
            {
                var f = new Form(xmlString, enableSkin);
                f.defaults = param;
                f.Topmost = true;
                f.Owner = GenericTools.MainWindow;
                if (f.ShowDialog() == false)
                {
                    if (f.LastError != null) LastError = f.LastError;
                }
                var _res = new FormResult();
                if(f.actionContext != null && f.actionContext.Action!=null) _res.Action = f.actionContext.Action.ToString();
                _res.Model = f.CurrentModel;
                return _res;
            });
            if (LastError != null) throw LastError;
            json = JsonConvert.SerializeObject(res, Formatting.Indented);
            result = JsonConvert.DeserializeObject<FormResult>(json);
            if(result.Model!=null)
                foreach (var prop in result.Model)
                {
                    var myVar = context.DataContext.GetProperties().Find(prop.Key, true);
                    if (myVar != null)
                    {
                        if(myVar.PropertyType == typeof(int))
                        {
                            if(prop.Value != null) myVar.SetValue(context.DataContext, int.Parse(prop.Value.ToString()));
                        } else
                        {
                            if (prop.Value != null) myVar.SetValue(context.DataContext, prop.Value);
                        }
                    }
                    else
                    {
                        Log.Debug("Recived property " + prop.Key + " but no variable exists to save the value in " + prop.Value);
                    }
                }
            await Task.Delay(1);

            return result;
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
