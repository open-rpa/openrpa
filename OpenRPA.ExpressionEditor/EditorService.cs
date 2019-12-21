using System;
using System.Activities.Presentation.Hosting;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRPA.ExpressionEditor
{
    public class EditorService : IExpressionEditorService
    {
        public EditorService(Interfaces.IDesigner designer)
        {
            this.designer = designer;
            AutoCompletionData = EditorUtil.autoCompletionTree;
        }
        public IExpressionEditorInstance CreateEditor(ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType)
        {
            ExpressionNode data = AutoCompletionData;
            data = AddVariablesToAutoCompletionList(data, variables);
            data = AddNamespacesToAutoCompletionList(data, importedNamespaces);
            EditorInstance instance = new EditorInstance
            {
                AutoCompletionList = data,
                LanguageKeywords = LanguageKeywords,
                Text = text,
                expressionType = expressionType,
                designer = designer
            };
            return instance;
        }
        private Interfaces.IDesigner designer = null;
        /// <summary>
        /// Gets a collection of editing language keywords the editor should support.
        /// </summary>
        public List<string> LanguageKeywords
        {
            get
            {
                return new List<string>(new[] { "And", "AndAlso", "New", "Or", "OrElse", "Throw" });
            }
        }
        /// <summary>
        /// Gets or sets the root node in the expression auto-completion tree.
        /// </summary>
        public ExpressionNode AutoCompletionData { get; set; }
        private ExpressionNode AddNamespacesToAutoCompletionList(ExpressionNode data, ImportedNamespaceContextItem importedNamespaces)
        {
            foreach(var ns in importedNamespaces.ImportedNamespaces)
            {

                var foundNodes = ExpressionNode.SearchForNode(data, ns, true, true);
                foreach(var node in foundNodes.Nodes)
                {
                    data.Add(node);
                }
            }
            return data;
        }
        /// <summary>
        /// Returns an updated auto-completion expression tree that uses the baseline data in the 
        /// <see cref="AutoCompletionData" /> property and appends the specified variable names.
        /// </summary>
        /// <param name="variables">The collection of variables to include.</param>
        private ExpressionNode AddVariablesToAutoCompletionList(ExpressionNode data, List<ModelItem> variables)
        {
            //Type systemType;
            ModelProperty property;

            foreach (ModelItem item in variables)
            {
                property = item.Properties["Name"];

                if (property == null) continue;

                string computedName = property.ComputedValue.ToString();
                List<ExpressionNode> results = new List<ExpressionNode>(from x in data.Nodes
                                                                        where
x.Name.Equals(computedName)
                                                                        select x);

                if (results.Count == 0)
                {
                    Type child = typeof(string);
                    if (item.ItemType.GenericTypeArguments != null && item.ItemType.GenericTypeArguments.Length > 0)
                    {
                        child = item.ItemType.GenericTypeArguments[0];
                    }
                    var entityNode = new ExpressionNode
                    {
                        Name = computedName,
                        ItemType = "variable",
                        Description = "Variable: " + computedName
                    };
                    EditorUtil.AddFieldNodes(entityNode, child);
                    EditorUtil.AddPropertyNodes(entityNode, child);
                    EditorUtil.AddMethodNodes(entityNode, child);
                    data.Nodes.Add(entityNode);

                }
            }

            return data;
        }
        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType)
        {
            return CreateEditor(importedNamespaces, variables, text, expressionType);
        }
        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType, Size initialSize)
        {
            return CreateEditor(importedNamespaces, variables, text, expressionType);
        }
        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text)
        {
            return CreateEditor(importedNamespaces, variables, text, null);
        }
        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Size initialSize)
        {
            return CreateEditor(importedNamespaces, variables, text, null);
        }
        public void CloseExpressionEditors()
        {
        }
        public void UpdateContext(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces)
        {
        }
    }
}
