using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenRPA.JavaBridge
{
    public class JavaHook
    {
        public const String WinAccessBridgeDll = "windowsaccessbridge-64.dll";
        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void Windows_run();
        //public delegate void JavaBridgeInitializedDelegate();
        //public event JavaBridgeInitializedDelegate OnJavaBridgeInitialized;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setJavaShutdownFP(JavaShutDownDelegate fp);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void JavaShutDownDelegate(System.Int32 vmID);
        public event JavaShutDownDelegate OnJavaShutDown;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseClickedFP(MouseClickedDelegate fp);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MouseClickedDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
        public event MouseClickedDelegate OnMouseClicked;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseEnteredFP(MouseEnteredDelegate fp);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MouseEnteredDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
        public event MouseEnteredDelegate OnMouseEntered;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseExitedFP(MouseExitedDelegate fp);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MouseExitedDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
        public event MouseExitedDelegate OnMouseExited;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMousePressedFP(MousePressedDelegate fp);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MousePressedDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
        public event MousePressedDelegate OnMousePressed;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseReleasedFP(MouseReleasedDelegate fp);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MouseReleasedDelegate(System.Int32 vmID, IntPtr jevent, IntPtr ac);
        public event MouseReleasedDelegate OnMouseReleased;


        private JavaShutDownDelegate javaShutDownDelegate;
        private MouseClickedDelegate mouseClickedDelegate;
        private MouseEnteredDelegate mouseEnteredDelegate;
        private MouseExitedDelegate mouseExitedDelegate;
        private MousePressedDelegate mousePressedDelegate;
        private MouseReleasedDelegate mouseReleasedDelegate;
        public JavaHook()
        {
            Windows_run();
            javaShutDownDelegate = new JavaShutDownDelegate(_OnJavaShutDown);
            setJavaShutdownFP(javaShutDownDelegate);

            mouseClickedDelegate = new MouseClickedDelegate(_OnMouseClicked);
            setMouseClickedFP(mouseClickedDelegate);
            mouseEnteredDelegate = new MouseEnteredDelegate(_OnMouseEntered);
            setMouseEnteredFP(mouseEnteredDelegate);
            mouseExitedDelegate = new MouseExitedDelegate(_OnMouseExited);
            setMouseExitedFP(mouseExitedDelegate);

            mousePressedDelegate = new MousePressedDelegate(_OnMousePressed);
            setMousePressedFP(mousePressedDelegate);
            mouseReleasedDelegate = new MouseReleasedDelegate(_OnMouseReleased);
            setMouseReleasedFP(mouseReleasedDelegate);
        }
        private void _OnJavaShutDown(int vmID) => OnJavaShutDown?.Invoke(vmID);
        private void _OnMouseClicked(int vmID, IntPtr jevent, IntPtr ac) => OnMouseClicked?.Invoke(vmID, jevent, ac);
        private void _OnMouseEntered(int vmID, IntPtr jevent, IntPtr ac) => OnMouseEntered?.Invoke(vmID, jevent, ac);
        private void _OnMouseExited(int vmID, IntPtr jevent, IntPtr ac) => OnMouseExited?.Invoke(vmID, jevent, ac);
        private void _OnMousePressed(int vmID, IntPtr jevent, IntPtr ac) => OnMousePressed?.Invoke(vmID, jevent, ac);
        private void _OnMouseReleased(int vmID, IntPtr jevent, IntPtr ac) => OnMouseReleased?.Invoke(vmID, jevent, ac);

    }
}
