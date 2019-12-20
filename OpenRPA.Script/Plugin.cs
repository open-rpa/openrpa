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

namespace OpenRPA.Script
{
    public class Plugin : ObservableObject, IRecordPlugin
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public static treeelement[] _GetRootElements(Selector anchor)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var result = new List<treeelement>();
            return result.ToArray();
        }
        public treeelement[] GetRootElements(Selector anchor)
        {
            return Plugin._GetRootElements(anchor);
        }
        public Selector GetSelector(Selector anchor, Interfaces.Selector.treeelement item)
        {
            return null;
        }
        public event Action<IRecordPlugin, IRecordEvent> OnUserAction
        {
            add { }
            remove { }
        }
        public event Action<IRecordPlugin, IRecordEvent> OnMouseMove
        {
            add { }
            remove { }
        }
        private Views.RecordPluginView view;
        public System.Windows.Controls.UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.RecordPluginView();
                }
                return view;
            }
        }
        public string Name { get => "Script"; }
        public string Status { get => ""; }
        public void Start()
        {
        }
        public void Stop()
        {
        }
        public bool ParseUserAction(ref IRecordEvent e)
        {
            return false;
        }
        public void Initialize(IOpenRPAClient client)
        {
            _ = PluginConfig.csharp_intellisense;
            _ = PluginConfig.vb_intellisense;
            Python.Runtime.PythonEngine.Initialize();
            _ = Python.Runtime.PythonEngine.BeginAllowThreads();
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
        public bool ParseMouseMoveAction(ref IRecordEvent e)
        {
            return false;
        }
    }

}
