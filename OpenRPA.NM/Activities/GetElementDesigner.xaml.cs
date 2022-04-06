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

namespace OpenRPA.NM
{
    public partial class GetElementDesigner : INotifyPropertyChanged
    {
        public GetElementDesigner()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void Open_Selector(object sender, RoutedEventArgs e)
        {
            ModelItem loadFrom = ModelItem.Parent;
            string loadFromSelectorString = "";
            NMSelector anchor = null;
            while (loadFrom.Parent != null)
            {
                var p = loadFrom.Properties.Where(x => x.Name == "Selector").FirstOrDefault();
                if (p != null)
                {
                    loadFromSelectorString = loadFrom.GetValue<string>("Selector");
                    anchor = new NMSelector(loadFromSelectorString);
                    break;
                }
                loadFrom = loadFrom.Parent;
            }
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            Interfaces.Selector.SelectorWindow selectors;
            if (!string.IsNullOrEmpty(SelectorString))
            {
                var selector = new NMSelector(SelectorString);
                selectors = new Interfaces.Selector.SelectorWindow("NM", selector, anchor, maxresults);
            }
            else
            {
                var selector = new NMSelector("[{Selector: 'NM'}]");
                selectors = new Interfaces.Selector.SelectorWindow("NM", selector, anchor, maxresults);
            }
            // selectors.Owner = GenericTools.MainWindow; -- Locks up and never returns ?
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
                    ModelItem.Properties["From"].SetValue(new InArgument<NMElement>()
                    {
                        Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<NMElement>("item")
                    });
                    ModelItem.Properties["MinResults"].SetValue(new InArgument<int>()
                    {
                        Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<int>("0")
                    });
                    //ModelItem.Properties["Timeout"].SetValue(new InArgument<TimeSpan>()
                    //{
                    //    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue< TimeSpan>("00:00:00")
                    //});
                }
            }
        }
        private async void Highlight_Click(object sender, RoutedEventArgs e)
        {
            ModelItem loadFrom = ModelItem.Parent;
            string loadFromSelectorString = "";
            NMSelector anchor = null;
            int parentmaxresults = 1;
            while (loadFrom.Parent != null)
            {
                var p = loadFrom.Properties.Where(x => x.Name == "Selector").FirstOrDefault();
                if (p != null)
                {
                    loadFromSelectorString = loadFrom.GetValue<string>("Selector");
                    anchor = new NMSelector(loadFromSelectorString);
                    parentmaxresults = loadFrom.GetValue<int>("MaxResults");
                    break;
                }
                loadFrom = loadFrom.Parent;
            }


            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            var selector = new NMSelector(SelectorString);
            // var elements = NMSelector.GetElementsWithuiSelector(selector, anchor, maxresults);
            var elements = new List<NMElement>();
            if (anchor != null)
            {
                var _base = NMSelector.GetElementsWithuiSelector(anchor, null, parentmaxresults);
                foreach (var _e in _base)
                {
                    var res = NMSelector.GetElementsWithuiSelector(selector, _e, maxresults);
                    elements.AddRange(res);
                }

            }
            else
            {
                var res = NMSelector.GetElementsWithuiSelector(selector, null, maxresults);
                elements.AddRange(res);
            }

            foreach (var ele in elements) await ele.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));

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
        public bool ShowLoopExpanded { get; set; }
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            ShowLoopExpanded = !ShowLoopExpanded;
            NotifyPropertyChanged("ShowLoopExpanded");
        }
        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            Activity loopaction = ModelItem.GetValue<Activity>("LoopAction");
            if (loopaction != null)
            {
                ShowLoopExpanded = true;
                NotifyPropertyChanged("ShowLoopExpanded");
            }
        }
    }
}