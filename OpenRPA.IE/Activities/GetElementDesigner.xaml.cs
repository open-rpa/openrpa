using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.IE
{
    public partial class GetElementDesigner : INotifyPropertyChanged
    {
        public GetElementDesigner()
        {
            InitializeComponent();
            HighlightImage = Extensions.GetImageSourceFromResource("search.png");
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public BitmapFrame HighlightImage { get; set; }
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void Open_Selector(object sender, RoutedEventArgs e)
        {
            ModelItem loadFrom = ModelItem.Parent;
            string loadFromSelectorString = "";
            IESelector anchor = null;
            while (loadFrom.Parent != null)
            {
                var p = loadFrom.Properties.Where(x => x.Name == "Selector").FirstOrDefault();
                if (p != null)
                {
                    loadFromSelectorString = loadFrom.GetValue<string>("Selector");
                    anchor = new IESelector(loadFromSelectorString);
                    break;
                }
                loadFrom = loadFrom.Parent;
            }
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            Interfaces.Selector.SelectorWindow selectors;
            if (!string.IsNullOrEmpty(SelectorString))
            {
                var selector = new IESelector(SelectorString);
                selectors = new Interfaces.Selector.SelectorWindow("IE", selector, anchor, maxresults);
            }
            else
            {
                var selector = new IESelector("[{Selector: 'IE'}]");
                selectors = new Interfaces.Selector.SelectorWindow("IE", selector, anchor, maxresults);
            }
            if (selectors.ShowDialog() == true)
            {
                ModelItem.Properties["Selector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(selectors.vm.json) });
                var l = selectors.vm.Selector.Last();
                if (l.Element != null)
                {
                    ModelItem.Properties["Image"].SetValue(l.Element.ImageString());
                    NotifyPropertyChanged("Image");
                }
                if (anchor != null)
                {
                    ModelItem.Properties["From"].SetValue(new InArgument<IEElement>()
                    {
                        Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IEElement>("item")
                    });

                }
            }
        }
        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            ModelItem loadFrom = ModelItem.Parent;
            string loadFromSelectorString = "";
            IESelector anchor = null;
            while (loadFrom.Parent != null)
            {
                var p = loadFrom.Properties.Where(x => x.Name == "Selector").FirstOrDefault();
                if (p != null)
                {
                    loadFromSelectorString = loadFrom.GetValue<string>("Selector");
                    anchor = new IESelector(loadFromSelectorString);
                    break;
                }
                loadFrom = loadFrom.Parent;
            }

            HighlightImage = Extensions.GetImageSourceFromResource(".x.png");
            NotifyPropertyChanged("HighlightImage");

            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            var selector = new IESelector(SelectorString);
            var elements = new List<IEElement>();
            if (anchor != null)
            {
                var _base = IESelector.GetElementsWithuiSelector(anchor, null, 10);
                foreach (var _e in _base)
                {
                    var res = IESelector.GetElementsWithuiSelector(selector, _e, maxresults);
                    elements.AddRange(res);
                }

            }
            else
            {
                var res = IESelector.GetElementsWithuiSelector(selector, null, maxresults);
                elements.AddRange(res);
            }

            if (elements.Count() > 0)
            {
                HighlightImage = Extensions.GetImageSourceFromResource("check.png");
                NotifyPropertyChanged("HighlightImage");
            }
            foreach (var ele in elements) ele.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
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
                //if (System.Text.RegularExpressions.Regex.Match(base64, "[a-f0-9]{24}").Success)
                //{
                //    return image.Screenutil.BitmapToImageSource(image.util.loadWorkflowImage(base64), image.Screenutil.ActivityPreviewImageWidth, image.Screenutil.ActivityPreviewImageHeight);
                //}

                // return OpenRPA.Interfaces.Image.Util.BitmapToImageSource
                using (var image = Interfaces.Image.Util.Base642Bitmap(base64))
                {
                    // Interfaces.Image.Util.SaveImageStamped(image, System.IO.Directory.GetCurrentDirectory(), "WindowsGetElement");
                    return Interfaces.Image.Util.BitmapToImageSource(image, Interfaces.Image.Util.ActivityPreviewImageWidth, Interfaces.Image.Util.ActivityPreviewImageHeight);
                }
            }
        }
    }
}