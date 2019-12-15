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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Image
{
    public partial class GetTextDesigner : INotifyPropertyChanged
    {
        public GetTextDesigner()
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
            ModelItem loadFrom = ModelItem.Parent;
            string loadFromSelectorString = "";
            while (loadFrom.Parent != null)
            {
                var p = loadFrom.Properties.Where(x => x.Name == "Image").FirstOrDefault();
                if (p != null)
                {
                    loadFromSelectorString = loadFrom.GetValue<string>("Selector");
                    break;
                }
                loadFrom = loadFrom.Parent;
            }
            var Image = loadFrom.GetValue<string>("Image");
            var stream = new System.IO.MemoryStream(Convert.FromBase64String(Image));
            var b = new System.Drawing.Bitmap(stream);
            var Threshold = loadFrom.GetValue<double>("Threshold");
            var CompareGray = loadFrom.GetValue<bool>("CompareGray");
            var Processname = loadFrom.GetValue<string>("Processname");
            var limit = loadFrom.GetValue<Rectangle>("Limit");
            if (Threshold < 0.5) Threshold = 0.8;

            Interfaces.GenericTools.Minimize(Interfaces.GenericTools.MainWindow);
            System.Threading.Thread.Sleep(100);
            var matches = ImageEvent.waitFor(b, Threshold, Processname, TimeSpan.FromMilliseconds(100), CompareGray, limit);
            if (matches.Count() == 0)
            {
                Interfaces.GenericTools.Restore();
                return;
            }
            var match = matches[0];

            Rectangle rect = Rectangle.Empty;
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
            {
                _overlayWindow.BackColor = System.Drawing.Color.Blue;
                _overlayWindow.Visible = true;
                _overlayWindow.Bounds = match;
                _overlayWindow.TopMost = true;
                rect = await getrectangle.GetitAsync();
            }

            ModelItem.Properties["OffsetX"].SetValue(new System.Activities.InArgument<int>(rect.X - match.X));
            ModelItem.Properties["OffsetY"].SetValue(new System.Activities.InArgument<int>(rect.Y - match.Y));
            ModelItem.Properties["Width"].SetValue(new System.Activities.InArgument<int>(rect.Width));
            ModelItem.Properties["Height"].SetValue(new System.Activities.InArgument<int>(rect.Height));
            Interfaces.GenericTools.Restore();


        }
        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
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
                var base64 = ImageString;
                if (string.IsNullOrEmpty(base64)) return null;
                using (var b = Interfaces.Image.Util.Base642Bitmap(base64))
                {
                    return Interfaces.Image.Util.BitmapToImageSource(b, Interfaces.Image.Util.ActivityPreviewImageWidth, Interfaces.Image.Util.ActivityPreviewImageHeight);
                }
            }
        }

    }
}