using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    public delegate void ServiceProcess();
    public class MyServiceBase : System.ServiceProcess.ServiceBase
    {
        public MyServiceBase(string ServiceName, ServiceProcess work)
        {
            this.ServiceName = ServiceName;
            this.work = work;
        }
        private ServiceProcess work;
        public static bool isRunning = false;
        private Task task = null;
        protected override void OnStart(string[] args)
        {
            task = Task.Run(() => { isRunning = true;  work(); isRunning = true; });
        }
        protected override void OnStop()
        {
            isRunning = false;
            while (!task.IsCompleted) System.Threading.Thread.Sleep(50);
            Log.Information("Complete.");
        }
    }
}
