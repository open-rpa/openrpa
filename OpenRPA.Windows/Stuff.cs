using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    using FlaUI.Core;
    using FlaUI.Core.AutomationElements;
    using FlaUI.Core.AutomationElements.Infrastructure;
    using FlaUI.UIA3;
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

        public static class Win32WindowUtils
        {
            private static int CurrentProcessId = 0;
            public static AutomationElement[] GetTopLevelWindows(AutomationBase _automation)
            {
                var automation = (UIA3Automation)_automation;
                var result = new List<AutomationElement>();
                if (CurrentProcessId == 0) CurrentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
                const bool nextWindowPlz = true;
                EnumDelegate filter = delegate (IntPtr hWnd, int lParam)
                {
                    if (!IsWindowVisible(hWnd)) return nextWindowPlz;
                    uint foundThreadId = GetWindowThreadProcessId(hWnd, out uint foundProcessId);
                    // if (!(foundThreadId != 0 && foundProcessId != 0)) return nextWindowPlz;
                    if ((int)foundProcessId == CurrentProcessId) return nextWindowPlz;
                    try
                    {
                        var nativeAutomationElement = automation.NativeAutomation.ElementFromHandle(hWnd);
                        if (nativeAutomationElement == null) return nextWindowPlz;
                        var automationElement = automation.WrapNativeElement(nativeAutomationElement);
                        if(automationElement.Properties.IsOffscreen.IsSupported)
                        {
                            if (!automationElement.IsOffscreen) result.Add(automationElement);
                        } else
                        {
                            result.Add(automationElement);
                        }
                        
                        // return okDoneScanning;
                    }
                    catch
                    {
                        // boo UIAutomation, don't be like that
                    }

                    return nextWindowPlz;
                };
                EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero);
                return result.ToArray();
            }
            public static Window GetTopLevelWindow(UIA3Automation automation, int desiredProcessId, string desiredTitle = null)
            {
                const bool nextWindowPlz = true;
                const bool okDoneScanning = false;
                Window result = null;
                EnumDelegate filter = delegate (IntPtr hWnd, int lParam)
                {
                    if (!IsWindowVisible(hWnd)) return nextWindowPlz;
                    uint foundThreadId = GetWindowThreadProcessId(hWnd, out uint foundProcessId);
                    if (!(foundThreadId != 0 && foundProcessId != 0)) return nextWindowPlz;
                    if ((int)foundProcessId != desiredProcessId) return nextWindowPlz;
                    StringBuilder buffer = new StringBuilder(255);
                    int nLength = GetWindowText(hWnd, buffer, buffer.Capacity + 1);
                    if (nLength == 0) return nextWindowPlz;
                    string foundTitle = buffer.ToString();
                    if (foundTitle != desiredTitle) return nextWindowPlz;
                    try
                    {
                        var nativeAutomationElement = automation.NativeAutomation.ElementFromHandle(hWnd);
                        if (nativeAutomationElement == null) return nextWindowPlz;
                        var automationElement = automation.WrapNativeElement(nativeAutomationElement);
                        result = automationElement.AsWindow();
                        return okDoneScanning;
                    }
                    catch
                    {
                        // boo UIAutomation, don't be like that
                    }

                    return nextWindowPlz;
                };

                EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero);

                return result;
            }

            private delegate bool EnumDelegate(IntPtr hWnd, int lParam);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        }
}
