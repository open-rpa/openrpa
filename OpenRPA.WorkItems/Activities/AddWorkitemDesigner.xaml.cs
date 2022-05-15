using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenRPA.Interfaces;
using System.Collections.ObjectModel;
using System.Activities.Presentation;

namespace OpenRPA.WorkItems.Activities
{
    public partial class AddWorkitemDesigner
    {
        public AddWorkitemDesigner()
        {
            InitializeComponent();
            _ = RobotInstance.instance;
            DataContext = this;
        }
        public ObservableCollection<IWorkitemQueue> WorkItemQueues { get { return global.OpenRPAClient.WorkItemQueues;  } }
        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string PropertyName = "Payload";
            DynamicArgumentDesignerOptions options1 = new DynamicArgumentDesignerOptions();
            options1.Title = ModelItem.GetValue<string>("DisplayName");
            DynamicArgumentDesignerOptions options = options1;
            if (!ModelItem.Properties[PropertyName].IsSet)
            {
                Log.Warning(PropertyName + " is not set");
                return;
            }
            ModelItem collection = ModelItem.Properties[PropertyName].Collection;
            if (collection == null)
            {
                collection = ModelItem.Properties[PropertyName].Dictionary;
            }
            if (collection == null)
            {
                Log.Warning(PropertyName + " is not a Collection or Dictionary");
                return;
            }
            using (ModelEditingScope scope = collection.BeginEdit(PropertyName + "Editing"))
            {
                if (DynamicArgumentDialog.ShowDialog(ModelItem, collection, ModelItem.GetEditingContext(), ModelItem.View, options))
                {
                    scope.Complete();
                }
                else
                {
                    scope.Revert();
                }
            }

        }
    }
}