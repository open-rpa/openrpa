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

using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OpenRPA.Interfaces.Overlay
{
    /// <summary>
    /// Various utilities to position windows relative to other windows taking
    /// into account multi-monitor setups if needed.
    /// </summary>
    public static class ScreenUtils
    {
        /// <summary>
        /// Given a rectangle returned by the Access Bridge API, return the closest
        /// possible rectangle that fits into bounds of one of the attached monitor.
        /// </summary>
        public static Rectangle FitToScreen(Rectangle rect)
        {
            var bounds = Screen.GetBounds(rect);
            return FitToBounds(bounds, rect);
        }

        /// <summary>
        /// Given a <paramref name="bounds"/> rectangle, return the closest possible
        /// rectangle that fits into it.
        /// 
        /// The input rectangle should be considered "user-input", i.e. the X and/or
        /// Y coordinates are usually set to "-1" to indicate "no location". Also,
        /// the width and height may also be set to "-1" to invicate an invalid
        /// rectangle.
        /// 
        /// Finally, for large components inside scroll panes, the coordinates
        /// returned by the Access Bridge API corresponding to the "virtual" size of
        /// the component, meaning the X and/Y coordinates maybe negative, and/or
        /// the width/height values may be much larger than the actual screen size.
        /// </summary>
        public static Rectangle FitToBounds(Rectangle bounds, Rectangle rect)
        {
            // By setting width/height to 1, we ensure we display "something"
            // if the x/y coordinates point to a visible location.
            if (rect.Width < 0)
            {
                rect.Width = 1;
            }
            if (rect.Height < 0)
            {
                rect.Height = 1;
            }
            bounds.Intersect(rect);
            return bounds;
        }

        /// <summary>
        /// Return a location that is guaranteed to not overlap with any screen
        /// monitors attached to the system. This location is useful to position
        /// windows so that they are not visible.
        /// </summary>
        public static Point InvisibleLocation()
        {
            var screen = Screen.AllScreens
                .OrderByDescending(x => x.Bounds.X)
                .ThenByDescending(x => x.Bounds.Y)
                .First();

            return new Point(screen.Bounds.X - 10, screen.Bounds.Y - 10);
        }

        public static Point EnsureVisible(Point point)
        {
            var bounds = Screen.GetBounds(point);
            var result = point;
            if (point.X < bounds.Left) result.X = bounds.Left;
            if (point.X > bounds.Right) result.X = bounds.Right;
            if (point.Y < bounds.Top) result.Y = bounds.Top;
            if (point.Y > bounds.Bottom) result.Y = bounds.Bottom;
            return result;
        }
    }
}
