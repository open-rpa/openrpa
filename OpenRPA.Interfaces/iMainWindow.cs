using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IMainWindow
    {
        IDesigner Designer { get; }
        void OnDetector(IDetectorPlugin plugin, IDetectorEvent detector, EventArgs e);
        void IdleOrComplete(IWorkflowInstance instance, EventArgs e);
        void WebSocketClient_OnOpen();
        void SetStatus(string message);
        void Hide();
        void Show();
        void Close();
    }
}
