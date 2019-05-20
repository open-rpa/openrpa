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

        private IExpressionEditorInstance CreateEditor(    AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces,
    List<ModelItem> variables, string text, Type expressionType)
        {
            EditorInstance instance = new EditorInstance
            {
                AutoCompletionList = this.AddVariablesToAutoCompletionList(variables),
                LanguageKeywords = this.LanguageKeywords,
                Text = text
            };
            return instance;
        }

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

        /// <summary>
        /// Returns an updated auto-completion expression tree that uses the baseline data in the 
        /// <see cref="AutoCompletionData" /> property and appends the specified variable names.
        /// </summary>
        /// <param name="variables">The collection of variables to include.</param>
        private ExpressionNode AddVariablesToAutoCompletionList(List<ModelItem> variables)
        {
            ExpressionNode data = this.AutoCompletionData;
            Type systemType;
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
                    data.Nodes.Add(new ExpressionNode
                    {
                        Name = computedName,
                        ItemType = "variable",
                        Description = "Variable: " + computedName
                    });
                }
            }

            return data;
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType)
        {
            EditorInstance instance = new EditorInstance
            {
                AutoCompletionList = this.AddVariablesToAutoCompletionList(variables),
                LanguageKeywords = this.LanguageKeywords,
                Text = text
            };
            return instance;
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType, Size initialSize)
        {
            EditorInstance instance = new EditorInstance
            {
                AutoCompletionList = this.AddVariablesToAutoCompletionList(variables),
                LanguageKeywords = this.LanguageKeywords,
                Text = text
            };
            return instance;
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text)
        {
            EditorInstance instance = new EditorInstance
            {
                AutoCompletionList = this.AddVariablesToAutoCompletionList(variables),
                LanguageKeywords = this.LanguageKeywords,
                Text = text
            };
            return instance;
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Size initialSize)
        {
            EditorInstance instance = new EditorInstance
            {
                AutoCompletionList = this.AddVariablesToAutoCompletionList(variables),
                LanguageKeywords = this.LanguageKeywords,
                Text = text
            };
            return instance;
        }

        public void CloseExpressionEditors()
        {
            throw new NotImplementedException();
        }

        public void UpdateContext(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces)
        {
            // throw new NotImplementedException();
        }
    }
}
