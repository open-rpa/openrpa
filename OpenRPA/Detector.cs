using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class Detector : LocallyCached, IDetector
    {
        public Detector()
        {
            _type = "detector";
            Properties = new Dictionary<string, object>();
            detectortype = "exchange";
        }
        public string Plugin { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public Dictionary<string, object> Properties { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public string projectid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string detectortype { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public async Task Save(bool skipOnline = false)
        {
            await Save<Detector>(skipOnline);
            if (System.Threading.Monitor.TryEnter(RobotInstance.instance.Detectors, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    var exists = RobotInstance.instance.Detectors.FindById(_id);
                    if (exists == null) RobotInstance.instance.Detectors.Add(this);
                    if (exists != null) RobotInstance.instance.Detectors.UpdateItem(exists, this);
                }
                finally
                {
                    System.Threading.Monitor.Exit(RobotInstance.instance.Detectors);
                }
            }
            else { throw new LockNotReceivedException("Saving detector"); }
        }
        public async Task Delete(bool skipOnline = false)
        {
            if(!skipOnline) await Delete<Detector>();
            if (System.Threading.Monitor.TryEnter(RobotInstance.instance.Detectors, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    RobotInstance.instance.Detectors.Remove(this);
                }
                finally
                {
                    System.Threading.Monitor.Exit(RobotInstance.instance.Detectors);
                }
            }
            else { throw new LockNotReceivedException("Deleting detector"); }
        }
        public void ExportFile(string filepath)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            System.IO.File.WriteAllText(filepath, json);
        }
        public void Start(bool doRegisterExchange)
        {
            IDetectorPlugin dp = Plugins.detectorPlugins.Where(x => x.Entity._id == _id).FirstOrDefault();
            if (dp == null)
            {
                dp = Plugins.AddDetector(RobotInstance.instance, this);
                if (dp == null)
                {
                    Log.Error("Failed loading detector " + name + " " + _id);
                    return;
                }
            }
            if(doRegisterExchange) _ = RegisterExchange();
            if(RobotInstance.instance != null && RobotInstance.instance.Window != null)
            {
                dp.OnDetector -= RobotInstance.instance.Window.OnDetector;
                dp.OnDetector += RobotInstance.instance.Window.OnDetector;
            } else
            {
                Log.Error("Failed registering detector event sink for " + name + " main window not loaded yet !!!!!");
            }
            dp.Start();
        }
        async public Task RegisterExchange()
        {
            Log.Debug("Register Exchange for " + name);
            IDetectorPlugin dp = Plugins.detectorPlugins.Where(x => x.Entity._id == _id).FirstOrDefault();
            if (dp == null) return;
            if (global.webSocketClient != null && global.webSocketClient.isConnected && global.openflowconfig != null && !string.IsNullOrEmpty(global.openflowconfig.version))
            {
                var ver = Version.Parse(global.openflowconfig.version);
                var reqver = Version.Parse("1.3.103"); // exchange support for detectors was not added until 1.3.103
                if (ver < reqver) return;
                if (dp.Entity != null && dp.Entity.detectortype == "exchange" && !string.IsNullOrEmpty(dp.Entity._id))
                {                    
                    await global.webSocketClient.RegisterExchange(dp.Entity._id, "fanout", false);
                }
            }
        }
        public void Stop()
        {
            var dp = Plugins.detectorPlugins.Where(x => x.Entity._id == _id).FirstOrDefault();
            if (dp != null)
            {
                dp.OnDetector -= RobotInstance.instance.Window.OnDetector;
                dp.Stop();
                Plugins.detectorPlugins.Remove(dp);
            }
        }
        public void UpdateRunning()
        {
            Stop();
            Plugins.UpdateDetector(RobotInstance.instance, this);
            Start(true);
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(name)) return "Detector";
            return name;
        }
    }
}
