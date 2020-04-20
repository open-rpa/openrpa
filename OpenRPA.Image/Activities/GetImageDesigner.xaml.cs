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
    public partial class GetImageDesigner : INotifyPropertyChanged
    {
        public GetImageDesigner()
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
        private async Task<Rectangle> GetBaseRectangle()
        {
            ModelItem loadFrom = ModelItem.Parent;
            string loadFromSelectorString = "";
            ModelItem gettext = null;

            var pp = ModelItem.Properties["Element"];
            if (pp.IsSet)
            {
                while (loadFrom.Parent != null)
                {
                    var p = loadFrom.Properties.Where(x => x.Name == "Image").FirstOrDefault();
                    if (p != null)
                    {
                        loadFromSelectorString = loadFrom.GetValue<string>("Selector");
                        break;
                    }
                    if (loadFrom.ItemType == typeof(GetText))
                    {
                        gettext = loadFrom;
                    }
                    loadFrom = loadFrom.Parent;
                }

            }
            Interfaces.IElement element = null;
            Rectangle match = Rectangle.Empty;
            if (!string.IsNullOrEmpty(loadFromSelectorString))
            {
                var selector = new Interfaces.Selector.Selector(loadFromSelectorString);
                var pluginname = selector.First().Selector;
                var Plugin = Interfaces.Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
                var elements = Plugin.GetElementsWithSelector(selector, null, 1);
                if (elements.Length > 0)
                {
                    element = elements[0];
                }


            }
            if (gettext != null && element != null)
            {

                var matches = GetText.Execute(element, gettext);
                if (matches.Length > 0)
                {
                    match = matches[0].Rectangle;
                }
                else
                {
                    var tip = new Interfaces.Overlay.TooltipWindow("Mark a found item");
                    match = await getrectangle.GetitAsync();
                    tip.Close();
                    tip = null;
                }
            }
            else
            {
                var image = loadFrom.GetValue<string>("Image");
                if (!string.IsNullOrEmpty(image))
                {
                    Bitmap b = Task.Run(() => {
                        return Interfaces.Image.Util.LoadBitmap(image);
                    }).Result;
                    using (b)
                    {
                        var Threshold = loadFrom.GetValue<double>("Threshold");
                        var CompareGray = loadFrom.GetValue<bool>("CompareGray");
                        var Processname = loadFrom.GetValue<string>("Processname");
                        var limit = loadFrom.GetValue<Rectangle>("Limit");
                        if (Threshold < 0.5) Threshold = 0.8;

                        Interfaces.GenericTools.Minimize();
                        System.Threading.Thread.Sleep(100);
                        var matches = ImageEvent.waitFor(b, Threshold, Processname, TimeSpan.FromMilliseconds(100), CompareGray, limit);
                        if (matches.Count() == 0)
                        {
                            Interfaces.GenericTools.Restore();
                            return Rectangle.Empty;
                        }
                        match = matches[0];
                    }
                }
            }

            return match;
        }
        private async void btn_Select(object sender, RoutedEventArgs e)
        {
            Rectangle match = await GetBaseRectangle();
            Rectangle rect = Rectangle.Empty;
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
            {
                _overlayWindow.BackColor = System.Drawing.Color.Blue;
                _overlayWindow.Visible = true;
                _overlayWindow.Bounds = match;
                _overlayWindow.TopMost = true;

                var tip = new Interfaces.Overlay.TooltipWindow("Select relative area to capture");
                rect = await getrectangle.GetitAsync();
                tip.Close();
                tip = null;
            }

            ModelItem.Properties["OffsetX"].SetValue(new System.Activities.InArgument<int>(rect.X - match.X));
            ModelItem.Properties["OffsetY"].SetValue(new System.Activities.InArgument<int>(rect.Y - match.Y));
            ModelItem.Properties["Width"].SetValue(new System.Activities.InArgument<int>(rect.Width));
            ModelItem.Properties["Height"].SetValue(new System.Activities.InArgument<int>(rect.Height));
            Interfaces.GenericTools.Restore();
        }
        private async void Highlight_Click(object sender, RoutedEventArgs e)
        {
            var OffsetX = ModelItem.GetValue<int>("OffsetX");
            var OffsetY = ModelItem.GetValue<int>("OffsetY");
            var Width = ModelItem.GetValue<int>("Width");
            var Height = ModelItem.GetValue<int>("Height");
            Rectangle match = await GetBaseRectangle();
            var _hi = new ImageElement(match);
            _ = _hi.Highlight(false, System.Drawing.Color.Blue, TimeSpan.FromSeconds(1));

            var rect = new ImageElement(new Rectangle(_hi.X + OffsetX, _hi.Y + OffsetY, Width, Height));
            await rect.Highlight(false, System.Drawing.Color.PaleGreen, TimeSpan.FromSeconds(1));
            Interfaces.GenericTools.Restore();
        }
    }
}