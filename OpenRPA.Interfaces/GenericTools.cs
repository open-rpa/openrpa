using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class GenericTools
    {
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MINIMIZE = 6;
        public const int SW_RESTORE = 9;

        public const int SW_SHOWDEFAULT = 10;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_SHOWNOACTIVATE = 4;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);
        public delegate void CancelHandler(EventArgs e);
        private static event CancelHandler _OnCancel;
        public static event CancelHandler OnCancel
        {
            add
            {
                if(_OnCancel== null)
                {
                    Input.InputDriver.Instance.OnKeyUp += Instance_OnKeyUp;
                }
                _OnCancel += value;
            }
            remove
            {
                _OnCancel -= value;
                if (_OnCancel == null)
                {
                    Input.InputDriver.Instance.OnKeyUp -= Instance_OnKeyUp;
                }
            }
        }
        private static void Instance_OnKeyUp(Input.InputEventArgs e)
        {
            if(e.Key == Input.KeyboardKey.ESC)
            {
                _OnCancel?.Invoke(EventArgs.Empty);
            }
        }

        public static void minimize(System.Windows.Window window)
        {
            RunUI(window, () =>
            {
                GenericTools.ShowWindow(new System.Windows.Interop.WindowInteropHelper(window).Handle, GenericTools.SW_MINIMIZE);
            });
        }
        public static void minimize(IntPtr hWnd)
        {
            GenericTools.ShowWindow(hWnd, GenericTools.SW_MINIMIZE);
        }
        public static void restore()
        {
            RunUI(() =>
            {
                if (mainWindow.WindowState == System.Windows.WindowState.Minimized)
                {
                    restore(handle);
                }
            });
        }
        public static void restore(System.Windows.Window window)
        {
            RunUI(window, () =>
            {
                IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                restore(hWnd);
            });
        }
        public static void restore(IntPtr hWnd)
        {
            GenericTools.ShowWindow(hWnd, GenericTools.SW_RESTORE);
            GenericTools.SetForegroundWindow(hWnd);
        }
        private static IntPtr _handle = IntPtr.Zero;
        public static IntPtr handle
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    _handle = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
                }
                return _handle;
            }
        }
        public static System.Windows.Window mainWindow { get; set; }

        public static void RunUI(Action action)
        {
            RunUI(mainWindow, action);
        }
        public static void RunUI(System.Windows.Window window, Action action)
        {
            if (window != null)
            {
                window.Dispatcher.Invoke(() =>
                {
                    action();
                });
            }
            else
            {
                action();
            }
        }
        private delegate void SafeCallDelegate();
        public static void RunUI(System.Windows.Forms.Form window, Action action)
        {
            if (window != null)
            {
                var d = new SafeCallDelegate(action);
                window.Invoke(d);
            }
            else
            {
                action();
            }
        }

    }
}
