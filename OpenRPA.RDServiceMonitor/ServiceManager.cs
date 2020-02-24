using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDServiceMonitor
{
    public class ServiceManager
    {
        public ServiceManager(string ServiceName)
        {
            this.ServiceName = ServiceName;
        }
        public string ServiceName { get; set; }
        public System.ServiceProcess.ServiceControllerStatus Status
        {
            get
            {
                using (var serviceController = new System.ServiceProcess.ServiceController(ServiceName))
                {
                    return serviceController.Status;
                }
            }
        }
        public bool IsServiceInstalled
        {
            get
            {
                try
                {
                    var status = Status;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        public void InstallService(Type fromType, string[] commandLineOptions)
        {
            if (IsServiceInstalled) return;
            var state = new System.Collections.Hashtable();
            var installer = GetAssemblyInstaller(fromType, commandLineOptions);
            installer.Install(state);
            installer.Commit(state);
        }
        public void InstallService(string location, string[] commandLineOptions)
        {
            if (IsServiceInstalled) return;
            var state = new System.Collections.Hashtable();
            var installer = GetAssemblyInstaller(location, commandLineOptions);
            installer.Install(state);
            installer.Commit(state);
        }
        public void UninstallService(Type fromType)
        {
            if (!IsServiceInstalled) return;
            var state = new System.Collections.Hashtable();
            var installer = GetAssemblyInstaller(fromType, null);
            installer.Uninstall(state);
        }
        public void UninstallService(string location)
        {
            if (!IsServiceInstalled) return;
            var state = new System.Collections.Hashtable();
            var installer = GetAssemblyInstaller(location, null);
            installer.Uninstall(state);
        }
        public System.Configuration.Install.AssemblyInstaller GetAssemblyInstaller(Type fromType, string[] commandLineOptions)
        {
            string location = null;
            if (fromType != null)
            {
                location = fromType.Assembly.Location;
            }
            else
            {
                location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            }
            return GetAssemblyInstaller(location, commandLineOptions);
        }
        public System.Configuration.Install.AssemblyInstaller GetAssemblyInstaller(string location, string[] commandLineOptions)
        {
            var installer = new System.Configuration.Install.AssemblyInstaller(location, commandLineOptions);
            installer.UseNewContext = true;
            return installer;
        }
        public async Task StartService()
        {
            if (!IsServiceInstalled) return;
            using (var serviceController = new System.ServiceProcess.ServiceController(ServiceName))
            {
                if (serviceController.Status != System.ServiceProcess.ServiceControllerStatus.Stopped) return;
                serviceController.Start();
                await WaitForStatusChange(serviceController, System.ServiceProcess.ServiceControllerStatus.Running);
            }
        }
        public async Task StopService()
        {
            if (!IsServiceInstalled) return;
            using (var serviceController = new System.ServiceProcess.ServiceController(ServiceName))
            {
                if (serviceController.Status != System.ServiceProcess.ServiceControllerStatus.Running) return;
                serviceController.Stop();
                await WaitForStatusChange(serviceController, System.ServiceProcess.ServiceControllerStatus.Running);
            }
        }
        private async Task WaitForStatusChange(System.ServiceProcess.ServiceController serviceController, System.ServiceProcess.ServiceControllerStatus NewStatus)
        {
            int count = 0;
            while (serviceController.Status != NewStatus && count < 30)
            {
                await Task.Delay(1000);
                serviceController.Refresh();
                count++;
                if (NewStatus == System.ServiceProcess.ServiceControllerStatus.Running && serviceController.Status == System.ServiceProcess.ServiceControllerStatus.Stopped) { break; }
            }
            if (serviceController.Status != NewStatus) throw new Exception("Failed to change status of service. Current status: " + serviceController.Status + " Desired status: " + NewStatus);
        }
    }
}
