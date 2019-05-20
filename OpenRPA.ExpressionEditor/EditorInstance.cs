using System;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenRPA.ExpressionEditor
{
    public class EditorInstance : IExpressionEditorInstance
    {
        /// <summary>
        /// The text box auto-completion editing is being done in.
        /// </summary>
        private readonly TextBox editor;

        public event EventHandler TextChanged;
        public event EventHandler LostAggregateFocus;
        public event EventHandler GotAggregateFocus;
        public event EventHandler Closing;

        /// <summary>
        /// Creates a new instance of the <see cref="EditorInstance" /> class.
        /// </summary>
        public EditorInstance()
        {
            this.Id = Guid.NewGuid();

            this.editor = new TextBox();
            this.editor.KeyDown += this.Editor_KeyDown;
            this.editor.LostFocus += this.Editor_LostFocus;
            this.editor.PreviewKeyDown += this.Editor_KeyPress;
            this.editor.TextChanged += this.Editor_TextChanged;
        }

        /// <summary>
        /// Gets or sets the root node of the expression tree that auto-completion editing is based on.
        /// </summary>
        public ExpressionNode AutoCompletionList { get; set; }

        /// <summary>
        /// Gets or sets the language-specific keywords (operators, types, etc.)
        /// </summary>
        public List<string> LanguageKeywords { get; set; }

        /// <summary>
        /// Gets the unique ID of the editor instance.
        /// </summary>
        public Guid Id { get; private set; }

        public Control HostControl
        {
            get { return this.editor; }
        }

        public string Text
        {
            get { return this.editor.Text; }
            set { this.editor.Text = value; }
        }
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return this.editor.VerticalScrollBarVisibility; }
            set { this.editor.VerticalScrollBarVisibility = value; }
        }
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return this.editor.HorizontalScrollBarVisibility; }
            set { this.editor.HorizontalScrollBarVisibility = value; }
        }
        public int MinLines
        {
            get { return this.editor.MinLines; }
            set { this.editor.MinLines = value; }
        }
        public int MaxLines
        {
            get { return this.editor.MaxLines; }
            set { this.editor.MaxLines = value; }
        }

        public bool HasAggregateFocus => true;

        public bool AcceptsReturn
        {
            get { return this.editor.AcceptsReturn; }
            set { this.editor.AcceptsReturn = value; }
        }
        public bool AcceptsTab
        {
            get { return this.editor.AcceptsTab; }
            set { this.editor.AcceptsTab = value; }
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            // throw new NotImplementedException();
        }


        private void Editor_LostFocus(object sender, RoutedEventArgs e)
        {
            // throw new NotImplementedException();
        }

        public void Close()
        {
        }

        public void Focus()
        {
            this.editor.Focus();
        }

        public void ClearSelection()
        {
        }

        public bool Cut()
        {
            return true;
        }

        public bool Copy()
        {
            return true;
        }

        public bool Paste()
        {
            return true;
        }

        public bool Undo()
        {
            return true;
        }

        public bool Redo()
        {
            return true;
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

        public bool CanUndo()
        {
            return this.editor.CanUndo;
        }

        public bool CanRedo()
        {
            return this.editor.CanRedo;
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
            return this.editor.Text;
        }








        /// <summary>The pop-up window that will display the auto-completion options.</summary>
        private AutoCompletionPopup completionPanel;

        /// <summary>Returns the current term being auto-completed in the editor.</summary>
        private string GetCurrentTerm()
        {
            string term = this.editor.Text;

            if (term.Contains(" "))
            {
                term = term.Substring(term.LastIndexOf(' ') + 1);
            }

            return term;
        }

        /// <summary>Returns a collection of the VB language keywords in scope, based on the 
        /// current filter.</summary>
        /// <param name="filter">The filter to use to sub-set the keywords list.</param>
        private List<ExpressionNode> GetKeywordsInScope(string filter)
        {
            List<ExpressionNode> keywords = new List<ExpressionNode>();

            foreach (string keyword in this.LanguageKeywords)
            {
                if (string.IsNullOrEmpty(filter) ||
                    keyword.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                {
                    keywords.Add(new ExpressionNode
                    {
                        Description = "Visual Basic Keyword: " + keyword,
                        Name = keyword,
                        ItemType = "keyword",
                        Parent = null
                    });
                }
            }

            return keywords;
        }

        /// <summary>Instantiates and displays a new auto-completion window using the supplied 
        /// auto-completion tree.</summary>
        /// 
        private void ShowPopup(List<ExpressionNode> rootNodes)
        {
            if (this.completionPanel != null && this.completionPanel.IsOpen) this.HidePopup();

            this.completionPanel = new AutoCompletionPopup
            {
                DataContext = rootNodes,
                PlacementTarget = this.editor,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
            };

            this.completionPanel.ListBoxItemDoubleClick += this.ListItem_DoubleClick;
        }

        /// <summary>Hides and unloads the auto-completion pop-up window.</summary>
        private void HidePopup()
        {
            if (this.completionPanel == null) return;
            this.completionPanel.ListBoxItemDoubleClick -= this.ListItem_DoubleClick;

            if (this.completionPanel.IsOpen) this.completionPanel.IsOpen = false;

            this.completionPanel = null;
        }

        /// <summary>Displays the auto-completion pop-up list box with the default root nodes 
        /// and VB language keywords.</summary>
        private void ShowRootAutoCompletionList()
        {
            List<ExpressionNode> keywords = new List<ExpressionNode>(this.AutoCompletionList.Nodes);
            keywords.AddRange(this.GetKeywordsInScope(""));
            this.ShowPopup(keywords);
            this.completionPanel.IsOpen = true;
        }

        /// <summary>Filters the auto-completion list based on text that has been entered and then
        /// displays the auto-completion pop-up pane with that content.</summary>
        private void ShowCustomAutoCompletionList()
        {
            string term = this.editor.Text;

            if (term.Contains(" ")) term = term.Substring(term.LastIndexOf(" ") + 1);

            if (term.Trim() == "")
            {
                this.ShowRootAutoCompletionList();
                return;
            }

            List<ExpressionNode> completionTerms =
                ExpressionNode.SubsetAutoCompletionList(this.AutoCompletionList, term);
            this.ShowPopup(completionTerms);
            this.completionPanel.IsOpen = true;
        }

        /// <summary>Occurs when a key is pressed down with focus on the editor text-box.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (!this.AcceptsTab && e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                TraversalRequest navigator = new TraversalRequest(FocusNavigationDirection.Next);
                UIElement element = Keyboard.FocusedElement as UIElement;

                if (element != null)
                {
                    element.MoveFocus(navigator);
                }
            }
        }

        /// <summary>Occurs when a key is pressed with focus on the editor text-box.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        private void Editor_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && this.completionPanel != null)
            {
                this.HidePopup();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (this.completionPanel == null || !this.completionPanel.IsOpen) return;

                if (e.Key == Key.Up) this.completionPanel.SelectedIndex -= 1;
                else this.completionPanel.SelectedIndex += 1;

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (this.editor.Text == "" || this.editor.Text.EndsWith(" "))
                {
                    // Display the default auto-completion list (root nodes and VB language options).
                    this.ShowRootAutoCompletionList();
                }
                else // Set a custom (filtered) list.
                {
                    this.ShowCustomAutoCompletionList();
                }

                e.Handled = true;
            }

            if (this.completionPanel != null && this.completionPanel.IsOpen &&
            (e.Key == Key.Enter || e.Key == Key.Tab))
    {
                string term = this.GetCurrentTerm();

                // Check if a word has been explicitly selected first.
                string completionWord = "";

                if (this.completionPanel != null && this.completionPanel.IsOpen &&
                this.completionPanel.SelectedIndex > -1)
        {
                    completionWord = this.completionPanel.SelectedItem.Name;
                }

                List<ExpressionNode> completionTerms = new List<ExpressionNode>();

                if (completionWord == "")
                {
                    completionTerms =
                        ExpressionNode.SubsetAutoCompletionList(this.AutoCompletionList, term);

                    if (!term.Contains(".")) completionTerms.AddRange(this.GetKeywordsInScope(term));

                    if (completionTerms.Count == 1) completionWord = completionTerms[0].Name;
                }

                if (completionWord != "")
                {
                    if (term.Contains(".")) term = term.Substring(term.LastIndexOf(".") + 1);

                    // Finish off the term...
                    this.editor.Text =
                        this.editor.Text.Substring(0, this.editor.Text.Length - term.Length) +
                        completionWord;
                    this.editor.SelectionStart = this.editor.Text.Length;
                }

                e.Handled = true;
            }
        }

        /// <summary>Occurs when the editor text-box looses focus.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        private void Editor_LostFocus(object sender, EventArgs e)
        {
            ListBoxItem item = Keyboard.FocusedElement as ListBoxItem;

            if (item == null)
            {
                if (this.completionPanel != null && this.completionPanel.IsOpen) this.HidePopup();
                this.LostAggregateFocus.Invoke(sender, e);
            }
        }

        /// <summary>Occurs when the text in editor text-box has changed.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        private void Editor_TextChanged(object sender, EventArgs e)
        {
            this.Text = this.editor.Text;

            if (this.completionPanel == null)
            {
                if (this.editor.Text == "" || this.editor.Text.EndsWith(" "))
                {
                    // Display the root auto-completion list and the VB language keywords as 
                    // the auto-completion options.
                    this.ShowRootAutoCompletionList();
                }

                return;
            }

            string term = this.GetCurrentTerm();

            List<ExpressionNode> rootNodes =
                ExpressionNode.SubsetAutoCompletionList(this.AutoCompletionList, term);

            if (!term.Contains("."))
            {
                // Add the VB language keywords as we aren't definitely targeting a 
                // namespace or type yet...
                rootNodes.AddRange(this.GetKeywordsInScope(term));
            }

            if (rootNodes.Count == 0)
            {
                this.HidePopup();
                return;
            }

            this.completionPanel.DataContext = rootNodes;
        }

        /// <summary>Occurs when an item in the auto-completion pop-up is double-clicked.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        private void ListItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;
            if (item == null) return;

            ExpressionNode node = item.DataContext as ExpressionNode;
            if (node == null) return;

            string term = this.GetCurrentTerm();
            string output = this.editor.Text;

            if (term.Contains("."))
            {
                output = output.Substring(0, output.Length - term.Length) +
                    term.Substring(0, term.LastIndexOf(".") + 1) + node.Name;
            }
            else
            {
                output = output.Substring(0, output.Length - term.Length) + node.Name;
            }

            this.editor.Text = output;
            this.editor.SelectionStart = this.editor.Text.Length;
            this.editor.Focus();
        }

    }
}
