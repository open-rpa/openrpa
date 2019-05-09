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

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Flags specifiying what properties to include in <see cref="AccessibleNode.GetProperties"/>.
  /// </summary>
  [Flags]
  public enum PropertyOptions {
    AccessibleContextInfo = 1 << 1,
    ObjectDepth = 1 << 2,
    ParentContext = 1 << 3,
    TopLevelWindowInfo = 1 << 4,
    ActiveDescendent = 1 << 5,
    VisibleChildren = 1 << 6,
    AccessibleActions = 1 << 7,
    AccessibleKeyBindings = 1 << 8,
    AccessibleIcons = 1 << 9,
    AccessibleRelationSet = 1 << 10,
    AccessibleText = 1 << 11,
    AccessibleHyperText = 1 << 12,
    AccessibleValue = 1 << 13,
    AccessibleSelection = 1 << 14,
    AccessibleTable = 1 << 15,
    AccessibleTableCells = 1 << 16,
    AccessibleTableCellsSelect = 1 << 17,
    Children = 1 << 18,
  }
}