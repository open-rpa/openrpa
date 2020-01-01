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
    public partial class TooltipWindow : Form
    {
        public TooltipWindow()
        {
            InitializeComponent();
            AllowTransparency = true;
            Opacity = 0.7;
            Visible = true;
            this.Label.AutoSize = true;
            this.Label.MaximumSize = new Size(140, 0);
        }
        public TooltipWindow(string text)
        {
            InitializeComponent();
            AllowTransparency = true;
            Opacity = 0.7;
            this.Label.Text = text;
            this.Label.AutoSize = true;
            this.Label.MaximumSize = new Size(140, 0);
            Visible = true;
        }
        public void SetTimeout(TimeSpan closeAfter)
        {
            tmr = new System.Windows.Forms.Timer();
            tmr.Tick += Tmr_Tick; ;
            tmr.Interval = (int)closeAfter.TotalMilliseconds;
            tmr.Start();
        }
        public void setLocation(System.Drawing.Rectangle rect)
        {
            GenericTools.RunUI(this, () => {
                this.Bounds = rect;
                this.TopMost = true;
                // this.SetBounds(rect.X, rect.Y, rect.Width, rect.Height);
            });
        }
        public void setText(string text)
        {
            GenericTools.RunUI(this, () => {
                this.Label.Text = text;
                // this.SetBounds(rect.X, rect.Y, rect.Width, rect.Height);
            });
        }
        private void Tmr_Tick(object sender, EventArgs e)
        {
            try
            {
                Close();
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
                result.ExStyle |= (int)WindowStylesEx.WS_EX_TOOLWINDOW;
                result.ExStyle |= (int)WindowStylesEx.WS_EX_TRANSPARENT;
                result.ExStyle |= (int)WindowStylesEx.WS_EX_NOACTIVATE;
                result.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
                return result;
            }
        }
        protected override void CreateHandle()
        {
            base.CreateHandle();
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

        public static HandleRef HWND_TOPMOST = new HandleRef(null, new IntPtr(-1));
        public static HandleRef HWND_NOTOPMOST = new HandleRef(null, new IntPtr(-2));
        public const int SWP_NOSIZE = 1;
        public const int SWP_NOMOVE = 2;
        public const int SWP_NOZORDER = 4;
        public const int SWP_NOACTIVATE = 16;
        public static void SetTopMost(HandleRef handleRef, bool value)
        {
            var key = value ? HWND_TOPMOST : HWND_NOTOPMOST;
            var result = SetWindowPos(handleRef, key, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            if (!result)
            {
                throw NativeMethods.GetLastWin32Error("SetTopMost:SetWindowPos");
            }
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool SetWindowPos(HandleRef hWnd, HandleRef hWndInsertAfter, int x, int y, int cx, int cy, int flags);
    }


}
