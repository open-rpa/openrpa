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
        public async Task Save()
        {
            await Save<Detector>();
        }
        public async Task Delete()
        {
            await Delete<Detector>();
        }
    }
}
