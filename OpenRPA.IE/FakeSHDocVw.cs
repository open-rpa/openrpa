using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    public class FakeSHDocVw
    {
        const int FormWidth = 800;
        const int FormHeight = 600;

        // You can also use VirtualScreen
        // System.Windows.Forms.SystemInformation.VirtualScreen.Width;
        // System.Windows.Forms.SystemInformation.VirtualScreen.Height;

        // http://www.c-sharpcorner.com/forums/thread/51998/opening-a-browser-and-hiding-the-address-bar-help.aspx
        // http://weblog.west-wind.com/posts/2005/Apr/29/Previewing-HTML-with-InternetExplorerApplication-in-C
        // http://social.msdn.microsoft.com/Forums/vstudio/en-US/ab6969c7-0a34-4d88-9a74-b66888d3d88f/ie-automation-navigating-the-soup

        // http://superuser.com/questions/459775/how-can-i-launch-a-browser-with-no-window-frame-or-tabs-address-bar
        public void OpenBrowserWindow(string strURL)
        {
            // System.Diagnostics.Process.Start(strURL)
            // For Internet Explorer you can use -k (kiosk mode):
            // iexplore.exe -k http://www.google.com/
            // Internet Explorer Command-Line Options 
            // http://msdn.microsoft.com/en-us/library/ie/hh826025(v=vs.85).aspx
            // Starts Internet Explorer in kiosk mode. The browser opens in a maximized window that does not display the address bar, 
            // the navigation buttons, or the status bar.

            System.Drawing.Rectangle rect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            // http://stackoverflow.com/questions/5049122/capture-the-screen-shot-using-net
            int posX = rect.Width - FormWidth;
            int posY = rect.Height - FormHeight;
            posX = Convert.ToInt32(posX / 2.0);
            posY = Convert.ToInt32(posY / 2.0);

            posX = Math.Max(posX, 0);
            posY = Math.Max(posY, 0);



            System.Type oType = System.Type.GetTypeFromProgID("InternetExplorer.Application");

            object o = System.Activator.CreateInstance(oType);
            o.GetType().InvokeMember("MenuBar", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { 0 });
            o.GetType().InvokeMember("ToolBar", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { 0 });
            o.GetType().InvokeMember("StatusBar", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { 0 });
            o.GetType().InvokeMember("AddressBar", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { 0 });
            o.GetType().InvokeMember("Visible", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { true });


            o.GetType().InvokeMember("Top", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { posY });
            o.GetType().InvokeMember("Left", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { posX });
            o.GetType().InvokeMember("Width", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { FormWidth });
            o.GetType().InvokeMember("Height", System.Reflection.BindingFlags.SetProperty, null, o, new object[] { FormHeight });

            o.GetType().InvokeMember("Navigate", System.Reflection.BindingFlags.InvokeMethod, null, o, new object[] { strURL });

            try
            {
                object ohwnd = o.GetType().InvokeMember("hwnd", System.Reflection.BindingFlags.GetProperty, null, o, null);
                System.IntPtr IEHwnd = (System.IntPtr)ohwnd;
                // NativeMethods.SetForegroundWindow (IEHwnd);
                NativeMethods.ShowWindow(IEHwnd, NativeMethods.WindowShowStyle.ShowMaximized);
            }
            catch (Exception ex)
            {
            }

        } // OpenBrowserWindow



        public class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(
                System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(
                System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool ShowWindow(IntPtr hwnd, WindowShowStyle nCmdShow);


            /// <summary>Enumeration of the different ways of showing a window using
            /// ShowWindow</summary>
            public enum WindowShowStyle : int
            {
                /// <summary>Hides the window and activates another window.</summary>
                /// <remarks>See SW_HIDE</remarks>
                Hide = 0,
                /// <summary>Activates and displays a window. If the window is minimized
                /// or maximized, the system restores it to its original size and
                /// position. An application should specify this flag when displaying
                /// the window for the first time.</summary>
                /// <remarks>See SW_SHOWNORMAL</remarks>
                ShowNormal = 1,
                /// <summary>Activates the window and displays it as a minimized window.</summary>
                /// <remarks>See SW_SHOWMINIMIZED</remarks>
                ShowMinimized = 2,
                /// <summary>Activates the window and displays it as a maximized window.</summary>
                /// <remarks>See SW_SHOWMAXIMIZED</remarks>
                ShowMaximized = 3,
                /// <summary>Maximizes the specified window.</summary>
                /// <remarks>See SW_MAXIMIZE</remarks>
                Maximize = 3,
                /// <summary>Displays a window in its most recent size and position.
                /// This value is similar to "ShowNormal", except the window is not
                /// actived.</summary>
                /// <remarks>See SW_SHOWNOACTIVATE</remarks>
                ShowNormalNoActivate = 4,
                /// <summary>Activates the window and displays it in its current size
                /// and position.</summary>
                /// <remarks>See SW_SHOW</remarks>
                Show = 5,
                /// <summary>Minimizes the specified window and activates the next
                /// top-level window in the Z order.</summary>
                /// <remarks>See SW_MINIMIZE</remarks>
                Minimize = 6,
                /// <summary>Displays the window as a minimized window. This value is
                /// similar to "ShowMinimized", except the window is not activated.</summary>
                /// <remarks>See SW_SHOWMINNOACTIVE</remarks>
                ShowMinNoActivate = 7,
                /// <summary>Displays the window in its current size and position. This
                /// value is similar to "Show", except the window is not activated.</summary>
                /// <remarks>See SW_SHOWNA</remarks>
                ShowNoActivate = 8,
                /// <summary>Activates and displays the window. If the window is
                /// minimized or maximized, the system restores it to its original size
                /// and position. An application should specify this flag when restoring
                /// a minimized window.</summary>
                /// <remarks>See SW_RESTORE</remarks>
                Restore = 9,
                /// <summary>Sets the show state based on the SW_ value specified in the
                /// STARTUPINFO structure passed to the CreateProcess function by the
                /// program that started the application.</summary>
                /// <remarks>See SW_SHOWDEFAULT</remarks>
                ShowDefault = 10,
                /// <summary>Windows 2000/XP: Minimizes a window, even if the thread
                /// that owns the window is hung. This flag should only be used when
                /// minimizing windows from a different thread.</summary>
                /// <remarks>See SW_FORCEMINIMIZE</remarks>
                ForceMinimized = 11
            }

        }
    }
}
