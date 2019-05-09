// Copyright 2016 Google Inc. All Rights Reserved.
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Wrapper around a 32-bit integer. This makes code slightly more typesafe
  /// than using Int32 values directly.
  /// </summary>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct JOBJECT32 {
    public static JOBJECT32 Zero = default(JOBJECT32);

    private readonly int _value;

    public JOBJECT32(int value) {
      _value = value;
    }

    public int Value {
      get { return _value; }
    }

    public static bool operator ==(JOBJECT32 x, JOBJECT32 y) {
      return x._value == y._value;
    }

    public static bool operator !=(JOBJECT32 x, JOBJECT32 y) {
      return x._value == y._value;
    }

    public override bool Equals(object obj) {
      if (obj is JOBJECT32) {
        return this == (JOBJECT32) obj;
      }
      return false;
    }

    public override int GetHashCode() {
      return _value.GetHashCode();
    }
  }
}