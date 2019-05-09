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

using System.Collections.Generic;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Ability to move in a path, or in a subset of a path, without mutating
  /// the path itself.
  /// </summary>
  public class PathCursor<T> {
    private readonly List<T> _items;
    private readonly int _start;
    private readonly int _end;
    private int _index;

    public PathCursor(List<T> items, int start, int end) {
      _items = items;
      _start = start;
      _end = end;
      _index = start;
    }

    public T Node {
      get {
        if (_start <= _index && _index < _end) {
          return _items[_index];
        }
        return default(T);
      }
    }

    public bool IsValid {
      get { return Node != null; }
    }

    public PathCursor<T> Clone() {
      return new PathCursor<T>(_items, _index, _end);
    }

    public PathCursor<T> MoveNext() {
      if (_index < _end) {
        _index++;
      }
      return this;
    }

    public PathCursor<T> MovePrevious() {
      if (_index >= _start) {
        _index--;
      }
      return this;
    }
  }
}