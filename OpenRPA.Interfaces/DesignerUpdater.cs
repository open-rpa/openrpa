using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    using System;
    using System.Activities.Presentation;
    using System.Activities.Presentation.Model;
    using System.Activities.Presentation.View;
    using System.Windows.Threading;

    internal sealed class DesignerUpdater
    {
        public static void UpdateModelItem(ModelItem originalItem, ModelItem updatedItem)
        {
            DesignerUpdater class2 = new DesignerUpdater(originalItem, updatedItem);

            Action method = class2.UpdateDesigner;

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, method);
        }

        internal DesignerUpdater(ModelItem originalItem, ModelItem newItem)
        {
            _originalModelItem = originalItem;
            _newModelItem = newItem;
        }

        private readonly ModelItem _originalModelItem;

        private readonly ModelItem _newModelItem;

        public void UpdateDesigner()
        {
            EditingContext editingContext = _originalModelItem.GetEditingContext();
            DesignerView designerView = editingContext.Services.GetService<DesignerView>();

            if ((designerView.RootDesigner != null) && (((WorkflowViewElement)designerView.RootDesigner).ModelItem == _originalModelItem))
            {
                designerView.MakeRootDesigner(_newModelItem);
            }

            Selection.SelectOnly(editingContext, _newModelItem);
        }
    }
}
