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

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// The root of all <see cref="AccessibleWindow"/> instances of a given Java
  /// Virtual Machine.
  /// </summary>
  public class AccessibleJvm : AccessibleNode {
    private readonly int _vmId;
    private readonly List<AccessibleWindow> _windows = new List<AccessibleWindow>();

    public AccessibleJvm(AccessBridge accessBridge, int vmId) : base(accessBridge) {
      _vmId = vmId;
    }

    public List<AccessibleWindow> Windows {
      get { return _windows; }
    }

    public override int JvmId {
      get { return _vmId; }
    }

    protected override int GetChildrenCount() {
      return _windows.Count;
    }

    protected override AccessibleNode GetChildAt(int i) {
      return _windows[i];
    }

    public override AccessibleNode GetParent() {
      return null;
    }

    public override string GetTitle() {
      var sb = new StringBuilder();
      _windows.ForEach(x => {
        var name = x.GetInfo().name;
        if (!string.IsNullOrEmpty(name)) {
          var maxLength = 60;
          if (name.Length > maxLength) {
            name = name.Substring(0, maxLength) + "...";
          }
          if (sb.Length > 0)
            sb.Append(", ");
          sb.Append('"');
          sb.Append(name);
          sb.Append('"');
        }
      });

      var legacyText = "";
      if (AccessBridge.IsLegacy) {
        legacyText = " [Legacy]";
      }

      if (sb.Length > 0) { 
        return string.Format("JVM {0}{1}: {2}", JvmId, legacyText, sb);
      } else {
        return string.Format("JVM {0}{1}", legacyText, JvmId);
      }
    }

    protected override void AddProperties(PropertyList list, PropertyOptions options) {
      list.AddProperty("JVM id", JvmId);
      AccessBridgeVersionInfo versionInfo;
      if (AccessBridge.Functions.GetVersionInfo(JvmId, out versionInfo)) {
        list.AddProperty("JVM version", versionInfo.VMversion);
        list.AddProperty("AccessBridge.class version", versionInfo.bridgeJavaClassVersion);
        list.AddProperty("JavaAccessBridge.dll version", versionInfo.bridgeJavaDLLVersion);
        list.AddProperty("WindowsAccessBridge.dll version", versionInfo.bridgeWinDLLVersion);
      }
      base.AddProperties(list, options);
    }

    /// <summary>
    /// Return the <see cref="Path{AccessibleNode}"/> of a node given a location on screen.
    /// Return <code>null</code> if there is no node at that location.
    /// </summary>
    public override Path<AccessibleNode> GetNodePathAt(Point screenPoint) {
      var windows = _windows.Select(x => x.GetNodePathAt(screenPoint)).Where(x => x != null).ToList();
      if (windows.Count == 0)
        return null;

      // Note: We should never have more than one window because
      // AccessibleWindow uses WindowFromPoint to filter out themselves if
      // needed.
      Debug.Assert(windows.Count == 1);
      var result = windows[0];
      result.AddRoot(this);
      return result;
    }
  }
}