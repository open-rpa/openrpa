using OpenRPA.Interfaces;
using System;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRPA.CodeEditor
{
    public class EditorInstance : CodeEditor, IExpressionEditorInstance
    {
        // public new event EventHandler TextChanged;
        public event EventHandler LostAggregateFocus;
        public event EventHandler GotAggregateFocus;
        public event EventHandler Closing;
        public Type expressionType = null;
        public IDesigner designer = null;
        /// <summary>
        /// Creates a new instance of the <see cref="EditorInstance" /> class.
        /// </summary>
        public EditorInstance()
        {
            Id = Guid.NewGuid();
            var lang = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.HighlightingDefinitions.Where(x => x.Name == "VB").FirstOrDefault();
            SyntaxHighlighting = lang;
            GotFocus += Editor_GotFocus;
            LostFocus += Editor_LostFocus;
            Unloaded += Editor_Unloaded;
        }
        private void Editor_Unloaded(object sender, RoutedEventArgs e)
        {
            Closing?.Invoke(sender, EventArgs.Empty);
        }
        private void Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            GotAggregateFocus?.Invoke(sender, e);
        }

        /// <summary>
        /// Gets the unique ID of the editor instance.
        /// </summary>
        public Guid Id { get; private set; }
        public System.Windows.Controls.Control HostControl
        {
            get { return this; }
        }
        public int MinLines
        {
            get { return 1; }
            set { _ = value; }
        }
        public int MaxLines
        {
            get { return 10; }
            set { _ = value; }
        }
        public bool HasAggregateFocus => true;
        public bool AcceptsReturn
        {
            get { return true; }
            set { _ = value; }
        }
        public bool AcceptsTab
        {
            get { return true; }
            set { _ = value; }
        }
        private void Editor_LostFocus(object sender, RoutedEventArgs e)
        {
            LostAggregateFocus?.Invoke(sender, EventArgs.Empty);
        }
        public void Close()
        {
        }
        public void ClearSelection()
        {
        }
        public bool CompleteWord()
        {
            return true;
        }
        public bool GlobalIntellisense()
        {
            return false;
        }
        public bool ParameterInfo()
        {
            return true;
        }
        public bool QuickInfo()
        {
            return true;
        }
        public bool IncreaseFilterLevel()
        {
            return false;
        }
        public bool DecreaseFilterLevel()
        {
            return false;
        }
        public bool CanCut()
        {
            return true;
        }
        public bool CanCopy()
        {
            return true;
        }
        public bool CanPaste()
        {
            return true;
        }
        public bool CanCompleteWord()
        {
            return true;
        }
        public bool CanGlobalIntellisense()
        {
            return false;
        }
        public bool CanParameterInfo()
        {
            return false;
        }
        public bool CanQuickInfo()
        {
            return false;
        }
        public bool CanIncreaseFilterLevel()
        {
            return false;
        }
        public bool CanDecreaseFilterLevel()
        {
            return false;
        }
        public string GetCommittedText()
        {
            return Text;
        }

        void IExpressionEditorInstance.Focus()
        {
            Focus();
        }

        bool IExpressionEditorInstance.Cut()
        {
            Cut();
            return true;
        }

        bool IExpressionEditorInstance.Copy()
        {
            Copy();
            return true;
        }

        bool IExpressionEditorInstance.Paste()
        {
            Paste();
            return true;
        }

        bool IExpressionEditorInstance.CanUndo()
        {
            return CanUndo;
        }

        bool IExpressionEditorInstance.CanRedo()
        {
            return CanRedo;
        }
    }
}
