//using ClosedXML.Excel;
//using ExcelDataReader;
using sharpAHK;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Forms;

namespace OpenRPA.Script.Activities
{
    [System.ComponentModel.Designer(typeof(ExecuteAHKScriptDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.autohotkey.png")]
    //[designer.ToolboxTooltip(Text = "Read CSV, xls or xlsx file and loads it into a DataSet")]
    public class ExecuteAHKScript : CodeActivity // , System.Activities.Presentation.IActivityTemplateFactory
    {
        [RequiredArgument]
        [OverloadGroup("filename")]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        [OverloadGroup("script")]
        public InArgument<string> Script { get; set; }
        [RequiredArgument]
        public InArgument<bool> NewInstance { get; set; }
        public void New_AHKSession(bool NewInstance = false)
        {
            if (ahkGlobal.ahkdll == null || NewInstance == true) { ahkGlobal.ahkdll = new AutoHotkey.Interop.AutoHotkeyEngine(); }

            else { ahkGlobal.ahkdll = null; }  // option to start new AHK session (resets variables and previously loaded functions)

            ahkGlobal.LoadedAHK = new List<string>(); // reset loaded ahk list
        }
        protected override void Execute(CodeActivityContext context)
        {
            var filename = Filename.Get(context);
            var script = Script.Get(context);
            var newinstance = NewInstance.Get(context);
            if (sharpAHK.ahkGlobal.ahkdll == null || newinstance) { New_AHKSession(newinstance); }
            if (!string.IsNullOrEmpty(filename))
            {
                if (!System.IO.File.Exists(filename)) throw new System.IO.FileNotFoundException("File not found", filename);
                script = System.IO.File.ReadAllText(filename);
            }
            var vars = context.DataContext.GetProperties();
            foreach (dynamic v in vars)
            {
                var value = v.GetValue(context.DataContext);
                if (value != null)
                {
                    try
                    {

                        sharpAHK.ahkGlobal.ahkdll.SetVar(v.DisplayName, value.ToString());
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    sharpAHK.ahkGlobal.ahkdll.SetVar(v.DisplayName, "");
                }
            }
            sharpAHK.ahkGlobal.ahkdll.ExecRaw(script);
            foreach (dynamic v in vars)
            {
                try
                {
                    var value = sharpAHK.ahkGlobal.ahkdll.GetVar(v.DisplayName);
                    v.SetValue(context.DataContext, value);
                }
                catch (Exception)
                {
                }
            }
            //var result = sharpAHK.ahkGlobal.ahkdll.ExecFunction(function, param1, param2, param3, param4);
            //Result.Set(context, result);

        }


    }
}