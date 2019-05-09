using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpenRPA.JavaBridge
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void MainWindow_Load(object sender, EventArgs e)
        {

        }
        private bool allowshowdisplay = false;
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }
        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            this.allowshowdisplay = true;
            this.Visible = !this.Visible;
        }
        public void AddText(string message)
        {

            SafeInvoke(textBox1, () =>
            {
                if (!message.EndsWith(Environment.NewLine)) message += Environment.NewLine;
                var timestring = DateTime.Now.ToString("[HH:mm:ss] ");
                textBox1.Text = (timestring + message) + textBox1.Text;
                //textBox1.Text += (timestring + message);
                //textBox1.SelectionStart = textBox1.TextLength;
                //textBox1.ScrollToCaret();
            }, false);
        }
        public static void SafeInvoke(Control uiElement, Action updater, bool forceSynchronous)
        {
            if (uiElement == null)
            {
                throw new ArgumentNullException("uiElement");
            }

            if (uiElement.InvokeRequired)
            {
                if (forceSynchronous)
                {
                    uiElement.Invoke((Action)delegate { SafeInvoke(uiElement, updater, forceSynchronous); });
                }
                else
                {
                    uiElement.BeginInvoke((Action)delegate { SafeInvoke(uiElement, updater, forceSynchronous); });
                }
            }
            else
            {
                if (uiElement.IsDisposed)
                {
                    //throw new ObjectDisposedException("Control is already disposed.");
                    return;
                }

                try
                {
                    updater();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                }
            }
        }
    }
}
