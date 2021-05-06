using OpenRPA.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
            RunUI(() =>
            {
                try
                {
                    if (window.WindowState != System.Windows.WindowState.Minimized)
                    {
                        NativeMethods.ShowWindow(new System.Windows.Interop.WindowInteropHelper(window).Handle, NativeMethods.SW_MINIMIZE);

                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
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
                try
                {
                    MainWindow.Visibility = System.Windows.Visibility.Visible;
                    if (MainWindow.WindowState == System.Windows.WindowState.Minimized || MainWindow.Visibility == System.Windows.Visibility.Hidden)
                    {
                        MainWindow.Show();
                        MainWindow.Visibility = System.Windows.Visibility.Visible;
                        Restore(Handle);
                        ActivateWindow(MainWindow);
                        MainWindow.Activate();
                        MainWindow.Focus();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }
        public static void Restore(System.Windows.Window window)
        {
            RunUI(() =>
            {
                try
                {
                    window.Visibility = System.Windows.Visibility.Visible;
                    if (window.WindowState == System.Windows.WindowState.Minimized || MainWindow.Visibility == System.Windows.Visibility.Hidden)
                    {
                        window.Visibility = System.Windows.Visibility.Visible;
                        IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                        Restore(hWnd);
                        MainWindow.Activate();
                        MainWindow.Focus();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
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
        public static async Task RunUIAsync(Func<Task> action)
        {
            await RunUIAsync(MainWindow, action);
        }
        public static async Task RunUIAsync(System.Windows.Window window, Func<Task> action)
        {
            if (window != null)
            {
                await window.Dispatcher.Invoke(action);
            }
            else
            {
                await action();
            }
        }
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
            AutomationHelper.syncContext.Send(o =>
            {
                action();
            }, null);
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
        public static void ActivateWindow(System.Windows.Window window)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();

            var threadId1 = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            var threadId2 = GetWindowThreadProcessId(hwnd, IntPtr.Zero);

            if (threadId1 != threadId2)
            {
                AttachThreadInput(threadId1, threadId2, true);
                SetForegroundWindow(hwnd);
                AttachThreadInput(threadId1, threadId2, false);
            }
            else
                SetForegroundWindow(hwnd);
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }
}
