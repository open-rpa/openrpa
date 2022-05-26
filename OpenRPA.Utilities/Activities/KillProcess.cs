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
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using ExcelDataReader;
using System.Data;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;
using System.Windows;

namespace OpenRPA.Utilities
{
    [Designer(typeof(KillProcessDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.killprocess.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class KillProcess : CodeActivity
    {
        [RequiredArgument, Category("Input"), Description("The Name of process to kill ( use windows taskmanager to find it, if in doubt )")]
        public InArgument<string> ProcessName { get; set; }
        [Category("Input"), Description("Kill process in all Windows Session")]
        public InArgument<bool> AllSessions { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var processname = ProcessName.Get(context);
            var allsessions = AllSessions.Get(context);
            Process[] looplist = null;
            if (allsessions)
            {
                looplist = Process.GetProcessesByName(processname);
                foreach (var p in looplist)
                {
                    p.Kill();
                }
                return;
            }
            var me = Process.GetCurrentProcess();
            looplist = Process.GetProcessesByName(processname).Where(x => x.SessionId == me.SessionId).ToArray();
            foreach (var p in looplist)
            {
                p.Kill();
            }

        }

    }


}