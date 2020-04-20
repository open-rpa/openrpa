using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Image
{
    public partial class GetElementDesigner : INotifyPropertyChanged
    {
        public GetElementDesigner()
        {
            InitializeComponent();
            HighlightImage = Extensions.GetImageSourceFromResource("search.png");
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public BitmapFrame HighlightImage { get; set; }
        private async void btn_Select(object sender, RoutedEventArgs e)
        {
            Interfaces.GenericTools.Minimize();

            var limit = ModelItem.GetValue<Rectangle>("Limit");
            Rectangle rect = Rectangle.Empty;
            Log.Information(limit.ToString());
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
            {
                _overlayWindow.BackColor = System.Drawing.Color.Blue;
                var tip = new Interfaces.Overlay.TooltipWindow("Select area to look for");
                if (limit != Rectangle.Empty)
                {
                    _overlayWindow.Visible = true;
                    _overlayWindow.Bounds = limit;
                    _overlayWindow.TopMost = true;
                    _overlayWindow.Opacity = 0.3;
                    tip.setText("Select area to look for within the blue area");
                }
                rect = await getrectangle.GetitAsync();
                tip.Close();
                tip = null;
            }

            if (limit != Rectangle.Empty)
            {
                if(!limit.Contains(rect))
                {
                    Log.Error(rect.ToString() + " is not within process limit of " + limit.ToString());
                    Interfaces.GenericTools.Restore();
                    return;
                }
            }

            var _image = new System.Drawing.Bitmap(rect.Width, rect.Height);
            var graphics = System.Drawing.Graphics.FromImage(_image as System.Drawing.Image);
            graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, _image.Size);
            ModelItem.Properties["Image"].SetValue(Interfaces.Image.Util.Bitmap2Base64(_image));
            NotifyPropertyChanged("Image");
            var element = AutomationHelper.GetFromPoint(rect.X, rect.Y);
            if (element != null)
            {
                var p = System.Diagnostics.Process.GetProcessById(element.ProcessId);
                var Processname = p.ProcessName;
                ModelItem.Properties["Processname"].SetValue(new System.Activities.InArgument<string>(Processname));
            }
            Interfaces.GenericTools.Restore();

        }
        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            var image = ImageString;
            Bitmap b = Task.Run(() => {
                return Interfaces.Image.Util.LoadBitmap(image);
            }).Result;
            using (b)
            {
                var Threshold = ModelItem.GetValue<double>("Threshold");
                var CompareGray = ModelItem.GetValue<bool>("CompareGray");
                var Processname = ModelItem.GetValue<string>("Processname");
                var limit = ModelItem.GetValue<Rectangle>("Limit");
                if (Threshold < 0.5) Threshold = 0.8;
                var matches = ImageEvent.waitFor(b, Threshold, Processname, TimeSpan.FromMilliseconds(100), CompareGray, limit);
                foreach (var r in matches)
                {
                    var element = new ImageElement(r);
                    element.Highlight(false, System.Drawing.Color.PaleGreen, TimeSpan.FromSeconds(1));

                }
            }
        }
        private async void ProcessLimit_Click(object sender, RoutedEventArgs e)
        {
            var Processname = ModelItem.GetValue<string>("Processname");
            var p = System.Diagnostics.Process.GetProcessesByName(Processname).FirstOrDefault();
            if (p == null) return;

            var allChildWindows = new WindowHandleInfo(p.MainWindowHandle).GetAllChildHandles();
            allChildWindows.Add(p.MainWindowHandle);
            var template = new Rectangle(0, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            Rectangle windowrect = Rectangle.Empty;
            foreach (var window in allChildWindows)
            {
                WindowHandleInfo.RECT rct;
                if (!WindowHandleInfo.GetWindowRect(new HandleRef(this, window), out rct))
                {
                    continue;
                }
                var _rect = new Rectangle(rct.Left, rct.Top, rct.Right - rct.Left + 1, rct.Bottom - rct.Top + 1);
                if (_rect.Width < template.Width && _rect.Height < template.Height)
                {
                    continue;
                }
                if ((_rect.X > 0 || (_rect.X + _rect.Width) > 0) &&
                        (_rect.Y > 0 || (_rect.Y + _rect.Height) > 0))
                {
                    windowrect = _rect;
                    continue;
                }
            }

            Interfaces.GenericTools.Minimize();
            var rect = await getrectangle.GetitAsync();

            var limit = new System.Drawing.Rectangle(rect.X - (int)windowrect.X, rect.Y - (int)windowrect.Y, rect.Width, rect.Height);
            ModelItem.Properties["Limit"].SetValue(new System.Activities.InArgument<System.Drawing.Rectangle>(limit));
            Interfaces.GenericTools.Restore();
            NotifyPropertyChanged("Limit");

        }
        public string ImageString
        {
            get
            {
                string result = string.Empty;
                result = ModelItem.GetValue<string>("Image");
                return result;
            }
        }
        public BitmapImage Image
        {
            get
            {
                var image = ImageString;
                System.Drawing.Bitmap b = Task.Run(() => {
                    return Interfaces.Image.Util.LoadBitmap(image);
                }).Result;
                using (b)
                {
                    if (b == null) return null;
                    return Interfaces.Image.Util.BitmapToImageSource(b, Interfaces.Image.Util.ActivityPreviewImageWidth, Interfaces.Image.Util.ActivityPreviewImageHeight);
                }
            }
        }

    }
}