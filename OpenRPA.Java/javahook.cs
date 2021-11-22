using OpenRPA.NamedPipeWrapper;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsAccessBridgeInterop;

namespace OpenRPA.Java
{
    public class Javahook
    {
        public delegate void OnInitilizedDelegate(AccessBridge accessBridge);
        public event OnInitilizedDelegate OnInitilized;
        public delegate void OnNewjvmDelegate(AccessBridge accessBridge, AccessibleJvm[] newjvms);
        public event OnNewjvmDelegate OnNewjvm;
        public delegate void JavaShutDownDelegate(int vmID);
        public event JavaShutDownDelegate OnJavaShutDown;
        public delegate void JavaHandler();
        public event JavaHandler Connected;
        public event JavaHandler Disconnected;

        private HwndCache _windowCache = null;
        private SingleDelayedTask _delayedRefresh = new SingleDelayedTask();
        private SingleDelayedTask _delayedAddEvents = new SingleDelayedTask();
        public List<AccessibleJvm> jvms = new List<AccessibleJvm>();
        public bool Initilized = false;
        public AccessBridge accessBridge { get; set; } = new AccessBridge();
        public bool IsLoaded
        {
            get
            {
                return accessBridge.IsLoaded;
            }
        }
        public bool IsLegacy
        {
            get
            {
                return accessBridge.IsLegacy;
            }
        }
        private static Javahook _instance = null;
        public static Javahook Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Javahook();
                    // _instance.init();
                }
                return _instance;
            }
        }
        public void init()
        {
            try
            {
                if (Initilized) return;
                GenericTools.RunUI(() =>
                {
                    Initilized = false;
                    _windowCache = new HwndCache();
                    Log.Debug("javahook.init()");
                    accessBridge.Initilized += (e1, e2) =>
                    {
                        Initilized = true;
                        Log.Information("javahook._accessBridge.Initilized");
                        OnInitilized?.Invoke(accessBridge);
                    };
                    try
                    {
                        if (IntPtr.Size == 4)
                        {
                            // accessBridge.Initialize(true);
                            accessBridge.Initialize(false);
                        }
                        else
                        {
                            accessBridge.Initialize(false);
                        }

                        refreshJvms(200);

                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void dispose()
        {
            accessBridge.Dispose();
        }
        public void refreshJvms()
        {
            _windowCache.Clear();
            jvms.Clear();
            refreshJvms(0);
            // jvms = EnumJvms();
        }
        public void refreshJvms(int miliseconds)
        {
            if (miliseconds == 0)
            {
                _refreshJvms();
                return;
            }
            _delayedRefresh.Post(TimeSpan.FromMilliseconds(miliseconds), () =>
            {
                _refreshJvms();
                refreshJvms(10000);
            });
        }
        public void _refreshJvms()
        {
            try
            {
                bool added = false;
                List<AccessibleJvm> newjvms = new List<AccessibleJvm>();
                _windowCache.Clear();
                var _jvms = EnumJvms();
                foreach (var jvm in _jvms)
                {
                    if (!jvms.Contains(jvm))
                    {
                        added = true;
                        jvms.Add(jvm);
                        newjvms.Add(jvm);
                    }
                }
                if (added)
                {
                    OnNewjvm?.Invoke(accessBridge, newjvms.ToArray());
                }
                // refreshJvms(1000);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
        private List<AccessibleJvm> EnumJvms()
        {
            return accessBridge.EnumJvms(hwnd => _windowCache.Get(accessBridge, hwnd));
        }
    }
}
