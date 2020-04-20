using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Script
{
    [Designer(typeof(PipInstallDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(PipInstall), "Resources.toolbox.comment.png")]
    [LocalizedToolboxTooltip("activity_pipinstall_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_pipinstall", typeof(Resources.strings))]
    public class PipInstall : CodeActivity
    {
        [LocalizedDisplayName("activity_pipinstall_modules", typeof(Resources.strings)), LocalizedDescription("activity_pipinstall_modules_help", typeof(Resources.strings))]
        public InArgument<string[]> Modules { get; set; }
        [LocalizedDisplayName("activity_pipinstall_force", typeof(Resources.strings)), LocalizedDescription("activity_pipinstall_force_help", typeof(Resources.strings))]
        public InArgument<bool> Force { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var modules = Modules.Get(context);

            if (PluginConfig.use_embedded_python)
            {
                var path = Python.Included.Installer.EmbeddedPythonHome;
                Log.Information("Installing pip into " + path);
                if (!Python.Included.Installer.IsPipInstalled())
                {
                    PythonUtil.Setup.InstallPip(path);
                    // Python.Included.Installer.InstallPip();
                }
                var force = Force.Get(context);
                foreach (var module in modules)
                {
                    string str1 = System.IO.Path.Combine(path, "Lib", module);
                    string str2 = System.IO.Path.Combine(path, "Lib", "site-packages", module);
                    var exists = System.IO.Directory.Exists(str1) && System.IO.File.Exists(System.IO.Path.Combine(str1, "__init__.py"));
                    if(!exists) exists = System.IO.Directory.Exists(str2) && System.IO.File.Exists(System.IO.Path.Combine(str2, "__init__.py"));
                    if(!exists || force)
                    {                        
                        Log.Information("Installing python module '" + module + "' into '" + str2 + "'");
                        Log.Information("run from '" + path + "'");
                        var cmd = System.IO.Path.Combine(path, "Scripts", "pip");
                        // var arg = " install  --no-warn-script-location --user " + module + (force ? " --force-reinstall" : "");
                        var arg = " install  --no-warn-script-location  " + module + (force ? " --force-reinstall" : "");

                        Log.Information(cmd + " " + arg);
                        var allcmd = "cd \"" + path + "\"" + Environment.NewLine + cmd + " " + arg;
                        System.IO.File.WriteAllText(System.IO.Path.Combine(path, "dopip.bat"), allcmd);
                        // PythonUtil.Setup.RunCommand2(path, allcmd);
                        PythonUtil.Setup.RunCommand(path, "dopip.bat", "");
                        // PythonUtil.Setup.RunCommand(path, allcmd, "");
                        //Python.Included.Installer.PipInstallModule(module);
                    }
                }
                
            }
            else
            {
                PythonUtil.Setup.Run(modules);
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