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

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// A (name, value) pair.
  /// </summary>
  public class PropertyNode {
    private readonly string _name;
    private readonly object _value;

    public PropertyNode(string name, object value) {
      _name = name;
      _value = value;
    }

    public string Name {
      get { return _name; }
    }

    public object Value {
      get { return _value; }
    }
  }
}