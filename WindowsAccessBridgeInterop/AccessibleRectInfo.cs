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

using System;
using System.Drawing;

namespace WindowsAccessBridgeInterop {
  public class AccessibleRectInfo : IEquatable<AccessibleRectInfo> {
    private readonly AccessibleContextNode _node;

    public AccessibleRectInfo(AccessibleContextNode node, int x, int y, int width, int height) {
      _node = node;
      X = x;
      Y = y;
      Width = width;
      Height = height;
    }

    public AccessibleRectInfo(AccessibleContextNode node, Point location, Size size) :
      this(node, location.X, location.Y, size.Width, size.Height) {
    }

    public AccessibleRectInfo(AccessibleContextNode node, AccessibleTextRectInfo rect) :
      this(node, rect.x, rect.y, rect.width, rect.height) {
    }

    public AccessibleRectInfo(AccessibleContextNode node, Rectangle rect) :
      this(node, rect.X, rect.Y, rect.Width, rect.Width) {
    }

    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Point Location {
      get { return new Point(X, Y); }
    }

    public Size Size {
      get {
        return new Size(Width, Height);
      }
    }

    public bool IsVisible {
      get { return Width >= 0 && Height >= 0; }
    }

    public Rectangle Rectangle {
      get { return new Rectangle(Location, Size); }
    }

    public AccessibleContextNode AccessibleNode {
      get { return _node; }
    }

    public override bool Equals(object obj) {
      return Equals(obj as AccessibleRectInfo);
    }

    public override int GetHashCode() {
      return Rectangle.GetHashCode();
    }

    public bool Equals(AccessibleRectInfo other) {
      if (other == null)
        return false;

      return Rectangle == other.Rectangle;
    }

    public override string ToString() {
      return Rectangle.ToString();
      //string.Format("[{0}, {1}, {2}, {3}]", X, Y, Width, Height);
    }
  }
}