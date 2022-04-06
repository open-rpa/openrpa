using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Converters;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OpenRPA.Interfaces;

namespace OpenRPA.Interfaces.Activities
{
    public class ArgumentCollectionEditor : DialogPropertyValueEditor
    {
        public ArgumentCollectionEditor()
        {
            this.InlineEditorTemplate = new DataTemplate();

            FrameworkElementFactory stack = new FrameworkElementFactory(typeof(StackPanel));
            stack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            FrameworkElementFactory label = new FrameworkElementFactory(typeof(Label));
            Binding labelBinding = new Binding("Value");
            label.SetValue(Label.ContentProperty, labelBinding);
            label.SetValue(Label.MaxWidthProperty, 90.0);

            stack.AppendChild(label);

            FrameworkElementFactory editModeSwitch = new FrameworkElementFactory(typeof(EditModeSwitchButton));

            editModeSwitch.SetValue(EditModeSwitchButton.TargetEditModeProperty, PropertyContainerEditMode.Dialog);

            stack.AppendChild(editModeSwitch);

            InlineEditorTemplate.VisualTree = stack;
        }
        public override void ShowDialog(PropertyValue propertyValue, System.Windows.IInputElement commandSource)
        {
            // https://stackoverflow.com/questions/8731605/exposing-collection-of-arguments-for-activity-in-property-grid
            var PropertyName = propertyValue.ParentProperty.PropertyName;
            ModelItem activity = new ModelPropertyEntryToOwnerActivityConverter().Convert(propertyValue.ParentProperty, typeof(ModelItem), false, null) as ModelItem;

            DynamicArgumentDesignerOptions options1 = new DynamicArgumentDesignerOptions();
            options1.Title = activity.GetValue<string>("DisplayName");
            DynamicArgumentDesignerOptions options = options1;
            if (!activity.Properties[PropertyName].IsSet)
            {
                Log.Output(PropertyName + " is not set");
                return;
            }
            ModelItem collection = activity.Properties[PropertyName].Collection;
            if (collection == null)
            {
                collection = activity.Properties[PropertyName].Dictionary;
            }
            if (collection == null)
            {
                Log.Output(PropertyName + " is not a Collection or Dictionary");
                return;
            }
            using (ModelEditingScope scope = collection.BeginEdit(PropertyName + "Editing"))
            {
                if (DynamicArgumentDialog.ShowDialog(activity, collection, activity.GetEditingContext(), activity.View, options))
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
