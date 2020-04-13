using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenRPA.Views
{
    public class ClosableTab : TabItem
    {
        public string Title
        {
            set
            {
                try
                {
                    ((CloseableHeader)this.Header).label_TabTitle.Content = value;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

        public event RoutedEventHandler OnClose;
        public CloseableHeader closableTabHeader { get; set; }
        public ClosableTab()
        {
            Log.FunctionIndent("ClosableTab", "ClosableTab");
            try
            {
                // Create an instance of the usercontrol
                closableTabHeader = new CloseableHeader();
                // Assign the usercontrol to the tab header
                this.Header = closableTabHeader;
                closableTabHeader.button_close.MouseEnter += new MouseEventHandler(button_close_MouseEnter);
                closableTabHeader.button_close.MouseLeave += new MouseEventHandler(button_close_MouseLeave);
                closableTabHeader.button_close.Click += new RoutedEventHandler(button_close_Click);
                closableTabHeader.label_TabTitle.SizeChanged += new SizeChangedEventHandler(label_TabTitle_SizeChanged);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("ClosableTab", "ClosableTab");
        }
        // Button MouseEnter - When the mouse is over the button - change color to Red
        void button_close_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                ((CloseableHeader)this.Header).button_close.Foreground = Brushes.Red;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        // Button MouseLeave - When mouse is no longer over button - change color back to black
        void button_close_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                ((CloseableHeader)this.Header).button_close.Foreground = Brushes.Black;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        // Button Close Click - Remove the Tab - (or raise
        // an event indicating a "CloseTab" event has occurred)
        void button_close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnClose?.Invoke(this, e);
                if (e.Handled) return;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }   
        }
        public void Close()
        {
            try
            {
                ((TabControl)this.Parent).Items.Remove(this);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        // Label SizeChanged - When the Size of the Label changes
        // (due to setting the Title) set position of button properly
        void label_TabTitle_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                ((CloseableHeader)this.Header).button_close.Margin = new Thickness(
                   ((CloseableHeader)this.Header).label_TabTitle.ActualWidth + 5, 3, 4, 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        protected override void OnSelected(RoutedEventArgs e)
        {
            try
            {
                base.OnSelected(e);
                ((CloseableHeader)this.Header).button_close.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        protected override void OnUnselected(RoutedEventArgs e)
        {
            try
            {
                base.OnUnselected(e);
                ((CloseableHeader)this.Header).button_close.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            try
            {
                base.OnMouseEnter(e);
                ((CloseableHeader)this.Header).button_close.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            try
            {
                base.OnMouseLeave(e);
                if (!this.IsSelected)
                {
                    ((CloseableHeader)this.Header).button_close.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
