
namespace OpenRPA.Image
{
    using System.Threading.Tasks;
    //using Emgu.CV;
    //using Emgu.CV.CvEnum;
    //using Emgu.CV.Structure;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using OpenRPA.Input;
    using OpenRPA.Interfaces;
    using Emgu.CV;
    using FlaUI.Core.AutomationElements;

    public static class getrectangle
    {

        private static System.Drawing.Rectangle rect;
        private static System.Drawing.Point start;
        private static OpenRPA.Interfaces.Overlay.OverlayWindow _overlayWindow;
        private static System.Threading.AutoResetEvent waitHandle;
        private static bool mouseDown = false;
        private static System.Drawing.Point point;
        private static System.Windows.Forms.Form form = null;

        public static void createform()
        {
            var t = Task.Factory.StartNew(() =>
            {
                if (secondThreadFormHandle != IntPtr.Zero) return;
                if (form == null) form = new overlayform();
                form.HandleCreated += SecondFormHandleCreated;
                form.HandleDestroyed += SecondFormHandleDestroyed;
                form.ShowDialog();
            });
        }
        const int WM_CLOSE = 0x0010;
        private static IntPtr secondThreadFormHandle;
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        extern static IntPtr PostMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
        public static void removeform()
        {

            if (secondThreadFormHandle != IntPtr.Zero)
                PostMessage(secondThreadFormHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
        static void SecondFormHandleCreated(object sender, EventArgs e)
        {
            if (form == null) return;
            secondThreadFormHandle = form.Handle;
            form.HandleCreated -= SecondFormHandleCreated;
        }

        static void SecondFormHandleDestroyed(object sender, EventArgs e)
        {
            if (form == null) return;
            secondThreadFormHandle = IntPtr.Zero;
            form.HandleDestroyed -= SecondFormHandleDestroyed;
        }
        private static void onCancel()
        {
            OpenRPA.Input.InputDriver.Instance.OnMouseDown -= onMouseDown;
            OpenRPA.Input.InputDriver.Instance.OnMouseUp -= onMouseUp2;
            OpenRPA.Input.InputDriver.Instance.OnMouseMove -= onMouseMove;
            OpenRPA.Input.InputDriver.Instance.onCancel -= onCancel;
            OpenRPA.Input.InputDriver.Instance.CallNext = true;
            point = System.Drawing.Point.Empty;
            removeform();
            waitHandle.Set();
        }
        private static void onMouseUp2(InputEventArgs e)
        {
            OpenRPA.Input.InputDriver.Instance.OnMouseDown -= onMouseDown;
            OpenRPA.Input.InputDriver.Instance.OnMouseUp -= onMouseUp2;
            OpenRPA.Input.InputDriver.Instance.OnMouseMove -= onMouseMove;
            OpenRPA.Input.InputDriver.Instance.onCancel -= onCancel;
            OpenRPA.Input.InputDriver.Instance.CallNext = true;
            point = new System.Drawing.Point(e.X, e.Y);
            removeform();
            waitHandle.Set();
        }
        async public static Task<System.Drawing.Rectangle> GetitAsync()
        {
            if (VersionHelper.IsWindows8OrGreater())
            {
                _overlayWindow = new Interfaces.Overlay.OverlayWindow(true);
            }
            else
            {
                _overlayWindow = new Interfaces.Overlay.OverlayWindow(false);

            }
            _overlayWindow.Visible = true;
            _overlayWindow.Bounds = new System.Drawing.Rectangle(0, 0, 10, 10);
            createform();

            mouseDown = false;
            var pos = System.Windows.Forms.Cursor.Position;
            rect = new System.Drawing.Rectangle(pos.X, pos.Y, 1, 1);
            //hi = new Highlighter(rect, System.Drawing.Color.Red);

            OpenRPA.Input.InputDriver.Instance.OnMouseDown += onMouseDown;
            OpenRPA.Input.InputDriver.Instance.OnMouseUp += onMouseUp;
            OpenRPA.Input.InputDriver.Instance.OnMouseMove += onMouseMove;
            OpenRPA.Input.InputDriver.Instance.onCancel += onCancel;
            OpenRPA.Input.InputDriver.Instance.CallNext = false;

            waitHandle = new System.Threading.AutoResetEvent(false);
            await waitHandle.WaitOneAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            //System.Windows.Forms.Application.Exit();

            return rect;

        }
        private static void onMouseDown(InputEventArgs e)
        {
            mouseDown = true;
            start = new System.Drawing.Point(e.X, e.Y);
        }
        private static void onMouseMove(InputEventArgs e)
        {
            if (_overlayWindow == null) return;
            if (!mouseDown)
            {
                rect = new System.Drawing.Rectangle(e.X, e.Y, 1, 1);
                _overlayWindow.setLocation(rect);
                return;
            }
            if (e.X < start.X)
            {
                rect.X = e.X;
                rect.Width = start.X - e.X;
            }
            else
            {
                rect.X = start.X;
                rect.Width = e.X - start.X;
            }
            if (e.Y < start.Y)
            {
                rect.Y = e.Y;
                rect.Height = start.Y - e.Y;
            }
            else
            {
                rect.Y = start.Y;
                rect.Height = e.Y - start.Y;
            }

            //rect.Height = e.Y - rect.Y;
            _overlayWindow.setLocation(rect);
        }
        private static void onMouseUp(InputEventArgs e)
        {
            if (rect.Width < 3 || rect.Height < 3) return;
            if (_overlayWindow != null) _overlayWindow.Dispose();
            _overlayWindow = null;
            OpenRPA.Input.InputDriver.Instance.OnMouseDown -= onMouseDown;
            OpenRPA.Input.InputDriver.Instance.OnMouseUp -= onMouseUp;
            OpenRPA.Input.InputDriver.Instance.OnMouseMove -= onMouseMove;
            OpenRPA.Input.InputDriver.Instance.onCancel -= onCancel;
            OpenRPA.Input.InputDriver.Instance.CallNext = true;

            removeform();
            waitHandle.Set();
        }
        public const int AddedWidth = 150;
        public const int AddedHeight = 100;

        public static System.Drawing.Bitmap GuessContour(AutomationElement element,
            int x, int y, out int OffsetX, out int OffsetY, out System.Drawing.Rectangle resultrect)
        {
            //var element = automationutil.getElementAt(x, y);
            var elementx = (int)element.BoundingRectangle.X;
            var elementy = (int)element.BoundingRectangle.Y;
            var elementw = (int)element.BoundingRectangle.Width;
            var elementh = (int)element.BoundingRectangle.Height;
            Log.Verbose(string.Format("Snap screenshot of element at ({0}, {1},{2},{3})",
                elementx, elementy, elementx + elementw, elementy + elementh));
            var desktopb = Interfaces.Image.Util.Screenshot(elementx, elementy, elementw, elementh);

            List<System.Drawing.Rectangle> con = FindContours(desktopb);
            //var point = new System.Drawing.Point(x - elementx, y - elementy);
            var point = new System.Drawing.Point(x, y);

            var saveimage = new Image<Emgu.CV.Structure.Bgr, Byte>(desktopb);
            foreach (var match in con)
            {
                saveimage.Draw(match, new Emgu.CV.Structure.Bgr(System.Drawing.Color.Red), 2);
            }
            // rpaactivities.image.util.saveImage(saveimage, "FoundContours");
            saveimage.Dispose();


            //var con = FindContours(bitmap);
            var overlaps = new List<System.Drawing.Rectangle>();
            // Make all matches a certain size
            var minSize = 30;
            for (var i = 0; i < con.Count; i++)
            {
                var match = con[i];
                if (match.Width < minSize)
                {
                    int dif = (minSize - match.Width);
                    match.Width += dif;
                    match.X -= (dif / 2);
                    if (match.X < 0) { match.X = 0; }
                    con[i] = match;
                }
                if (match.Height < minSize)
                {
                    int dif = (minSize - match.Height);
                    match.Height += dif;
                    match.Y -= (dif / 2);
                    if (match.Y < 0) { match.Y = 0; }
                    con[i] = match;
                }
            }
            // Take only hits that
            foreach (var match in con)
            {
                if (match.Contains(point))
                {
                    overlaps.Add(match);
                }
            }


            //if(overlaps.Count > 0)
            //{
            //    saveimage = new Image<Emgu.CV.Structure.Bgr, Byte>(desktopb);
            //    foreach (var match in con)
            //    {
            //        saveimage.Draw(match, new Bgr(System.Drawing.Color.Red), 2);
            //    }
            //    rpaactivities.image.util.saveImage(saveimage, "GuessContour-hits");
            //    saveimage.Dispose();
            //}

            Log.Verbose("Found " + con.Count + " Contours with " + overlaps.Count + " overlaps");
            var rect = System.Drawing.Rectangle.Empty;
            //bool again = false;
            foreach (var match in overlaps)
            {
                if (match.Width < 500 && match.Height < 500)
                {
                    //if (rect == System.Drawing.Rectangle.Empty)
                    //{
                    //    var testb = desktopb.Clone(match, System.Drawing.Imaging.PixelFormat.Undefined);
                    //    var test = Matches.FindMatches(desktopb, testb, 0.8, 2, false);
                    //    System.Diagnostics.Trace.WriteLine("Testing with " + match.ToString() + " yelded " + test.Count() + " results");
                    //    if (test.Count() > 0)
                    //    {
                    //        rect = match;
                    //    }
                    //}
                    if (rect == System.Drawing.Rectangle.Empty)
                    {
                        rect = match;
                    }
                    else if (rect.Width < match.Width || rect.Height < match.Height)
                    {
                        //if (again)
                        //{
                        //    rect = match;
                        //    again = false;
                        //}
                        //rect = match;
                    }
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine("skipping: " + match.ToString());
                }
            }

            if (rect != System.Drawing.Rectangle.Empty)
            {
                saveimage = new Image<Emgu.CV.Structure.Bgr, Byte>(desktopb);
                saveimage.Draw(rect, new Emgu.CV.Structure.Bgr(System.Drawing.Color.Red), 2);
                // rpaactivities.image.util.saveImage(saveimage, "GuessContour-result");
                saveimage.Dispose();
                desktopb.Dispose();
                // System.Diagnostics.Trace.WriteLine("using overlaps " + rect.ToString());
                if (rect.Width > elementw) { rect.Width = elementw; }
                if (rect.Height > elementh) { rect.Height = elementh; }
                OffsetX = x - rect.X;
                OffsetY = y - rect.Y;
                resultrect = new System.Drawing.Rectangle(elementx + rect.X, elementy + rect.Y, rect.Width, rect.Height);
                System.Diagnostics.Trace.WriteLine(string.Format("Snap screenshot found Contour at ({0}, {1},{2},{3})",
                    elementx + rect.X, elementy + rect.Y, rect.Width, rect.Height), "Debug");
                return Interfaces.Image.Util.Screenshot(elementx + rect.X, elementy + rect.Y, rect.Width, rect.Height);
            }
            OffsetX = x;
            OffsetY = y;
            desktopb.Dispose();
            resultrect = System.Drawing.Rectangle.Empty;
            return null;
        }


        // https://stackoverflow.com/questions/29156091/opencv-edge-border-detection-based-on-color
        public static List<System.Drawing.Rectangle> FindContours(System.Drawing.Bitmap bitmap)
        {
            double cannyThresholdLinking = 120.0;
            double cannyThreshold = 180.0;
            Emgu.CV.Structure.LineSegment2D[] lines;
            UMat cannyEdges = new UMat();
            using (var img = new Image<Emgu.CV.Structure.Bgr, Byte>(bitmap))
            {
                using (UMat uimage = new UMat())
                {
                    //Convert the image to grayscale and filter out the noise
                    CvInvoke.CvtColor(img, uimage, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                    //use image pyr to remove noise
                    UMat pyrDown = new UMat();
                    CvInvoke.PyrDown(uimage, pyrDown);
                    CvInvoke.PyrUp(pyrDown, uimage);

                    CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);
                    lines = CvInvoke.HoughLinesP(
                           cannyEdges,
                           1, //Distance resolution in pixel-related units
                           Math.PI / 45.0, //Angle resolution measured in radians.
                           20, //threshold
                           30, //min Line width
                           10); //gap between lines

                }
            }

            var result = new List<System.Drawing.Rectangle>();

            //VectorOfVectorOfPointF contours = new VectorOfVectorOfPointF();

            using (var contours = new Emgu.CV.Util.VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, Emgu.CV.CvEnum.RetrType.List, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                //Mat hierarchy = null;
                //CvInvoke.FindContours(bwImage, contours, hierarchy, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);
                int contCount = contours.Size;
                //System.Diagnostics.Trace.WriteLine("contCount: " + contCount.ToString());
                for (int i = 0; i < contCount; i++)
                {
                    using (var contour = contours[i])
                    {
                        var _rect = CvInvoke.BoundingRectangle(contour);
                        result.Add(_rect);
                        //segmentRectangles.Add(CvInvoke.BoundingRectangle(contour));
                        //img.Draw(CvInvoke.BoundingRectangle(contour), new Bgr(Color.Red), 5);
                        //System.Diagnostics.Trace.WriteLine("{0},{1} = {2},{3},{4},{5}", point.X, point.Y, _rect.X, _rect.Y, _rect.Width, _rect.Height);
                        //if (_rect.Contains(point))
                        //{
                        //    overlaps.Add(_rect);
                        //    //img.Draw(CvInvoke.BoundingRectangle(contour), new Bgr(System.Drawing.Color.Red), 2);
                        //}
                        //else
                        //{
                        //    //img.Draw(CvInvoke.BoundingRectangle(contour), new Bgr(System.Drawing.Color.Blue), 2);
                        //}
                    }
                }
                //System.Diagnostics.Trace.WriteLine("overlaps: " + overlaps.Count);
            }
            cannyEdges.Dispose();
            return result;
        }


    }


    public class overlayform : System.Windows.Forms.Form
    {
        public overlayform()
        {
            DoubleBuffered = true;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ShowInTaskbar = false;
            TopMost = true;
            Name = "previewForm";

            //BackColor = System.Drawing.Color.Transparent;
            TransparencyKey = System.Drawing.Color.Turquoise;
            BackColor = System.Drawing.Color.Turquoise;

            ResumeLayout(false);
            Cursor = System.Windows.Forms.Cursors.Cross;

            //ResumeLayout(false);
        }
    }

}
