using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IPlugin
    {
        void Initialize(IOpenRPAClient client);
        System.Windows.Controls.UserControl editor { get; }
        string Name { get; }
    }
}
