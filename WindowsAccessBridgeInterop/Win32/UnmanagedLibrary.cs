// Copyright 2015 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace WindowsAccessBridgeInterop.Win32 {
  /// <summary>
  /// Utility class to wrap an unmanaged DLL and be responsible for freeing it.
  /// </summary>
  /// <remarks>
  /// This is a managed wrapper over the native LoadLibrary, GetProcAddress, and
  /// FreeLibrary calls.
  /// </remarks>
  public class UnmanagedLibrary : IDisposable {
    // Unmanaged resource. CLR will ensure SafeHandles get freed, without requiring a finalizer on this class.
    private readonly SafeLibraryHandle _libraryHandle;

    /// <summary>
    /// Constructor to load a dll and be responible for freeing it.
    /// </summary>
    /// <param name="fileName">full path name of dll to load</param>
    /// <exception cref="System.IO.FileNotFoundException">if fileName can't be found</exception>
    /// <remarks>Throws exceptions on failure. Most common failure would be file-not-found, or
    /// that the file is not a  loadable image.</remarks>
    public UnmanagedLibrary(string fileName) {
      _libraryHandle = NativeMethods.LoadLibrary(fileName);
      if (_libraryHandle.IsInvalid) {
        try {
          var hr = Marshal.GetHRForLastWin32Error();
          Marshal.ThrowExceptionForHR(hr);
        } catch (Exception e) {
          throw new Exception(string.Format("Error loading library \"{0}\"", fileName), e);
        }
      }
    }

    public string Path {
      get {
        if (_libraryHandle.IsInvalid) {
          throw new InvalidOperationException("Cannot retrieve path because no library has been loaded yet");
        }

        var sb = new StringBuilder(4096);
        var size = NativeMethods.GetModuleFileName(_libraryHandle, sb, (uint)sb.Capacity);
        if (size == 0) {
          throw new InvalidOperationException("Error retrieving library path");
        }

        return sb.ToString(0, (int)size);
      }
    }

    public FileVersionInfo Version {
      get { return FileVersionInfo.GetVersionInfo(Path); }
    }

    /// <summary>
    /// Dynamically lookup a function in the dll via kernel32!GetProcAddress.
    /// </summary>
    /// <param name="functionName">raw name of the function in the export table.</param>
    /// <returns>null if function is not found. Else a delegate to the unmanaged function.
    /// </returns>
    /// <remarks>GetProcAddress results are valid as long as the dll is not yet unloaded. This
    /// is very very dangerous to use since you need to ensure that the dll is not unloaded
    /// until after you're done with any objects implemented by the dll. For example, if you
    /// get a delegate that then gets an IUnknown implemented by this dll,
    /// you can not dispose this library until that IUnknown is collected. Else, you may free
    /// the library and then the CLR may call release on that IUnknown and it will crash.</remarks>
    public TDelegate GetUnmanagedFunction<TDelegate>(string functionName) where TDelegate : class {
      var p = NativeMethods.GetProcAddress(_libraryHandle, functionName);

      // Failure is a common case, especially for adaptive code.
      if (p == IntPtr.Zero) {
        return null;
      }
      Delegate function = Marshal.GetDelegateForFunctionPointer(p, typeof(TDelegate));

      // Ideally, we'd just make the constraint on TDelegate be
      // System.Delegate, but compiler error CS0702 (constrained can't be System.Delegate)
      // prevents that. So we make the constraint system.object and do the cast from object-->TDelegate.
      object o = function;

      return (TDelegate)o;
    }

    public Delegate GetUnmanagedFunction(string functionName, Type type) {
      var p = NativeMethods.GetProcAddress(_libraryHandle, functionName);

      // Failure is a common case, especially for adaptive code.
      if (p == IntPtr.Zero) {
        return null;
      }
      return Marshal.GetDelegateForFunctionPointer(p, type);
    }

    /// <summary>
    /// Call FreeLibrary on the unmanaged dll. All function pointers
    /// handed out from this class become invalid after this.
    /// </summary>
    /// <remarks>This is very dangerous because it suddenly invalidate
    /// everything retrieved from this dll. This includes any functions
    /// handed out via GetProcAddress, and potentially any objects returned
    /// from those functions (which may have an implemention in the
    /// dll).</remarks>
    public void Dispose() {
      if (!_libraryHandle.IsClosed) {
        _libraryHandle.Close();
      }
    }

    // See http://msdn.microsoft.com/msdnmag/issues/05/10/Reliability/ for more about safe handles.
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    private sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid {
      private SafeLibraryHandle() : base(true) { }

      protected override bool ReleaseHandle() {
        return NativeMethods.FreeLibrary(handle);
      }
    }

    static class NativeMethods {
      const string SKernel = "kernel32";

      [DllImport(SKernel, CharSet = CharSet.Auto, BestFitMapping = false, SetLastError = true)]
      public static extern SafeLibraryHandle LoadLibrary(string fileName);

      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
      [DllImport(SKernel, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool FreeLibrary(IntPtr hModule);

      [DllImport(SKernel)]
      public static extern IntPtr GetProcAddress(SafeLibraryHandle hModule, string procname);

      [DllImport(SKernel, CharSet = CharSet.Auto, BestFitMapping = false, SetLastError = true)]
      public static extern uint GetModuleFileName(
        [In]SafeLibraryHandle hModule,
        [Out]StringBuilder lpFilename,
        [In]uint nSize);
    }
  }
}