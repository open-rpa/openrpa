using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.Interfaces.Overlay
{
    public partial class OverlayWindow : Form
    {
        private bool ClickThrough = true;
        public OverlayWindow(bool ClickThrough)
        {
            this.ClickThrough = ClickThrough;
            InitializeComponent();
            AllowTransparency = true;
            Opacity = 0.5;
        }
        public void SetTimeout(TimeSpan closeAfter)
        {
            tmr = new Timer();
            tmr.Tick += Tmr_Tick; ;
            tmr.Interval = (int)closeAfter.TotalMilliseconds;
            tmr.Start();
        }
        public void setLocation(Rectangle rect)
        {
            GenericTools.RunUI(this, () => {
                Bounds = rect;
                TopMost = true;
                // this.SetBounds(rect.X, rect.Y, rect.Width, rect.Height);
            });
        }
        private void Tmr_Tick(object sender, EventArgs e)
        {
            try
            {
                Close();
                if (tmr != null)
                {
                    tmr.Tick -= Tmr_Tick;
                    tmr.Dispose();
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var result = base.CreateParams;
                if (!ClickThrough) return result;
                 result.ExStyle |= (int)win32.WindowStylesEx.WS_EX_TOOLWINDOW;
                result.ExStyle |= (int)win32.WindowStylesEx.WS_EX_TRANSPARENT;
                result.ExStyle |= (int)win32.WindowStylesEx.WS_EX_NOACTIVATE;
                result.ExStyle |= (int)win32.WindowStylesEx.WS_EX_LAYERED;
                return result;
            }
        }
        protected override void CreateHandle()
        {
            try
            {
                base.CreateHandle();
            }
            catch (Exception)
            {
                return;
            }
            // Note: We need this because the Form.TopMost property does not respect
            // the "ShowWithoutActivation" flag, meaning the window will steal the
            // focus every time it is made visible.
            SetTopMost(new HandleRef(this, Handle), true);
        }
        private System.Windows.Forms.Timer tmr;
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public static void SetTopMost(HandleRef handleRef, bool value)
        {
            var key = value ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST;
            var result = NativeMethods.SetWindowPos(handleRef, key, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            if (!result)
            {
                throw NativeMethods.GetLastWin32Error("SetTopMost:SetWindowPos");
            }
        }
    }

}
