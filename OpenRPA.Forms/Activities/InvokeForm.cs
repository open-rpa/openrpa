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

namespace OpenRPA.Forms.Activities
{
    [System.ComponentModel.Designer(typeof(InvokeFormDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.form30.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class InvokeForm : AsyncTaskCodeActivity<FormResult>
    {
        [RequiredArgument]
        public InArgument<string> Form { get; set; }
        protected async override Task<FormResult> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var xmlString = Form.Get(context);
            FormResult result = null;
            string json = "";

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

            Exception LastError = null;
            // var res = await GenericTools.mainWindow.Dispatcher.InvokeAsync<FormResult>(() =>
            var res = GenericTools.mainWindow.Dispatcher.Invoke<FormResult>(() =>
            {
                var f = new Form(xmlString);
                f.defaults = param;
                f.Topmost = true;
                if(f.ShowDialog() == false)
                {
                    if (f.LastError != null) LastError = f.LastError;
                }
                var _res = new FormResult();
                if(f.actionContext != null && f.actionContext.Action!=null) _res.Action = f.actionContext.Action.ToString();
                _res.Model = f.CurrentModel;
                return _res;
            });
            if (LastError != null) throw LastError;
            //var t = await GenericTools.mainWindow.Dispatcher.InvokeAsync(() =>
            //{
            //    //var min = true;
            //    //if (GenericTools.mainWindow.WindowState != System.Windows.WindowState.Minimized) min = false;
            //    //GenericTools.restore();
            //    object model = Forge.Forms.FormBuilding.FormBuilder.Default.GetDefinition(xmlString);
            //    // var options = new Forge.Forms.WindowOptions();
            //    var options = Forge.Forms.WindowOptions.Default;
            //    options.CanResize = true; options.TopMost = true; options.ShowCloseButton = true;
            //    options.BringToFront = true;
            //    var _result = Forge.Forms.Show.Window(options).For<object>(model);
            //    // if (min) { GenericTools.minimize(); }
            //    return _result;
            //});
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
                            myVar.SetValue(context.DataContext, int.Parse(prop.Value.ToString()));
                        } else
                        {
                            myVar.SetValue(context.DataContext, prop.Value);
                        }
                        //var myValue = myVar.GetValue(context.DataContext);
                    
                    }
                    else
                    {
                        Log.Debug("Recived property " + prop.Key + " but no variable exists to save the value in " + prop.Value);
                    }
                }

            return result;
        }
    }
}
