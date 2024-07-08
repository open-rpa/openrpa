using System.Collections.ObjectModel;

namespace OpenRPA.Selenium
{
    public partial class SwitchToDesigner
    {
        public SwitchToDesigner()
        {
            InitializeComponent();
        }
        public ObservableCollection<string> Actions
        {
            get
            {
                return new ObservableCollection<string> {
                    "Frame",
                    "ParentFrame",
                    "Window",
                    "NewWindow",
                    "DefaultContent",
                    "ActiveElement",
                    "Alert",
                };
            }
        }
    }
}