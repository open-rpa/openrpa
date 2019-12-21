using System;
using System.Activities.Presentation.Hosting;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRPA.CodeEditor
{
    public class EditorService : IExpressionEditorService
    {
        private static OpenRPA.ExpressionEditor.EditorService service;
        public EditorService(Interfaces.IDesigner designer)
        {
            this.designer = designer;
        }
        private IExpressionEditorInstance CreateEditor(ImportedNamespaceContextItem importedNamespaces, List<ModelItem> Variables, string text, Type expressionType, Size initialSize)
        {
            var namespaces = importedNamespaces.ImportedNamespaces;
            if (namespaces == null) namespaces = new System.Collections.ObjectModel.Collection<string>();
            if (!namespaces.Contains("System.Collections")) namespaces.Add("System.Collections");
            if (!namespaces.Contains("System.Collections.Generic")) namespaces.Add("System.Collections.Generic");

            bool useOld = false;
            if((initialSize.IsEmpty || initialSize.Height < 25) && useOld)
            {
                if (service == null)
                {
                    ExpressionEditor.EditorUtil.Init();
                    service = new OpenRPA.ExpressionEditor.EditorService(designer);
                }
                return service.CreateEditor(importedNamespaces, Variables, text, expressionType);
            }
            var variables = new Dictionary<string, Type>();
            foreach (var variableModel in Variables)
            {
                var variable = variableModel.GetCurrentValue() as System.Activities.LocationReference;
                variables.Add(variable.Name, variable.Type);
            }
            EditorInstance instance = new EditorInstance
            {
                Text = text,
                expressionType = expressionType,
                designer = designer,
                Variables = variables,
                Namespaces = namespaces.ToArray()
            };
            return instance;
        }
        private Interfaces.IDesigner designer = null;
        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType)
        {
            return CreateEditor(importedNamespaces, variables, text, expressionType, Size.Empty);
        }
        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType, Size initialSize)
        {
            return CreateEditor(importedNamespaces, variables, text, expressionType, initialSize);
        }
        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text)
        {
            return CreateEditor(importedNamespaces, variables, text, null, Size.Empty);
        }
        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Size initialSize)
        {
            return CreateEditor(importedNamespaces, variables, text, null, initialSize);
        }
        public void CloseExpressionEditors()
        {
        }
        public void UpdateContext(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces)
        {
        }
    }
}
