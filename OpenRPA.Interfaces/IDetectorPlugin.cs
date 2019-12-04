using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public delegate void DetectorDelegate(IDetectorPlugin plugin, IDetectorEvent detector, EventArgs e);
    public interface IDetectorPlugin : INotifyPropertyChanged, IPlugin
    {
        void Initialize(IOpenRPAClient client, Detector Entity);
        Detector Entity { get; set;  }
        event DetectorDelegate OnDetector;
        void Start();
        void Stop();
    }
}
