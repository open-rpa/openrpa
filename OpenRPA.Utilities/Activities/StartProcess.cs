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
    [Designer(typeof(StartProcessDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.play.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class StartProcess : CodeActivity
    {
        [RequiredArgument, Category("Input"), Description("The full filename and path to the executable to start")]
        public InArgument<string> Filename { get; set; }
        [Category("Input"), Description("Arguments to be parsed the the executable")]
        public InArgument<string> Arguments { get; set; }
        [Category("Input"), Description("Working directory for the executable")]
        public InArgument<string> WorkingDirectory { get; set; }
        [Category("Input"), Description("Wait for the process to exit")]
        public InArgument<bool> WaitForExit { get; set; }
        [Category("Input"), Description("Wait until for this long, for the process to exit")]
        public InArgument<TimeSpan> WaitForExitTimeout { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var filename = Filename.Get(context);
            var arguments = Arguments.Get(context);
            var workingdirectory = WorkingDirectory.Get(context);
            var waitforexit = WaitForExit.Get(context);
            var waitforexittimeout = WaitForExitTimeout.Get(context);
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = filename;
            startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = workingdirectory;
            var p = Process.Start(startInfo);
            if (waitforexit)
            {
                if(waitforexittimeout.TotalMilliseconds > 50)
                {
                    p.WaitForExit((int)waitforexittimeout.TotalMilliseconds);
                } else
                {
                    p.WaitForExit();
                }
            }
        }

    }


}