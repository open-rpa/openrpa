using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Activities.Presentation.Model;
    using System.Linq;

    public static class GenericArgumentTypeUpdater
    {
        private const String DisplayName = "DisplayName";

        public static void Attach(ModelItem modelItem)
        {
            Attach(modelItem, Int32.MaxValue);
        }

        public static void Attach(ModelItem modelItem, Int32 maximumUpdatableTypes)
        {
            Type[] genericArguments = modelItem.ItemType.GetGenericArguments();

            if (genericArguments.Any() == false)
            {
                return;
            }

            Int32 argumentCount = genericArguments.Length;
            Int32 updatableArgumentCount = Math.Min(argumentCount, maximumUpdatableTypes);
            EditingContext context = modelItem.GetEditingContext();
            AttachedPropertiesService attachedPropertiesService = context.Services.GetService<AttachedPropertiesService>();

            for (Int32 index = 0; index < updatableArgumentCount; index++)
            {
                AttachUpdatableArgumentType(modelItem, attachedPropertiesService, index, updatableArgumentCount);
            }
        }

        private static void AttachUpdatableArgumentType(
            ModelItem modelItem, AttachedPropertiesService attachedPropertiesService, Int32 argumentIndex, Int32 argumentCount)
        {
            String propertyName = "ArgumentType";

            if (argumentCount > 1)
            {
                propertyName += argumentIndex + 1;
            }

            AttachedProperty<Type> attachedProperty = new AttachedProperty<Type>
            {
                Name = propertyName,
                OwnerType = modelItem.ItemType,
                IsBrowsable = true
            };

            attachedProperty.Getter = (ModelItem arg) => GetTypeArgument(arg, argumentIndex);
            attachedProperty.Setter = (ModelItem arg, Type newType) => UpdateTypeArgument(arg, argumentIndex, newType);

            attachedPropertiesService.AddProperty(attachedProperty);
        }

        private static bool DisplayNameRequiresUpdate(ModelItem modelItem)
        {
            String currentDisplayName = (String)modelItem.Properties[DisplayName].ComputedValue;

            // Sometimes the display name is empty
            if (String.IsNullOrWhiteSpace(currentDisplayName))
            {
                return true;
            }

            // The default calculation of a generic type does not include spaces in the generic type arguments
            // However an activity might include these as the default display name
            // Strip spaces to provide a more accurate match
            String defaultDisplayName = GetActivityDefaultName(modelItem.ItemType);

            currentDisplayName = currentDisplayName.Replace(" ", String.Empty);
            defaultDisplayName = defaultDisplayName.Replace(" ", String.Empty);

            if (String.Equals(currentDisplayName, defaultDisplayName, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        private static String GetActivityDefaultName(Type activityType)
        {
            Activity activity = (Activity)Activator.CreateInstance(activityType);

            return activity.DisplayName;
        }

        private static Type GetTypeArgument(ModelItem modelItem, Int32 argumentIndex)
        {
            return modelItem.ItemType.GetGenericArguments()[argumentIndex];
        }

        private static void UpdateTypeArgument(ModelItem modelItem, Int32 argumentIndex, Type newGenericType)
        {
            Type itemType = modelItem.ItemType;
            Type[] genericTypes = itemType.GetGenericArguments();

            // Replace the type being changed
            genericTypes[argumentIndex] = newGenericType;

            Type newType = itemType.GetGenericTypeDefinition().MakeGenericType(genericTypes);
            EditingContext editingContext = modelItem.GetEditingContext();
            Object instanceOfNewType = Activator.CreateInstance(newType);
            ModelItem newModelItem = ModelFactory.CreateItem(editingContext, instanceOfNewType);

            using (ModelEditingScope editingScope = newModelItem.BeginEdit("Change type argument"))
            {
                MorphHelper.MorphObject(modelItem, newModelItem);
                MorphHelper.MorphProperties(modelItem, newModelItem);

                if (itemType.IsSubclassOf(typeof(Activity)) && newType.IsSubclassOf(typeof(Activity)))
                {
                    if (DisplayNameRequiresUpdate(modelItem))
                    {
                        // Update to the new display name
                        String newDisplayName = GetActivityDefaultName(newType);

                        newModelItem.Properties[DisplayName].SetValue(newDisplayName);
                    }
                }

                DesignerUpdater.UpdateModelItem(modelItem, newModelItem);

                editingScope.Complete();
            }
        }
    }
}
