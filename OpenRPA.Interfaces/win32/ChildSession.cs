using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.win32
{
    public static class ChildSession
    {
        public static bool IsChildSessionsEnabled()
        {
            bool res;
            try
            {
                WTSIsChildSessionsEnabled(out res);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Failed to enable child session. Are you using Windows Server 2012/Windows 8 or newer?", exception);
            }
            return res;
        }
        public static void EnableChildSessions()
        {
            if (!WTSEnableChildSessions(true))
            {
                throw new InvalidOperationException("Failed to enable child session");
            }
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Terminal Server Client", true))
                {
                    key.SetValue("RemoteDesktop_SuppressWhenMinimized", 2);
                }
            }
            catch (Exception)
            {
            }
            //try
            //{
            //    var exepath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            //    {
            //        key.SetValue("openrpa", exepath);
            //    }
            //}
            //catch (Exception ex)
            //{
            //}
        }
        public static void DisableChildSessions()
        {
            if (!WTSEnableChildSessions(false))
            {
                throw new InvalidOperationException("Failed to disable child session");
            }
        }
        public static void LogOffChildSession()
        {
            uint SessionId;
            if (WTSGetChildSessionId(out SessionId) && !NativeMethods.WTSLogoffSession(IntPtr.Zero, (int)SessionId, true))
            {
                throw new Exception("Failed to logoff child session " + SessionId + " " + Marshal.GetLastWin32Error());
            }
        }
        public static void DisconnectChildSession()
        {
            uint SessionId;
            if (WTSGetChildSessionId(out SessionId) && !NativeMethods.WTSDisconnectSession(IntPtr.Zero, (int)SessionId, true))
            {
                throw new Exception("Failed to disconnect child session " + SessionId + " " + Marshal.GetLastWin32Error());
            }
        }
        public static uint GetChildSessionId()
        {
            uint SessionId;
            if (WTSGetChildSessionId(out SessionId))
            {
                return SessionId;
            }
            return 0;
            // throw new Exception("Failed to get child session " + Marshal.GetLastWin32Error());
        }
        // https://docs.microsoft.com/en-us/windows/win32/termserv/child-sessions?redirectedfrom=MSDN
        // https://stackoverflow.com/questions/31670504/windows-8-rdp-child-session-how-to-use
        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSIsChildSessionsEnabled(out bool isEnabled);
        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSEnableChildSessions(bool enable);
        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSGetChildSessionId(out uint SessionId);
    }
}
