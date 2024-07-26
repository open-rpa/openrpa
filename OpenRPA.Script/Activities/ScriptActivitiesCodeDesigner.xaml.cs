using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Activities.Statements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Script.Activities
{
    public partial class ScriptActivitiesCodeDesigner : INotifyPropertyChanged
    {
        public ScriptActivitiesCodeDesigner()
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
            ModelItem loadFrom = ModelItem.Parent;
            while (loadFrom.Parent != null)
            {
                var p = loadFrom.Properties.Where(x => x.Name == "Argument").FirstOrDefault();
                if (p != null)
                {
                    var value = p.ComputedValue;
                    if (value != null)
                    {
                        if (value is System.Activities.DelegateInArgument)
                        {
                            variableModels.Add(p.Value);
                        }
                    }
                }
                if (loadFrom.ItemType == typeof(Sequence))
                {
                }
                loadFrom = loadFrom.Parent;
            }

            return variableModels;
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            ModelItemDictionary dictionary = base.ModelItem.Properties["Arguments"].Dictionary;
            var options = new System.Activities.Presentation.DynamicArgumentDesignerOptions() { Title = ModelItem.GetValue<string>("DisplayName") };
            using (ModelEditingScope modelEditingScope = dictionary.BeginEdit())
            {
                if (System.Activities.Presentation.DynamicArgumentDialog.ShowDialog(base.ModelItem, dictionary, base.Context, base.ModelItem.View, options))
                {
                    modelEditingScope.Complete();
                }
                else
                {
                    modelEditingScope.Revert();
                }
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
            if (!namespaces.Contains("System.Collections.Generic")) namespaces.Add("System.Collections.Generic");


            string[] current = ModelItem.GetValue<string[]>("namespaces");
            if (current == null || namespaces.Count() != current.Count())
            {
                ModelItem.Properties["namespaces"].SetValue(namespaces.ToArray());
            }
            else
            {
                bool changed = false;
                for (var i = 0; i < namespaces.Count(); i++)
                    if (namespaces[i] != current[i]) changed = true;
                if (changed)
                {
                    ModelItem.Properties["namespaces"].SetValue(namespaces.ToArray());
                }
            }

            string designerIconFile = ModelItem.GetValue<string>("designerIconFile");
            if(!string.IsNullOrEmpty(designerIconFile) && File.Exists(designerIconFile))
            {
                var uri = new Uri(designerIconFile, UriKind.Absolute);
                var bitmap = new BitmapImage(uri);
                ImageDrawing imageDrawing = new ImageDrawing(bitmap, new Rect(0, 0, 16, 16));
                DrawingBrush drawingBrush = new DrawingBrush(imageDrawing);
                Icon = drawingBrush;
            }

        }
    }
}