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
using System.Drawing;
using System.Linq;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Base class of all accessible nodes exposed by the <see cref="AccessBridge"/>.
  /// </summary>
  public abstract class AccessibleNode : IEquatable<AccessibleNode>, IDisposable {
    private readonly AccessBridge _accessBridge;

    public AccessibleNode(AccessBridge accessBridge) {
      _accessBridge = accessBridge;
    }

    public AccessBridge AccessBridge {
      get { return _accessBridge; }
    }

    public abstract int JvmId { get; }

    public virtual void Dispose() {
      // Nothing by default
    }

    public abstract AccessibleNode GetParent();


    /// <summary>
    /// Return true if the node is valid for a short period of time only,
    /// e.g. if the parent has the "manages descendants" state.
    /// </summary>
    public virtual bool IsManagedDescendant { get { return false; } }

    protected abstract int GetChildrenCount();

    protected abstract AccessibleNode GetChildAt(int i);

    public IEnumerable<AccessibleNode> GetChildren() {
      return Enumerable.Range(0, GetChildrenCount()).Select(i => GetChildAt(i));
    }

    public abstract string GetTitle();

    /// <summary>
    /// Returns a <see cref="PropertyList"/> containings all the properties of
    /// this node. Properties are split in groups. <paramref name="options"/>
    /// determines which properties/group to include. This is required because
    /// some properties are expensive to retrieve (e.g. flattened list of
    /// descendents).
    /// </summary>
    public PropertyList GetProperties(PropertyOptions options) {
      var result = new PropertyList();
      AddProperties(result, options);
      return result;
    }

    /// <summary>
    /// Returns a <see cref="PropertyList"/> suitable for displaying in a tool
    /// tip window.
    /// </summary>
    public PropertyList GetToolTipProperties(PropertyOptions options) {
      var result = new PropertyList();
      AddToolTipProperties(result, options);
      return result;
    }

    protected virtual void AddProperties(PropertyList list, PropertyOptions options) {
    }

    protected virtual void AddToolTipProperties(PropertyList list, PropertyOptions options) {
    }

    /// <summary>
    /// Return the screen rectangle of this node. Return <code>null</code> if
    /// the node does not have any screen location information. Note that the
    /// returned rectangle may be outside of the bounds of the physical screen
    /// because either the node is virtualized inside a scrollable region (e.g.
    /// an editor) or because there are multiple monitors attached to the
    /// computer.
    /// </summary>
    public virtual Rectangle? GetScreenRectangle() {
      return null;
    }

    /// <summary>
    /// Return the <see cref="Path{AccessibleNode}"/> of a node given a location on screen.
    /// Return <code>null</code> if there is no node at that location.
    /// </summary>
    public virtual Path<AccessibleNode> GetNodePathAt(Point screenPoint) {
      // Bail early if this node is not visible
      var rectangle = GetScreenRectangle();
      if (rectangle == null)
        return null;

      // Look for candidate children first
      var childPaths = GetChildren()
        .Select(x => x.GetNodePathAt(screenPoint))
        .Where(x => x != null)
        .OrderBy(x => {
          // Order by surface size so that smaller (i.e. more specific) nodes is picked first.
          var rect = x.Leaf.GetScreenRectangle();
          if (rect == null)
            return int.MaxValue;
          return rect.Value.Width * rect.Value.Height;
        })
        .ToList();

      // If no children, return our path if we contain the screenPoint.
      if (childPaths.Count == 0) {
        if (rectangle.Value.Contains(screenPoint)) {
          var path = new Path<AccessibleNode>();
          path.AddRoot(this);
          return path;
        }
        return null;
      } else {
        // Note: childPaths are ordered by ascending surface, so picking the
        // smallest one makes sense as it is most likely the most specific
        // result.
        var result = childPaths[0];
        result.AddRoot(this);
        return result;
      }
    }

    public override bool Equals(object obj) {
      return Equals(obj as AccessibleNode);
    }

    public override int GetHashCode() {
      // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
      return base.GetHashCode();
    }

    public virtual bool Equals(AccessibleNode other) {
      if (other == null)
        return false;

      return JvmId == other.JvmId;
    }

    /// <summary>
    /// Return the index of this node in its parent, <code>-1</code> if the
    /// value is unknown. This is useful when node equality does not work
    /// because transient children may be the "same" even through the Java
    /// instance is different.
    /// </summary>
    public virtual int GetIndexInParent() {
      return -1;
    }

    public virtual void Refresh() {
    }
  }
}