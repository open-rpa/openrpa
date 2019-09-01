using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Script.Activities
{
    public partial class InvokeCodeDesigner : INotifyPropertyChanged
    {
        public InvokeCodeDesigner()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<ModelItem> FindVariablesInScope(ModelItem ModelItem)
        {
            List<ModelItem> variableModels = null;
            var assemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies().ToList();
            var sap = assemblies.Where(x => x.Name == "System.Activities.Presentation").First();
            Assembly asm = Assembly.Load(sap.ToString());

            var t = asm.GetType("System.Activities.Presentation.View.VariableHelper");
            var ms = asm.GetType("System.Activities.Presentation.View.VariableHelper").GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var m in ms)
            {
                if (m.Name == "FindVariablesInScope" && m.GetParameters().Count() == 1)
                {
                    var o = m.Invoke(m, new[] { ModelItem });
                    variableModels = o as List<ModelItem>;
                }
            }
            return variableModels;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ec = ModelItemExtensions.GetEditingContext(ModelItem);
            var modelService = ec.Services.GetService<System.Activities.Presentation.Services.ModelService>();
            ModelItemCollection importsModelItem = modelService.Root.Properties["Imports"].Collection;
            var namespaces = new List<string>();
            foreach (ModelItem import in importsModelItem) namespaces.Add(import.Properties["Namespace"].ComputedValue as string);

            var vars = FindVariablesInScope(ModelItem);
            string code = ModelItem.GetValue<string>("Code");
            string language = ModelItem.GetValue<string>("Language");
            if (string.IsNullOrEmpty(language)) language = "VB";
            var f = new Editor(code, language, vars, namespaces.ToArray());
            f.textEditor.SyntaxHighlighting = f.Languages.Where(x => x.Name == language).FirstOrDefault();
            f.ShowDialog();
            if (f.textEditor.Text != code)
            {
                ModelItem.Properties["Code"].SetValue(new InArgument<string>() { Expression = new Literal<string>(f.textEditor.Text) });
            }
            if (f.textEditor.SyntaxHighlighting.Name != language)
            {
                ModelItem.Properties["Language"].SetValue(new InArgument<string>() { Expression = new Literal<string>(f.textEditor.SyntaxHighlighting.Name) });
            }
        }

        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            var ec = ModelItemExtensions.GetEditingContext(ModelItem);
            var modelService = ec.Services.GetService<System.Activities.Presentation.Services.ModelService>();
            ModelItemCollection importsModelItem = modelService.Root.Properties["Imports"].Collection;
            var namespaces = new List<string>();
            foreach (ModelItem import in importsModelItem) namespaces.Add(import.Properties["Namespace"].ComputedValue as string);
            if (!namespaces.Contains("System.Collections")) namespaces.Add("System.Collections");

            string[] current = ModelItem.GetValue<string[]>("namespaces");
            if(current == null || namespaces.Count() != current.Count())
            {
                ModelItem.Properties["namespaces"].SetValue(namespaces.ToArray());
            }           

        }
    }
}