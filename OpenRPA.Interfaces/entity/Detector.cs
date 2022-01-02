using Newtonsoft.Json;
using OpenRPA.Interfaces.entity;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;


namespace OpenRPA.Input
{
    public class Detector : apibase, IDetector
    {
        public Detector()
        {
            _type = "detector";
            Properties = new Dictionary<string, object>();
        }
        public string Plugin { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public Dictionary<string, object> Properties { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public string projectid { get; set; }
    }
}
