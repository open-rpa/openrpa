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

namespace OpenRPA.AHK.Activities
{
    [System.ComponentModel.Designer(typeof(ExecuteFunctionDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.autohotkey.png")]
    //[designer.ToolboxTooltip(Text = "Read CSV, xls or xlsx file and loads it into a DataSet")]
    public class ExecuteFunction : CodeActivity // , System.Activities.Presentation.IActivityTemplateFactory
    {
        [RequiredArgument]
        [OverloadGroup("filename")]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        [OverloadGroup("script")]
        public InArgument<string> Script { get; set; }
        [RequiredArgument]
        public InArgument<bool> NewInstance { get; set; }
        [RequiredArgument]
        public InArgument<string> Function { get; set; }
        public InArgument<string> Param1 { get; set; }
        public InArgument<string> Param2 { get; set; }
        public InArgument<string> Param3 { get; set; }
        public InArgument<string> Param4 { get; set; }
        public OutArgument<string> Result { get; set; }
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
            var function = Function.Get(context);
            var newinstance = NewInstance.Get(context);
            var param1 = Param1.Get(context);
            var param2 = Param2.Get(context);
            var param3 = Param3.Get(context);
            var param4 = Param4.Get(context);
            if (sharpAHK.ahkGlobal.ahkdll == null || newinstance) { New_AHKSession(newinstance); }
            if (!sharpAHK.ahkGlobal.ahkdll.FunctionExists(function))
            {
                if (!string.IsNullOrEmpty(filename))
                {
                    sharpAHK.ahkGlobal.ahkdll.LoadFile(filename);
                }
                else
                {
                    sharpAHK.ahkGlobal.ahkdll.LoadScript(script);
                }
            }
            if (!sharpAHK.ahkGlobal.ahkdll.FunctionExists(function)) throw new ArgumentNullException("Function '" + function + "' does not exists");
            var result = sharpAHK.ahkGlobal.ahkdll.ExecFunction(function, param1, param2, param3, param4);
            Result.Set(context, result);

        }


    }
}