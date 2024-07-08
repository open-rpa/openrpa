using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;

namespace OpenRPA.Selenium.Activities
{ 
    public partial class ElementActionsDesigner
    {
        public ElementActionsDesigner()
        {
            InitializeComponent();
            DataContext = this;
        }
        public ObservableCollection<string> Actions
        {
            get
            {
                return new ObservableCollection<string> {
                    "Click",
                    "SendKeys",
                    "Submit",
                    "Clear",
                    "Text",
                    "TagName",
                    "PropertyValue",
                    "GetAttribute",
                    "GetCssValue"
                };
            }
        }

        private void cbbOption_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;

            if (selectedItem != null)
            {
                var showArgument = new List<string> { "PropertyValue", "GetAttribute", "GetCssValue" };
                if (showArgument.Contains(selectedItem.Content.ToString()))
                {
                    //tblArgument.Visibility = Visibility.Visible;
                    //txtArgument.Visibility = Visibility.Visible;
                }
                else
                {
                    //tblArgument.Visibility = Visibility.Collapsed;
                    //txtArgument.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}