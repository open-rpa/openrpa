using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenRPA.JavaBridge
{
    public class JavaHook_64 : IJavaHook
    {

        private JavaShutDownDelegate javaShutDownDelegate;
        private MouseClickedDelegate mouseClickedDelegate;
        private MouseEnteredDelegate mouseEnteredDelegate;
        private MouseExitedDelegate mouseExitedDelegate;
        private MousePressedDelegate mousePressedDelegate;
        private MouseReleasedDelegate mouseReleasedDelegate;
        public JavaHook_64()
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
        public const String WinAccessBridgeDll = "windowsaccessbridge-64.dll";
        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void Windows_run();
        //public delegate void JavaBridgeInitializedDelegate();
        //public event JavaBridgeInitializedDelegate OnJavaBridgeInitialized;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setJavaShutdownFP(JavaShutDownDelegate fp);
        public event JavaShutDownDelegate OnJavaShutDown;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseClickedFP(MouseClickedDelegate fp);
        public event MouseClickedDelegate OnMouseClicked;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseEnteredFP(MouseEnteredDelegate fp);
        public event MouseEnteredDelegate OnMouseEntered;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseExitedFP(MouseExitedDelegate fp);
        public event MouseExitedDelegate OnMouseExited;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMousePressedFP(MousePressedDelegate fp);
        public event MousePressedDelegate OnMousePressed;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseReleasedFP(MouseReleasedDelegate fp);
        public event MouseReleasedDelegate OnMouseReleased;
        private void _OnJavaShutDown(int vmID) => OnJavaShutDown?.Invoke(vmID);
        private void _OnMouseClicked(int vmID, IntPtr jevent, IntPtr ac) => OnMouseClicked?.Invoke(vmID, jevent, ac);
        private void _OnMouseEntered(int vmID, IntPtr jevent, IntPtr ac) => OnMouseEntered?.Invoke(vmID, jevent, ac);
        private void _OnMouseExited(int vmID, IntPtr jevent, IntPtr ac) => OnMouseExited?.Invoke(vmID, jevent, ac);
        private void _OnMousePressed(int vmID, IntPtr jevent, IntPtr ac) => OnMousePressed?.Invoke(vmID, jevent, ac);
        private void _OnMouseReleased(int vmID, IntPtr jevent, IntPtr ac) => OnMouseReleased?.Invoke(vmID, jevent, ac);

    }


    public class JavaHook_32 : IJavaHook
    {

        private JavaShutDownDelegate javaShutDownDelegate;
        private MouseClickedDelegate mouseClickedDelegate;
        private MouseEnteredDelegate mouseEnteredDelegate;
        private MouseExitedDelegate mouseExitedDelegate;
        private MousePressedDelegate mousePressedDelegate;
        private MouseReleasedDelegate mouseReleasedDelegate;
        public JavaHook_32()
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
        public const String WinAccessBridgeDll = "WindowsAccessBridge-32.dll";
        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void Windows_run();
        //public delegate void JavaBridgeInitializedDelegate();
        //public event JavaBridgeInitializedDelegate OnJavaBridgeInitialized;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setJavaShutdownFP(JavaShutDownDelegate fp);
        public event JavaShutDownDelegate OnJavaShutDown;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseClickedFP(MouseClickedDelegate fp);
        public event MouseClickedDelegate OnMouseClicked;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseEnteredFP(MouseEnteredDelegate fp);
        public event MouseEnteredDelegate OnMouseEntered;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseExitedFP(MouseExitedDelegate fp);
        public event MouseExitedDelegate OnMouseExited;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMousePressedFP(MousePressedDelegate fp);
        public event MousePressedDelegate OnMousePressed;

        [DllImport(WinAccessBridgeDll, SetLastError = true, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public extern static void setMouseReleasedFP(MouseReleasedDelegate fp);
        public event MouseReleasedDelegate OnMouseReleased;
        private void _OnJavaShutDown(int vmID) => OnJavaShutDown?.Invoke(vmID);
        private void _OnMouseClicked(int vmID, IntPtr jevent, IntPtr ac) => OnMouseClicked?.Invoke(vmID, jevent, ac);
        private void _OnMouseEntered(int vmID, IntPtr jevent, IntPtr ac) => OnMouseEntered?.Invoke(vmID, jevent, ac);
        private void _OnMouseExited(int vmID, IntPtr jevent, IntPtr ac) => OnMouseExited?.Invoke(vmID, jevent, ac);
        private void _OnMousePressed(int vmID, IntPtr jevent, IntPtr ac) => OnMousePressed?.Invoke(vmID, jevent, ac);
        private void _OnMouseReleased(int vmID, IntPtr jevent, IntPtr ac) => OnMouseReleased?.Invoke(vmID, jevent, ac);

    }

}
