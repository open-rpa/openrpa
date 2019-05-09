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

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// A list of <see cref="PropertyNode"/>.
  /// </summary>
  public class PropertyList : List<PropertyNode> {
    public PropertyNode AddProperty(string name, object value) {
      var prop = new PropertyNode(name, value);
      Add(prop);
      return prop;
    }

    public PropertyGroup AddGroup(string name, object value = null) {
      var group = new PropertyGroup(name, value);
      Add(group);
      return group;
    }
  }
}