/*
 * Taken from http://www.codeproject.com/KB/WPF/DockPanelSplitter.aspx and changed slightly by henon.
 * Original License: http://www.codeproject.com/info/cpol10.aspx
 * 
 * Copyright (C) 2009, objo
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the project nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/// Code Project article
/// http://www.codeproject.com/KB/WPF/DockPanelSplitter.aspx
/// 
/// CodePlex project
/// http://wpfcontrols.codeplex.com
///
/// DockPanelSplitter is a splitter control for DockPanels.
/// Add the DockPanelSplitter after the element you want to resize.
/// Set the DockPanel.Dock to define which edge the splitter should work on.
///
/// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
///
/// Step 1a) Using this custom control in a XAML file that exists in the current project.
/// Add this XmlNamespace attribute to the root element of the markup file where it is 
/// to be used:
///
///     xmlns:OsDps="clr-namespace:DockPanelSplitter"
///
///
/// Step 1b) Using this custom control in a XAML file that exists in a different project.
/// Add this XmlNamespace attribute to the root element of the markup file where it is 
/// to be used:
///
///     xmlns:MyNamespace="clr-namespace:DockPanelSplitter;assembly=DockPanelSplitter"
///
/// You will also need to add a project reference from the project where the XAML file lives
/// to this project and Rebuild to avoid compilation errors:
///
///     Right click on the target project in the Solution Explorer and
///     "Add Reference"->"Projects"->[Select this project]
///
///
/// Step 2)
/// Go ahead and use your control in the XAML file.
///
///     <MyNamespace:DockPanelSplitter/>
///

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenRPA.Interfaces.Selector
{
    /// <summary>
    /// DockPanelSplitter is a splitter control for DockPanels.
    /// Add the DockPanelSplitter after the element you want to resize.
    /// Set the DockPanel.Dock to define which edge the splitter should work on.
    /// </summary>
    public partial class DockPanelSplitter : Border
    {

        /// <summary>
        /// Resize the target element proportionally with the parent container
        /// Set to false if you don't want the element to be resized when the parent is resized.
        /// </summary>
        public bool ProportionalResize
        {
            get { return (bool)GetValue(ProportionalResizeProperty); }
            set { SetValue(ProportionalResizeProperty, value); }
        }

        public static readonly DependencyProperty ProportionalResizeProperty =
            DependencyProperty.Register("ProportionalResize", typeof(bool), typeof(DockPanelSplitter), new UIPropertyMetadata(true));


        #region Private fields
        private FrameworkElement m_element;     // element to resize (target element)
        private double m_width;                 // current desired width of the m_element, can be less than minwidth
        private double m_height;                // current desired height of the m_element, can be less than minheight
        private double m_previous_parent_width;   // current width of parent element, used for proportional resize
        private double m_previous_parent_height;  // current height of parent element, used for proportional resize
        #endregion

        public DockPanelSplitter()
        {
            InitializeComponent();
            Loaded += DockPanelSplitter_Loaded;
        }

        void DockPanelSplitter_Loaded(object sender, RoutedEventArgs e)
        {
            var dp = Parent as Panel;
            Debug.Assert(dp != null);
            // Subscribe to the parent's size changed event
            dp.SizeChanged += Parent_SizeChanged;

            // Store the current size of the parent DockPanel
            m_previous_parent_width = dp.ActualWidth;
            m_previous_parent_height = dp.ActualHeight;

            // Find the target element
            UpdateTargetElement();

        }

        /// <summary>
        /// Update the target element (the element the DockPanelSplitter works on)
        /// </summary>
        private void UpdateTargetElement()
        {
            var dp = Parent as Panel;
            Debug.Assert(dp != null);
            int i = dp.Children.IndexOf(this);

            // The splitter cannot be the first child of the parent DockPanel
            // The splitter works on the 'older' sibling 
            if (i > 0 && dp.Children.Count > 0)
            {
                m_element = dp.Children[i - 1] as FrameworkElement;
            }
        }

        private void SetTargetWidth(double new_width)
        {
            if (new_width < m_element.MinWidth)
                new_width = m_element.MinWidth;
            if (new_width > m_element.MaxWidth)
                new_width = m_element.MaxWidth;
            m_element.Width = new_width;
        }

        private void SetTargetHeight(double new_height)
        {
            if (new_height < m_element.MinHeight)
                new_height = m_element.MinHeight;
            if (new_height > m_element.MaxHeight)
                new_height = m_element.MaxHeight;
            m_element.Height = new_height;
        }

        private void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!ProportionalResize) return;

            var dp = Parent as DockPanel;
            Debug.Assert(dp != null);
            double sx = dp.ActualWidth / m_previous_parent_width;
            double sy = dp.ActualHeight / m_previous_parent_height;

            if (!double.IsInfinity(sx))
                SetTargetWidth(m_element.Width * sx);
            if (!double.IsInfinity(sy))
                SetTargetHeight(m_element.Height * sy);

            m_previous_parent_width = dp.ActualWidth;
            m_previous_parent_height = dp.ActualHeight;

        }

        double AdjustWidth(double dx, Dock dock)
        {
            if (dock == Dock.Right)
                dx = -dx;

            m_width += dx;
            SetTargetWidth(m_width);

            return dx;
        }

        double AdjustHeight(double dy, Dock dock)
        {
            if (dock == Dock.Bottom)
                dy = -dy;

            m_height += dy;
            SetTargetHeight(m_height);

            return dy;
        }

        Point m_start_drag_point;

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (!IsEnabled) return;
            var dock = DockPanel.GetDock(this);
            if (dock == Dock.Left || dock == Dock.Right)
            {
                Cursor = Cursors.SizeWE;
            }
            else
            {
                Cursor = Cursors.SizeNS;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!IsEnabled) return;

            if (!IsMouseCaptured)
            {
                m_start_drag_point = e.GetPosition(Parent as IInputElement);
                UpdateTargetElement();
                if (m_element != null)
                {
                    m_width = m_element.ActualWidth;
                    m_height = m_element.ActualHeight;
                    CaptureMouse();
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                var pt_current = e.GetPosition(Parent as IInputElement);
                var delta = new Point(pt_current.X - m_start_drag_point.X, pt_current.Y - m_start_drag_point.Y);
                var dock = DockPanel.GetDock(this);
                var is_vertical = (dock == Dock.Left || dock == Dock.Right);

                if (is_vertical)
                    delta.X = AdjustWidth(delta.X, dock);
                else
                    delta.Y = AdjustHeight(delta.Y, dock);

                var is_bottom_or_right = (dock == Dock.Right || dock == Dock.Bottom);

                // When docked to the bottom or right, the position has changed after adjusting the size
                if (is_bottom_or_right)
                    m_start_drag_point = e.GetPosition(Parent as IInputElement);
                else
                    m_start_drag_point = new Point(m_start_drag_point.X + delta.X, m_start_drag_point.Y + delta.Y);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
            base.OnMouseUp(e);
        }

    }
}