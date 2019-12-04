using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using OpenRPA.Script.Activities;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.Java
{
    public class Plugin : ObservableObject, IRecordPlugin
    {
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            var result = new List<treeelement>();
            return result.ToArray();
        }
        public treeelement[] GetRootElements(Selector anchor)
        {
            return Plugin._GetRootElements(anchor);
        }
        public Interfaces.Selector.Selector GetSelector(Selector anchor, Interfaces.Selector.treeelement item)
        {
            return null;
        }
        public event Action<IRecordPlugin, IRecordEvent> OnUserAction;
        public event Action<IRecordPlugin, IRecordEvent> OnMouseMove;
        public System.Windows.Controls.UserControl editor => null;
        public string Name { get => "Script"; }
        // public string Status => (hook!=null && hook.jvms.Count>0 ? "online":"offline");
        public string Status { get => ""; }
        public void Start()
        {
        }
        public void Stop()
        {
        }
        public bool parseUserAction(ref IRecordEvent e)
        {
            return false;
        }
        public void Initialize(IOpenRPAClient client)
        {
            Python.Runtime.PythonEngine.Initialize();
            IntPtr ctx = Python.Runtime.PythonEngine.BeginAllowThreads();
            //if (InvokeCode.pool == null)
            //{
            //    InvokeCode.pool = RunspaceFactory.CreateRunspacePool(1, 5);
            //    InvokeCode.pool.ApartmentState = System.Threading.ApartmentState.MTA;
            //    InvokeCode.pool.Open();
            //}

        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            return null;
        }
        public void LaunchBySelector(Selector selector, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
            throw new NotImplementedException();
        }
        public bool Match(SelectorItem item, IElement m)
        {
            return false;
        }
        public bool parseMouseMoveAction(ref IRecordEvent e)
        {
            return false;
        }
    }

}
