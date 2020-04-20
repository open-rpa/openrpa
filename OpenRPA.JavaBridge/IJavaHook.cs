using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenRPA.JavaBridge
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void JavaShutDownDelegate(System.Int32 vmID);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MouseClickedDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MouseEnteredDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MouseExitedDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MousePressedDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MouseReleasedDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
    interface IJavaHook
    {
        event JavaShutDownDelegate OnJavaShutDown;
        event MouseClickedDelegate OnMouseClicked;
        event MouseEnteredDelegate OnMouseEntered;
        event MouseExitedDelegate OnMouseExited;
        event MousePressedDelegate OnMousePressed;
        event MouseReleasedDelegate OnMouseReleased;
    }
}
