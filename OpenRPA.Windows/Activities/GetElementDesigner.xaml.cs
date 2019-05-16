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

namespace OpenRPA.Windows
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

        public BitmapFrame HighlightImage { get; set; }
        public BitmapImage Image { get; set; }

        private void Open_Selector(object sender, RoutedEventArgs e)
        {
            //WindowsSelector from = null;
            //string fromstring = null;
            //if (loadFrom != null) fromstring = loadFrom.GetValue<string>("Selector");
            //if (fromstring != null) from = new selector.zenselector(fromstring);
            
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            // if (!string.IsNullOrEmpty(ZenSelector)) ZenSelector = JArray.Parse(ZenSelector).ToString();


            var selector = new WindowsSelector(SelectorString);
            var selectors = new Interfaces.Selector.SelectorWindow("Windows", selector, maxresults);

            if(selectors.ShowDialog() == true)
            {
                ModelItem.Properties["Selector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(selectors.vm.json) });
            }


            //OpenRPA.p selector.Selector selector = null;
            //if (from != null) selector = new selector.Selector(rpaactivities.selector.elementtype.uia3, from);
            //if (from == null) selector = new selector.Selector(rpaactivities.selector.elementtype.uia3);
            //if (!string.IsNullOrEmpty(ZenSelector)) selector.SetSelector(ZenSelector);
            //selector.btnSetAnchor.Visibility = Visibility.Collapsed;
            //selector.ShowDialog();

            //if (selector.vm.json != ZenSelector)
            //{
            //    ModelItem.Properties["ZenSelector"].SetValue(new InArgument<string>() { Expression = new Literal<string>(selector.vm.json) });
            //    var element = (UIElement)selector.vm.element;
            //    if (element != null)
            //    {
            //        ModelItem.Properties["Image"].SetValue(element.Image());
            //        ModelItem.Properties["ScreenImage"].SetValue(element.ScreenImage());
            //        NotifyPropertyChanged("Image");
            //        NotifyPropertyChanged("ScreenImageImage");
            //    }
            //}
            ////var offsetx = zensel.click.offsetx;
            ////var offsety = zensel.click.offsety;
            ////ModelItem.Properties["OffsetX"].SetValue(new System.Activities.InArgument<int>(offsetx));
            ////ModelItem.Properties["OffsetY"].SetValue(new System.Activities.InArgument<int>(offsety));
            //rpaExtension.Current.restore();

        }

        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            string SelectorString = ModelItem.GetValue<string>("Selector");
            int maxresults = ModelItem.GetValue<int>("MaxResults");
            var selector = new WindowsSelector(SelectorString);
            var elements = WindowsSelector.GetElementsWithuiSelector(selector, null, maxresults);
            foreach (var ele in elements) ele.Highlight(false, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
        }
    }
}