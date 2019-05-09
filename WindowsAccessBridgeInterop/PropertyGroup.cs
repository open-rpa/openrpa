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
using System.IO;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// A property node that can have children (as a <see cref="PropertyList"/>).
  /// </summary>
  public class PropertyGroup : PropertyNode {
    private readonly PropertyList _children = new PropertyList();

    public PropertyGroup(string name, object value = null) : base(name, value) {
    }

    public bool Expanded { get; set; }

    public event EventHandler<ErrorEventArgs> Error;

    public PropertyNode AddProperty(string name, object value) {
      return _children.AddProperty(name, value);
    }

    public PropertyGroup AddGroup(string name, object value = null) {
      return _children.AddGroup(name, value);
    }

    public Action LoadChildren { get; set; }

    public PropertyList Children {
      get {
        if (LoadChildren != null) {
          var temp = LoadChildren;
          LoadChildren = null;
          try {
            temp();
          } catch (Exception e) {
            OnError(new ErrorEventArgs(e));
          }
        }
        return _children;
      }
    }

    public override string ToString() {
      return string.Format("Group: {0}-{1}", Name, Value ?? "");
    }

    protected virtual void OnError(ErrorEventArgs e) {
      var handler = Error;
      if (handler != null)
        handler(this, e);
    }
  }
}