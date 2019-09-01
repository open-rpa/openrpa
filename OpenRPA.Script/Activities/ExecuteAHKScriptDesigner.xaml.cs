using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Media;


namespace OpenRPA.Script.Activities
{
    public partial class ExecuteAHKScriptDesigner
    {
        public ExecuteAHKScriptDesigner()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ec = ModelItemExtensions.GetEditingContext(ModelItem);
            var modelService = ec.Services.GetService<System.Activities.Presentation.Services.ModelService>();
            ModelItemCollection importsModelItem = modelService.Root.Properties["Imports"].Collection;
            var namespaces = new System.Collections.Generic.List<string>();
            foreach (ModelItem import in importsModelItem) namespaces.Add(import.Properties["Namespace"].ComputedValue as string);

            string script = ModelItem.GetValue<string>("Script");
            var f = new Editor(script, "AutoHotkey", null, namespaces.ToArray());
            f.highlightingComboBox.Visibility = Visibility.Hidden;
            f.ShowDialog();
            if (f.textEditor.Text != script)
            {
                ModelItem.Properties["Script"].SetValue(new InArgument<string>() { Expression = new Literal<string>(f.textEditor.Text) });
            }

        }
    }
}