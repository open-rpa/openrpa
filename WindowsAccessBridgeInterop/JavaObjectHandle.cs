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
using System.Runtime.InteropServices;
using System.Threading;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Handle for objects returned from the WindowAccessBridge-XX dll.
  /// This ensures that all java objects references are released when not used
  /// by any C# object.
  /// 
  /// Note we cannot use the <see cref="SafeHandle"/> class, because
  /// the 32-bit version of WindowAccessBridge uses 64-bit pointers
  /// for object handles (<see cref="SafeHandle"/> uses the <see cref="IntPtr"/>
  /// type, which is 32-bit on 32-bit platforms).
  /// </summary>
  public class JavaObjectHandle : IDisposable {
    private readonly int _jvmId;
    private readonly JOBJECT64 _handle;
    private readonly bool _isLegacy;
    private bool _disposed;
    /// <summary>
    /// Counter of the number of object handles that have been wrapped into
    /// a <see cref="JavaObjectHandle"/> and not yet released either by the
    /// GC or by calling <see cref="Dispose()"/>.
    /// </summary>
    private static long _activeCount;
    /// <summary>
    /// Note: It looks like the windows access bridge DLL is not quite
    /// multi-thread safe, so we use a queue to store java objects to be
    /// released. The queue must be flushed by calling <see
    /// cref="FlushReleaseQueue"/>.
    /// </summary>
    private static List<ReleaseData> _releaseQueue = new List<ReleaseData>();

    public JavaObjectHandle(int jvmId, JOBJECT64 handle) {
      _jvmId = jvmId;
      _handle = handle;
      _isLegacy = false;
      if (handle.Value == 0) {
        GC.SuppressFinalize(this);
      }
      RecordActivation(handle);
    }

    public JavaObjectHandle(int jvmId, JOBJECT32 handle)
      : this(jvmId, new JOBJECT64(handle.Value)) {
    }

    ~JavaObjectHandle() {
      Dispose(false);
    }

    public int JvmId {
      get { return _jvmId; }
    }

    public JOBJECT64 Handle {
      get { return _handle; }
    }

    public JOBJECT32 HandleLegacy {
      get { return new JOBJECT32((Int32)_handle.Value); }
    }

    public bool IsClosed {
      get { return _disposed; }
    }

    public bool IsNull {
      get { return _handle.Value == 0; }
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing) {
      if (_disposed)
        return;
      RecordDeactivation(_handle);
      EnqueueRelease(_jvmId, _handle, _isLegacy);
      _disposed = true;
    }

    private static void RecordActivation(JOBJECT64 handle) {
      if (handle.Value != 0) {
        Interlocked.Increment(ref _activeCount);
      }
    }

    private static void RecordDeactivation(JOBJECT64 handle) {
      if (handle.Value != 0) {
        Interlocked.Decrement(ref _activeCount);
      }
    }

    public static long ActiveContextCount {
      get { return _activeCount; }
    }

    public static long InactiveContextCount {
      get { return _releaseQueue.Count; }
    }

    private struct ReleaseData {
      public int JvmId;
      public JOBJECT64 Handle;
      public bool IsLegacy;
    }

    private static void EnqueueRelease(int jvmid, JOBJECT64 handle, bool isLegacy) {
      // Skip NULL handles, they don't need to be released.
      if (handle.Value == 0)
        return;

      lock (_releaseQueue) {
        _releaseQueue.Add(new ReleaseData {
          JvmId = jvmid,
          Handle = handle,
          IsLegacy = isLegacy
        });
      }
    }

    /// <summary>
    /// Release the java objects that are not in use anymore. Must be called on
    /// the same thread that uses the Windows Access Bridge (typically the Main
    /// UI thread).
    /// </summary>
    public static int FlushReleaseQueue() {
      if (_releaseQueue == null)
        return 0;

      List<ReleaseData> temp = null;
      lock (_releaseQueue) {
        if (_releaseQueue.Count > 0) {
          temp = _releaseQueue;
          _releaseQueue = new List<ReleaseData>();
        }
      }

      if (temp == null)
        return 0;

      int count = 0;
      foreach (var x in temp) {
        ReleaseJavaObject(x.JvmId, x.Handle, x.IsLegacy);
        count++;
      }
      return count;
    }

    private static void ReleaseJavaObject(int jvmid, JOBJECT64 handle, bool isLegacy) {
      Debug.Assert(handle.Value != 0);

      // Note: We need the "ReleaseXxx" method to be static, as we can't depend
      // on any other managed object for calling into the WindowsAccessBridge
      // DLL. Also, we depend on the fact the CLR tries to load the method from
      // the DLL only when the method is actually called. This allows us to work
      // correctly on either a 64-bit or 32-bit.
      if (isLegacy) {
        ReleaseJavaObjectFP_Legacy(jvmid, (int)handle.Value);
      } else {
        if (IntPtr.Size == 4) {
          ReleaseJavaObjectFP_32(jvmid, handle.Value);
        } else {
          ReleaseJavaObjectFP_64(jvmid, handle.Value);
        }
      }
    }

    [DllImport("WindowsAccessBridge.dll", EntryPoint = "releaseJavaObject", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ReleaseJavaObjectFP_Legacy(int jvmId, Int32 javaObject);

    [DllImport("WindowsAccessBridge-32.dll", EntryPoint = "releaseJavaObject", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ReleaseJavaObjectFP_32(int jvmId, Int64 javaObject);

    [DllImport("WindowsAccessBridge-64.dll", EntryPoint = "releaseJavaObject", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ReleaseJavaObjectFP_64(int jvmId, Int64 javaObject);
  }
}