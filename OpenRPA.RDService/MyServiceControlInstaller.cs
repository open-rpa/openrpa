using OpenRPA.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    [System.ComponentModel.RunInstallerAttribute(true)]
    public class MyServiceControlInstaller : System.Configuration.Install.Installer
    {
        private System.ServiceProcess.ServiceProcessInstaller myServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
        private System.ServiceProcess.ServiceInstaller myServiceInstaller = new System.ServiceProcess.ServiceInstaller();
        public string ServiceName { get; set; } = Program.ServiceName;
        public MyServiceControlInstaller()
        {
            myServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            myServiceProcessInstaller.Password = null;
            myServiceProcessInstaller.Username = null;
            myServiceInstaller.Description = ServiceName;
            myServiceInstaller.DisplayName = ServiceName;
            myServiceInstaller.ServiceName = ServiceName;
            myServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            myServiceInstaller.ServicesDependedOn = new string[] { "WinRM" };
            // var i = new System.Configuration.Install.Installer();
            Installers.Add(myServiceProcessInstaller);
            Installers.Add(myServiceInstaller);
            // Installers.AddRange(i);
            //Installers.AddRange {)
            //Me.Installers.AddRange(new System.Configuration.Install.Installer() { in   myServiceProcessInstaller, myServiceInstaller});
        }
        public override void Install(IDictionary stateSaver)
        {
            if(Context.Parameters.ContainsKey("username") && Context.Parameters.ContainsKey("password"))
            {
                myServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.User;
                myServiceProcessInstaller.Username = Context.Parameters["username"];
                myServiceProcessInstaller.Password = Context.Parameters["password"];
                Console.WriteLine("username: " + Context.Parameters["username"]);
                Console.WriteLine("password: " + Context.Parameters["password"]);
            }
            if (Config.local.jwt != null && Config.local.jwt.Length > 0)
            {
                try
                {
                    PluginConfig.tempjwt = new System.Net.NetworkCredential(string.Empty, Config.local.UnprotectString(Config.local.jwt)).Password;
                    PluginConfig.entropy = null;
                    Config.Save();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            SetInterActWithDesktop();
            base.Install(stateSaver);
        }
        private static bool SetInterActWithDesktop()
        {
            //var service = new System.Management.ManagementObject(
            //        String.Format("WIN32_Service.Name='{0}'", "YourServiceName"));
            //try
            //{
            //    var paramList = new object[11];
            //    paramList[5] = true;
            //    service.InvokeMethod("Change", paramList);
            //    return true;
            //}
            //finally
            //{
            //    service.Dispose();
            //}
            return false;

        }
    }
}
