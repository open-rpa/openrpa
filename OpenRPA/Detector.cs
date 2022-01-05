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
        }
        public string Plugin { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public Dictionary<string, object> Properties { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public string projectid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public async Task Save()
        {
            await Save<Detector>();
        }
        public async Task Delete()
        {
            await Delete<Detector>();
        }
        public void ExportFile(string filepath)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            System.IO.File.WriteAllText(filepath, json);
        }
        public void Start()
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
            dp.OnDetector -= RobotInstance.instance.Window.OnDetector;
            dp.OnDetector += RobotInstance.instance.Window.OnDetector;
            dp.Start();
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
            Start();
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(name)) return "Detector";
            return name;
        }
    }
}
