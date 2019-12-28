using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
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

namespace OpenRPA.Windows
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
        private void Open_Selector(object sender, RoutedEventArgs e)
        {
            ModelItem loadFrom = ModelItem.Parent;
            string loadFromSelectorString = "";
            WindowsSelector anchor = null;
            while (loadFrom.Parent != null)
            {
                var p = loadFrom.Properties.Where(x => x.Name == "Selector").FirstOrDefault();
                if(p != null)
                {
                    loadFromSelectorString = loadFrom.GetValue<string>("Selector");
                    anchor = new WindowsSelector(loadFromSelectorString);
                    break;
                }
                loadFrom = loadFrom.Parent;
            }
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            Interfaces.Selector.SelectorWindow selectors;
            if (!string.IsNullOrEmpty(SelectorString)) {
                var selector = new WindowsSelector(SelectorString);
                selectors = new Interfaces.Selector.SelectorWindow("Windows", selector, anchor, maxresults);
            } else
            {
                var selector = new WindowsSelector("[{Selector: 'Windows'}]");
                selectors = new Interfaces.Selector.SelectorWindow("Windows", selector, anchor, maxresults);
            }
            if (selectors.ShowDialog() == true)
            {
                ModelItem.Properties["Selector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(selectors.vm.json) });
                var l = selectors.vm.Selector.Last();
                if(l.Element != null)
                {
                    ModelItem.Properties["Image"].SetValue(l.Element.ImageString());
                    NotifyPropertyChanged("Image");
                }
                if (anchor!=null)
                {
                    ModelItem.Properties["From"].SetValue(new InArgument<IElement>()
                    {
                        Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
                    });

                }
            }
        }
        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            ModelItem loadFrom = ModelItem.Parent;
            string loadFromSelectorString = "";
            WindowsSelector anchor = null;
            while (loadFrom.Parent != null)
            {
                var p = loadFrom.Properties.Where(x => x.Name == "Selector").FirstOrDefault();
                if (p != null)
                {
                    loadFromSelectorString = loadFrom.GetValue<string>("Selector");
                    anchor = new WindowsSelector(loadFromSelectorString);
                    break;
                }
                loadFrom = loadFrom.Parent;
            }

            HighlightImage = Extensions.GetImageSourceFromResource(".x.png");
            NotifyPropertyChanged("HighlightImage");
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            var selector = new WindowsSelector(SelectorString);

            Task.Run(() =>
            {
                var elements = new List<UIElement>();
                if (anchor != null)
                {
                    var _base = WindowsSelector.GetElementsWithuiSelector(anchor, null, 10);
                    foreach (var _e in _base)
                    {
                        var res = WindowsSelector.GetElementsWithuiSelector(selector, _e, maxresults);
                        elements.AddRange(res);
                    }

                }
                else
                {
                    var res = WindowsSelector.GetElementsWithuiSelector(selector, null, maxresults);
                    elements.AddRange(res);
                }

                if (elements.Count() > 0)
                {
                    HighlightImage = Extensions.GetImageSourceFromResource("check.png");
                    NotifyPropertyChanged("HighlightImage");
                }
                foreach (var ele in elements) ele.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));

            });

        }
        public string ImageString
        {
            get
            {
                return ModelItem.GetValue<string>("Image");
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