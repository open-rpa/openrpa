using OpenRPA.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class GenericTools
    {
        public static void Minimize()
        {
            Minimize(MainWindow);
        }
        public static void Minimize(System.Windows.Window window)
        {
            RunUI(window, () =>
            {
                if (window.WindowState != System.Windows.WindowState.Minimized)
                {
                    NativeMethods.ShowWindow(new System.Windows.Interop.WindowInteropHelper(window).Handle, NativeMethods.SW_MINIMIZE);

                }
            });
        }
        public static void Minimize(IntPtr hWnd)
        {
            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_MINIMIZE);
        }
        public static void Restore()
        {
            RunUI(() =>
            {
                if (MainWindow.WindowState == System.Windows.WindowState.Minimized)
                {
                    MainWindow.Visibility = System.Windows.Visibility.Visible;
                    Restore(Handle);
                }
            });
        }
        public static void Restore(System.Windows.Window window)
        {
            RunUI(window, () =>
            {
                if (window.WindowState == System.Windows.WindowState.Minimized)
                {
                    window.Visibility = System.Windows.Visibility.Visible;
                    IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                    Restore(hWnd);
                }
            });
        }
        public static void Restore(IntPtr hWnd)
        {
            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(hWnd);
        }
        private static IntPtr _handle = IntPtr.Zero;
        public static IntPtr Handle
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    _handle = new System.Windows.Interop.WindowInteropHelper(MainWindow).Handle;
                }
                return _handle;
            }
        }
        public static System.Windows.Window MainWindow { get; set; }
        public static IDesigner Designer { get => ((IMainWindow)MainWindow).Designer; }
        public static async Task<T> RunUIAsync<T>(Func<Task<T>> action)
        {
            return await RunUIAsync(MainWindow, action);
        }
        public static async Task<T> RunUIAsync<T>(System.Windows.Window window, Func<Task<T>> action)
        {
            if (window != null)
            {
                return await window.Dispatcher.Invoke(action);
            }
            else
            {
                return await action();
            }
        }
        public static void RunUI(Action action)
        {
            RunUI(MainWindow, action);
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
        public static string ToShortString() => ToShortString(Guid.NewGuid());
        public static string ToShortString(Guid guid)
        {
            var base64Guid = Convert.ToBase64String(guid.ToByteArray());
            // Replace URL unfriendly characters with better ones
            base64Guid = base64Guid.Replace('+', '-').Replace('/', '_');
            // Remove the trailing ==
            return base64Guid.Substring(0, base64Guid.Length - 2);
        }
        public static Guid FromShortString(string str)
        {
            str = str.Replace('_', '/').Replace('-', '+');
            var byteArray = Convert.FromBase64String(str + "==");
            return new Guid(byteArray);
        }
        public static string YoutubeLikeId()
        {
            System.Threading.Thread.Sleep(1);//make everything unique while looping
            long ticks = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0))).TotalMilliseconds;//EPOCH
            char[] baseChars = new char[] { '0','1','2','3','4','5','6','7','8','9',
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x'};
            int i = 32;
            char[] buffer = new char[i];
            int targetBase = baseChars.Length;
            do
            {
                buffer[--i] = baseChars[ticks % targetBase];
                ticks /= targetBase;
            }
            while (ticks > 0);
            char[] result = new char[32 - i];
            Array.Copy(buffer, i, result, 0, 32 - i);
            return new string(result);
        }
    }
}
