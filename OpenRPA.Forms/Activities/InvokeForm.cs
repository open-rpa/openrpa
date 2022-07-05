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
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.invokeform.png")]
    [LocalizedToolboxTooltip("activity_invokeform_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_invokeform", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_invokeform_helpurl", typeof(Resources.strings))]
    public class InvokeForm : AsyncTaskCodeActivity<FormResult>
    {
        [RequiredArgument]
        public InArgument<string> Form { get; set; }
        public Dictionary<string, Argument> Arguments { get; set; } = new Dictionary<string, Argument>();
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
            if (Arguments == null || Arguments.Count == 0)
            {
                foreach (dynamic v in vars)
                {
                    var value = v.GetValue(context.DataContext);
                    if (value != null)
                    {
                        //_payload.Add(v.DisplayName, value);
                        try
                        {
                            try
                            {
                                //if(fields.Contains(v.DisplayName))
                                if (value.GetType() == typeof(System.Data.DataTable)) continue;
                                if (value.GetType() == typeof(System.Data.DataView)) continue;
                                if (value.GetType() == typeof(System.Data.DataRowView)) continue;
                                param[v.DisplayName] = value;
                            }
                            catch (Exception)
                            {

                                throw;
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
                            if (value != null) param[a.Key] = ((System.Data.DataTable)value).ToJArray();
                        }
                        else
                        {
                            param[a.Key] = a.Value;
                        }
                    }
                    else { param[a.Key] = null; }
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
                if (f.actionContext != null && f.actionContext.Action != null) _res.Action = f.actionContext.Action.ToString();
                _res.Model = f.CurrentModel;
                return _res;
            });
            if (LastError != null) throw LastError;
            json = JsonConvert.SerializeObject(res, Formatting.Indented);
            result = JsonConvert.DeserializeObject<FormResult>(json);
            if (result.Model != null)
                if (Arguments == null || Arguments.Count == 0)
                {
                    foreach (var prop in result.Model)
                    {
                        var myVar = context.DataContext.GetProperties().Find(prop.Key, true);
                        if (myVar != null)
                        {
                            try
                            {
                                if (myVar.PropertyType == typeof(int))
                                {
                                    if (prop.Value != null) myVar.SetValue(context.DataContext, int.Parse(prop.Value.ToString()));
                                }
                                else
                                {
                                    if (prop.Value != null) myVar.SetValue(context.DataContext, prop.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
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
                        var prop = result.Model[a.Key];
                        if (prop != null)
                        {
                            var myVar = context.DataContext.GetProperties().Find(a.Key, true);
                            if (Arguments[a.Key].ArgumentType == typeof(System.Data.DataTable))
                            {
                                try
                                {
                                    var json2 = result.Model[a.Key].ToString();
                                    if (!string.IsNullOrEmpty(json2))
                                    {
                                        var jarray = JArray.Parse(json2);
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
                                if (result.Model[a.Key] is JToken t)
                                {
                                    System.Reflection.MethodInfo method = typeof(JToken).GetMethod(nameof(JToken.Value)); // typeof(JToken).GetMethod(nameof(JToken.Value));
                                    System.Reflection.MethodInfo generic = method.MakeGenericMethod(Arguments[a.Key].ArgumentType);
                                    var value = generic.Invoke(t, new object[] { });
                                    Arguments[a.Key].Set(context, value);
                                }
                                else if (result.Model[a.Key] is JArray _a)
                                {
                                }
                                else if (result.Model[a.Key] is Int64 i64)
                                {
                                    if(Arguments[a.Key].ArgumentType == typeof(int))
                                    {
                                        Arguments[a.Key].Set(context, Convert.ToInt32(result.Model[a.Key]));
                                    } else
                                    {
                                        Arguments[a.Key].Set(context, result.Model[a.Key]);
                                    }
                                }
                                else
                                {
                                    Arguments[a.Key].Set(context, result.Model[a.Key]);
                                }
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
