using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDServiceMonitor
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
            myServiceInstaller.ServicesDependedOn = new string[] { "OpenRPA" };
            Installers.Add(myServiceProcessInstaller);
            Installers.Add(myServiceInstaller);
        }
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
        }
    }
}
