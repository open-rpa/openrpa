using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IDetector : IProjectableBase
    {
        string Plugin { get; set; }
        string detectortype { get; set; }
        Dictionary<string, object> Properties { get; set; }
        Task Delete(bool skipOnline = false);
        Task Save(bool skipOnline = false);
    }
}
