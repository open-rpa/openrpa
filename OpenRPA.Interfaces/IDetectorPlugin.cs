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
    public interface IDetectorPlugin : INotifyPropertyChanged
    {
        void Initialize(Detector Entity);
        string Name { get; }
        entity.Detector Entity { get; set;  }
        System.Windows.Controls.UserControl editor { get; }
        event DetectorDelegate OnDetector;
        void Start();
        void Stop();
    }
}
