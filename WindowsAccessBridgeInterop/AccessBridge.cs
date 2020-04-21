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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using WindowsAccessBridgeInterop.Win32;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Class used to dynamically load and access the Java Access Bridge DLL.
  /// </summary>
  public class AccessBridge : IDisposable {
    private AccessBridgeLibrary _library;
    private AccessBridgeFunctions _functions;
    private AccessBridgeEvents _events;
    private bool _disposed;

    public AccessBridge() {
      CollectionSizeLimit = 100;
      TextLineCountLimit = 200;
      TextLineLengthLimit = 200;
      TextBufferLengthLimit = 1024;
    }

    public AccessBridgeFunctions Functions {
      get {
        ThrowIfDisposed();
        Initialize(false);
        return _functions;
      }
    }

    public AccessBridgeEvents Events {
      get {
        ThrowIfDisposed();
        Initialize(false);
        return _events;
      }
    }

    public bool IsLoaded {
      get {
        return _library != null;
      }
    }

    public FileVersionInfo LibraryVersion {
      get {
        ThrowIfDisposed();
        Initialize(false);
        return _library.Version;
      }
    }

    public bool IsLegacy {
      get {
        ThrowIfDisposed();
        Initialize(false);
        return _library.IsLegacy;
      }
    }

    public int CollectionSizeLimit { get; set; }
    public int TextLineCountLimit { get; set; }
    public int TextLineLengthLimit { get; set; }
    public int TextBufferLengthLimit { get; set; }

    public event EventHandler Initilized;
    public event EventHandler Disposed;

    public void Initialize(bool ForceLegacy) {
      ThrowIfDisposed();
      if (_library != null)
        return;

      var library = LoadLibrary(ForceLegacy);
      if (library.IsLegacy) {
        var libraryFunctions = LoadEntryPointsLegacy(library);
        var functions = new AccessBridgeNativeFunctionsLegacy(libraryFunctions);
        var events = new AccessBridgeNativeEventsLegacy(libraryFunctions);

        // Everything is initialized correctly, save to member variables.
        _library = library;
        _functions = functions;
        _events = events;
      } else {
        var libraryFunctions = LoadEntryPoints(library);
        var functions = new AccessBridgeNativeFunctions(libraryFunctions);
        var events = new AccessBridgeNativeEvents(libraryFunctions);

        // Everything is initialized correctly, save to member variables.
        _library = library;
        _functions = functions;
        _events = events;
      }
      _functions.Windows_run();
      OnInitilized();
    }

    public void Dispose() {
      if (_disposed)
        return;

      if (_events != null) {
        _events.Dispose();
      }

      if (_library != null) {
        _library.Dispose();
        _functions = null;
        _library = null;
      }

      _disposed = true;
      OnDisposed();
    }

    private void ThrowIfDisposed() {
      if (_disposed)
        throw new ObjectDisposedException("Access Bridge library has been disposed");
    }

    public List<AccessibleJvm> EnumJvms() {
      return EnumJvms(hwnd => {
        return CreateAccessibleWindow(hwnd);
      });
    }

    public List<AccessibleJvm> EnumJvms(Func<IntPtr, AccessibleWindow> windowFunc) {
      if (_library == null)
        return new List<AccessibleJvm>();

      try {
        var windows = new List<AccessibleWindow>();
        var success = NativeMethods.EnumWindows((hWnd, lParam) => {
          var window = windowFunc(hWnd);
          if (window != null) {
            windows.Add(window);
          }
          return true;
        }, IntPtr.Zero);

        if (!success) {
          var hr = Marshal.GetHRForLastWin32Error();
          Marshal.ThrowExceptionForHR(hr);
        }

        // Group windows by JVM id
        return windows.GroupBy(x => x.JvmId).Select(g => {
          var result = new AccessibleJvm(this, g.Key);
          result.Windows.AddRange(g.OrderBy(x => x.GetDisplaySortString()));
          return result;
        }).OrderBy(x => x.JvmId).ToList();
      } catch (Exception e) {
        throw new ApplicationException("Error detecting running applications", e);
      }
    }

    public AccessibleWindow CreateAccessibleWindow(IntPtr hwnd) {
      if (!Functions.IsJavaWindow(hwnd))
        return null;

      int vmId;
      JavaObjectHandle ac;
            //Console.WriteLine("Value : {0:X}", hwnd);
            //Console.WriteLine("Value : {0:X}", hwnd.ToInt64());
            if (!Functions.GetAccessibleContextFromHWND(hwnd, out vmId, out ac))
        return null;

      return new AccessibleWindow(this, hwnd, ac);
    }

    public class AccessBridgeLibrary : UnmanagedLibrary {
      public AccessBridgeLibrary(string fileName) : base(fileName) {
      }

      public bool IsLegacy { get; set; }
    }

    private static AccessBridgeLibrary LoadLibrary(bool ForceLegacy) {
      try {
        AccessBridgeLibrary library;
                if(ForceLegacy)
                {
                library = new AccessBridgeLibrary("WindowsAccessBridge.dll");
                library.IsLegacy = true;
                }
                else if (IntPtr.Size == 4) {
          try {
            library = new AccessBridgeLibrary("WindowsAccessBridge-32.dll");
          } catch {
            try {
              library = new AccessBridgeLibrary("WindowsAccessBridge.dll");
              library.IsLegacy = true;
            } catch {
              // Ignore, we'll trow the initial exception
              library = null;
            }
            if (library == null)
              throw;
          }
        } else if (IntPtr.Size == 8) {
          library = new AccessBridgeLibrary("WindowsAccessBridge-64.dll");
        } else {
          throw new InvalidOperationException("Unknown platform.");
        }
        return library;
      } catch (Exception e) {
        var sb = new StringBuilder();
        sb.AppendLine("Error loading the Java Access Bridge DLL.");
        sb.AppendLine("This usually happens if the Java Access Bridge is not installed.");
        if (IntPtr.Size == 8) {
          sb.Append("Please make sure to install the 64-bit version of the " +
            "Java SE Runtime Environment version 7 or later. ");
          sb.AppendFormat("Alternatively, try running the 32-bit version of {0} " +
            "if a 32-bit version of the JRE is installed.", Assembly.GetEntryAssembly().GetName().Name);
        } else {
          sb.Append(
            "Please make sure to install the 32-bit version of the " +
            "Java SE Runtime Environment version 7 or later.");
        }
        throw new ApplicationException(sb.ToString(), e);
      }
    }

    private static AccessBridgeEntryPoints LoadEntryPoints(UnmanagedLibrary library) {
      var functions = new AccessBridgeEntryPoints();
      LoadEntryPointsImpl(library, functions);
      return functions;
    }

    private static AccessBridgeEntryPointsLegacy LoadEntryPointsLegacy(UnmanagedLibrary library) {
      var functions = new AccessBridgeEntryPointsLegacy();
      LoadEntryPointsImpl(library, functions);
      return functions;
    }

    private static void LoadEntryPointsImpl(UnmanagedLibrary library, object functions) {
      var publicMembers = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
      foreach (var property in functions.GetType().GetProperties(publicMembers)) {
        var name = property.Name;

        // All entry point names have lower case, except for "Windows_run"
        switch (name) {
          case "Windows_run":
            break;

          default:
            name = char.ToLower(name[0], CultureInfo.InvariantCulture) + name.Substring(1);
            break;
        }

        // Event setters have a "FP" suffix
        if (name.StartsWith("set") && name != "setTextContents") {
          name += "FP";
        }

        try {
          var function = library.GetUnmanagedFunction(name, property.PropertyType);
          if (function == null) {
            throw new ApplicationException(string.Format("Function {0} not found in AccessBridge", name));
          }
          property.SetValue(functions, function, null);
        } catch (Exception e) {
          throw new ArgumentException(string.Format("Error loading function {0} from access bridge library", name), e);
        }
      }
    }

    protected virtual void OnInitilized() {
      var handler = Initilized;
      if (handler != null) handler(this, EventArgs.Empty);
    }

    protected virtual void OnDisposed() {
      var handler = Disposed;
      if (handler != null) handler(this, EventArgs.Empty);
    }
  }
}
