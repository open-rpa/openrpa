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
        public int Priority { get => 200; }
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
        //private static PyObject InstallAndImport(bool force = false)
        //{
        //    //var installer = new Installer();
        //    Installer.SetupPython(force).Wait();
        //    //Installer.InstallWheel(typeof(NumPy).Assembly, "numpy-1.16.3-cp37-cp37m-win_amd64.whl").Wait();
        //    PythonEngine.Initialize();
        //    var mod = Py.Import("numpy");
        //    return mod;
        //}
        public static IOpenRPAClient client = null;
        public void Initialize(IOpenRPAClient client)
        {
            Plugin.client = client;
            _ = PluginConfig.csharp_intellisense;
            _ = PluginConfig.vb_intellisense;
            //_ = PluginConfig.use_embedded_python;
            //_ = PluginConfig.py_create_no_window;

            //System.Diagnostics.Debugger.Launch();
            //System.Diagnostics.Debugger.Break();
            bool hadError = false;
            //if (PluginConfig.use_embedded_python)
            //{
            //    if (!Python.Included.Installer.IsPythonInstalled())
            //    {
            //        Python.Included.Installer.SetupPython(true);//.Wait();
            //        while (!Python.Included.Installer.IsPythonInstalled())
            //        {
            //            Thread.Sleep(1000);
            //        }
            //    }
            //    else
            //    {
            //        Python.Included.Installer.SetupPython(false).Wait();
            //    }
            //    var path = Python.Included.Installer.EmbeddedPythonHome;
            //    PythonUtil.Setup.SetPythonPath(path, true);
            //    // Python.Runtime.PythonEngine.Initialize();
            //}
            //else
            //{
            //    try
            //    {
            //        if (!string.IsNullOrEmpty(PluginConfig.python_exe_path)) PythonUtil.Setup.SetPythonPath(PluginConfig.python_exe_path, false);
            //        PythonUtil.Setup.Run();
            //    }
            //    catch (Exception ex)
            //    {
            //        hadError = true;
            //        Log.Error(ex.ToString());
            //    }
            //}
            // PythonUtil.Setup.Run();
            //Python.Runtime.PythonEngine.Initialize();
            if (!hadError)
            {
                //try
                //{
                //    _ = Python.Runtime.PythonEngine.BeginAllowThreads();
                //}
                //catch (Exception ex)
                //{
                //    Log.Error(ex.ToString());
                //}
            }
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
        public IElement LaunchBySelector(Selector selector, bool CheckRunning, TimeSpan timeout)
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
        void IRecordPlugin.StatusTextMouseUp()
        {
        }
    }

}
