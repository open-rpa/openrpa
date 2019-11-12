using FreeRDP;
using FreeRDP.Core;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.RDService
{
    public unsafe class RdpClient : Panel, IUpdate, IPrimaryUpdate
    {
        public void Attach(Control parent)
        {
            BackColor = System.Drawing.SystemColors.AppWorkspace;
            Parent = parent;
            Width = parent.ClientRectangle.Width;
            Height = parent.ClientRectangle.Height;
        }

        public RDP rdp;
        private ConnectionSettings settings;
        private Thread thread;
        private static bool procRunning = true;
        /**
		 * Instantiate RDP and Thread
		 */
        public RdpClient()
        {
            rdp = new RDP();
            thread = new Thread(() => ThreadProc(rdp));
        }
        public bool isConnected
        {
            get
            {
                if (rdp == null) return false;
                return rdp.Connected;
            }
        }
        /**
		 * Connect to FreeRDP server, start thread
		 */
        public void Connect(string hostname, string domain, string username, string password, int port = 3389, ConnectionSettings settings = null)
        {
            rdp.SetUpdateInterface(this);
            rdp.SetPrimaryUpdateInterface(this);

            this.settings = settings;

            rdp.Connect(hostname, domain, username, password, port, settings);

            procRunning = true;
            thread.Start();
        }
        /**
		 * Disconnect from FreeRDP server, stop thread
		 */
        public void Disconnect()
        {
            rdp.Disconnect();
            procRunning = false;
            thread = new Thread(() => ThreadProc(rdp));
        }
        public void OnMouseEvent(UInt16 pointerFlags, UInt16 x, UInt16 y)
        {
            rdp.SendInputMouseEvent(pointerFlags, x, y);
        }
        public void OnKeyboardEvent(UInt16 keyboardFlags, UInt16 keyCode)
        {
            var f = (KeyboardFlags)Enum.Parse(typeof(KeyboardFlags), keyboardFlags.ToString());
            rdp.SendInputKeyboardEvent(f, keyCode);
        }
        public void BeginPaint(rdpContext* context)
        {
            Log.Verbose("BeginPaint");
        }
        public void EndPaint(rdpContext* context)
        {
            Log.Verbose("EndPaint");
        }
        protected override void WndProc(ref Message msg)
        {
            switch (msg.Msg)
            {
                case 15://WM_PAINT
                    base.WndProc(ref msg);
                    Graphics g = Graphics.FromHwnd(Handle);
                    Pen pen = new Pen(Color.Red);
                    g.DrawRectangle(pen, 0, 0, 10, 10);
                    return;
            }
            base.WndProc(ref msg);
        }
        public void SetBounds(rdpContext* context, rdpBounds* bounds)
        {
            Log.Verbose("SetBounds");
        }
        public void Synchronize(rdpContext* context)
        {
            Log.Verbose("Synchronize");
        }
        public void DesktopResize(rdpContext* context)
        {
            Log.Verbose("DesktopResize");
        }
        public void BitmapUpdate(rdpContext* context, BitmapUpdate* bitmap)
        {
            Log.Verbose("BitmapUpdate");
        }
        public void Palette(rdpContext* context, PaletteUpdate* palette)
        {
            Log.Verbose("Palette");
        }
        public void PlaySound(rdpContext* context, PlaySoundUpdate* playSound)
        {
            Log.Verbose("PlaySound");
        }
        public void SurfaceBits(rdpContext* context, SurfaceBits* surfaceBits)
        {
            Log.Verbose("SurfaceBits");
            SurfaceBitsCommand cmd = new SurfaceBitsCommand();
            cmd.Read(surfaceBits);
        }
        public void DstBlt(rdpContext* context, DstBltOrder* dstblt)
        {
            Log.Verbose("DstBlt");
        }
        public void PatBlt(rdpContext* context, PatBltOrder* patblt)
        {
            Log.Verbose("PatBlt");
        }
        public void ScrBlt(rdpContext* context, ScrBltOrder* scrblt)
        {
            Log.Verbose("ScrBlt");
        }
        public void OpaqueRect(rdpContext* context, OpaqueRectOrder* opaqueRect)
        {
            Log.Verbose("OpaqueRect");
        }
        public void DrawNineGrid(rdpContext* context, DrawNineGridOrder* drawNineGrid)
        {
            Log.Verbose("DrawNineGrid");
        }
        public void MultiDstBlt(rdpContext* context, MultiDstBltOrder* multi_dstblt) { }
        public void MultiPatBlt(rdpContext* context, MultiPatBltOrder* multi_patblt) { }
        public void MultiScrBlt(rdpContext* context, MultiScrBltOrder* multi_scrblt) { }
        public void MultiOpaqueRect(rdpContext* context, MultiOpaqueRectOrder* multi_opaque_rect) { }
        public void MultiDrawNineGrid(rdpContext* context, MultiDrawNineGridOrder* multi_draw_nine_grid) { }
        public void LineTo(rdpContext* context, LineToOrder* line_to) { }
        public void Polyline(rdpContext* context, PolylineOrder* polyline) { }
        public void MemBlt(rdpContext* context, MemBltOrder* memblt) { }
        public void Mem3Blt(rdpContext* context, Mem3BltOrder* mem3blt) { }
        public void SaveBitmap(rdpContext* context, SaveBitmapOrder* save_bitmap) { }
        public void GlyphIndex(rdpContext* context, GlyphIndexOrder* glyph_index) { }
        public void FastIndex(rdpContext* context, FastIndexOrder* fast_index) { }
        public void FastGlyph(rdpContext* context, FastGlyphOrder* fast_glyph) { }
        public void PolygonSC(rdpContext* context, PolygonSCOrder* polygon_sc) { }
        public void PolygonCB(rdpContext* context, PolygonCBOrder* polygon_cb) { }
        public void EllipseSC(rdpContext* context, EllipseSCOrder* ellipse_sc) { }
        public void EllipseCB(rdpContext* context, EllipseCBOrder* ellipse_cb) { }
        static void ThreadProc(RDP rdp)
        {
            while (procRunning)
            {
                // rdp.CheckFileDescriptor();
                Thread.Sleep(10);
            }
        }
    }
}
