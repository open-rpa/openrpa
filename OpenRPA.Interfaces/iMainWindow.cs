using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IMainWindow
    {
        event ReadyForActionEventHandler ReadyForAction;
        event StatusEventHandler Status;
        bool VisualTracking { get; set; }
        bool SlowMotion { get; set; }
        bool IsLoading { get; set; }
        object SelectedContent { get; }
        void OnOpenWorkflow(IWorkflow workflow);
        IDesigner[] Designers { get; }
        IDesigner Designer { get; }
        IDesigner LastDesigner { get; }
        void OnDetector(IDetectorPlugin plugin, IDetectorEvent detector, EventArgs e);
        void IdleOrComplete(IWorkflowInstance instance, EventArgs e);
        void MainWindow_WebSocketClient_OnOpen();
        void SetStatus(string message);
        void Hide();
        void Show();
        void Close();
        void OnOpen(object _item);
    }
}
