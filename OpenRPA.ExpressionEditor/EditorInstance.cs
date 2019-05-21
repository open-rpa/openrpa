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
        /// <summary>The pop-up window that will display the auto-completion options.</summary>
        private AutoCompletionPopup completionPanel;
        /// <summary>
        /// Gets or sets the root node of the expression tree that auto-completion editing is based on.
        /// </summary>
        public ExpressionNode AutoCompletionList { get; set; }
        /// <summary>
        /// Gets or sets the language-specific keywords (operators, types, etc.)
        /// </summary>
        public List<string> LanguageKeywords { get; set; }
        public event EventHandler TextChanged;
        public event EventHandler LostAggregateFocus;
        public event EventHandler GotAggregateFocus;
        public event EventHandler Closing;
        /// <summary>
        /// Creates a new instance of the <see cref="EditorInstance" /> class.
        /// </summary>
        public EditorInstance()
        {
            Id = Guid.NewGuid();

            editor = new TextBox();
            editor.KeyDown += Editor_KeyDown;
            editor.GotFocus += Editor_GotFocus;
            editor.LostFocus += Editor_LostFocus;
            editor.PreviewKeyDown += Editor_KeyPress;
            editor.TextChanged += Editor_TextChanged;
            editor.Unloaded += Editor_Unloaded;
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
        public Control HostControl
        {
            get { return editor; }
        }
        public string Text
        {
            get { return editor.Text; }
            set { editor.Text = value; }
        }
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return editor.VerticalScrollBarVisibility; }
            set { editor.VerticalScrollBarVisibility = value; }
        }
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return editor.HorizontalScrollBarVisibility; }
            set { editor.HorizontalScrollBarVisibility = value; }
        }
        public int MinLines
        {
            get { return editor.MinLines; }
            set { editor.MinLines = value; }
        }
        public int MaxLines
        {
            get { return editor.MaxLines; }
            set { editor.MaxLines = value; }
        }
        public bool HasAggregateFocus => true;
        public bool AcceptsReturn
        {
            get { return editor.AcceptsReturn; }
            set { editor.AcceptsReturn = value; }
        }
        public bool AcceptsTab
        {
            get { return editor.AcceptsTab; }
            set { editor.AcceptsTab = value; }
        }
        private void Editor_LostFocus(object sender, RoutedEventArgs e)
        {
            LostAggregateFocus?.Invoke(sender, EventArgs.Empty);
        }
        public void Close()
        {
        }
        public void Focus()
        {
            editor.Focus();
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
            return editor.CanUndo;
        }
        public bool CanRedo()
        {
            return editor.CanRedo;
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
            return editor.Text;
        }
        /// <summary>Returns the current term being auto-completed in the editor.</summary>
        private string GetCurrentTerm()
        {
            string term = editor.Text;

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

            foreach (string keyword in LanguageKeywords)
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
            if (completionPanel != null && completionPanel.IsOpen) HidePopup();

            completionPanel = new AutoCompletionPopup
            {
                DataContext = rootNodes,
                PlacementTarget = editor,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
            };

            completionPanel.ListBoxItemDoubleClick += ListItem_DoubleClick;
        }
        /// <summary>Hides and unloads the auto-completion pop-up window.</summary>
        private void HidePopup()
        {
            if (completionPanel == null) return;
            completionPanel.ListBoxItemDoubleClick -= ListItem_DoubleClick;

            if (completionPanel.IsOpen) completionPanel.IsOpen = false;

            completionPanel = null;
        }
        /// <summary>Displays the auto-completion pop-up list box with the default root nodes 
        /// and VB language keywords.</summary>
        private void ShowRootAutoCompletionList()
        {
            List<ExpressionNode> keywords = new List<ExpressionNode>(AutoCompletionList.Nodes);
            keywords.AddRange(GetKeywordsInScope(""));
            ShowPopup(keywords);
            completionPanel.IsOpen = true;
        }
        /// <summary>Filters the auto-completion list based on text that has been entered and then
        /// displays the auto-completion pop-up pane with that content.</summary>
        private void ShowCustomAutoCompletionList()
        {
            string term = editor.Text;

            if (term.Contains(" ")) term = term.Substring(term.LastIndexOf(" ") + 1);

            if (term.Trim() == "")
            {
                ShowRootAutoCompletionList();
                return;
            }

            List<ExpressionNode> completionTerms =
                ExpressionNode.SubsetAutoCompletionList(AutoCompletionList, term);
            ShowPopup(completionTerms);
            completionPanel.IsOpen = true;
        }
        /// <summary>Occurs when a key is pressed down with focus on the editor text-box.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (!AcceptsTab && e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                TraversalRequest navigator = new TraversalRequest(FocusNavigationDirection.Next);
                UIElement element = Keyboard.FocusedElement as UIElement;

                if (element != null)
                {
                    element.MoveFocus(navigator);
                }
            }
            if (completionPanel != null && completionPanel.IsOpen)
            {

            }
        }
        /// <summary>Occurs when a key is pressed with focus on the editor text-box.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        private void Editor_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && completionPanel != null)
            {
                HidePopup();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (completionPanel == null || !completionPanel.IsOpen) return;

                if (e.Key == Key.Up) completionPanel.SelectedIndex -= 1;
                else completionPanel.SelectedIndex += 1;

                e.Handled = true;
                return;
            }
            if (e.Key == Key.PageUp || e.Key == Key.PageDown)
            {
                if (completionPanel == null || !completionPanel.IsOpen) return;

                if (e.Key == Key.PageUp) completionPanel.SelectedIndex -= 5;
                else completionPanel.SelectedIndex += 5;

                e.Handled = true;
                return;
            }
            if (e.Key == Key.Home || e.Key == Key.End)
            {
                if (completionPanel == null || !completionPanel.IsOpen) return;

                if (e.Key == Key.Home) completionPanel.SelectedIndex = 0;
                else completionPanel.SelectedIndex = completionPanel.Count-1;

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (editor.Text == "" || editor.Text.EndsWith(" "))
                {
                    // Display the default auto-completion list (root nodes and VB language options).
                    ShowRootAutoCompletionList();
                }
                else // Set a custom (filtered) list.
                {
                    ShowCustomAutoCompletionList();
                }

                e.Handled = true;
            }

            if (completionPanel != null && completionPanel.IsOpen &&
            (e.Key == Key.Enter || e.Key == Key.Tab))
    {
                string term = GetCurrentTerm();

                // Check if a word has been explicitly selected first.
                string completionWord = "";

                if (completionPanel != null && completionPanel.IsOpen &&
                completionPanel.SelectedIndex > -1)
        {
                    completionWord = completionPanel.SelectedItem.Name;
                }

                List<ExpressionNode> completionTerms = new List<ExpressionNode>();

                if (completionWord == "")
                {
                    completionTerms =
                        ExpressionNode.SubsetAutoCompletionList(AutoCompletionList, term);

                    var test1 = GetKeywordsInScope(term);
                    if (!term.Contains(".")) completionTerms.AddRange(GetKeywordsInScope(term));

                    if (completionTerms.Count > 0) completionWord = completionTerms[0].Name;
                    //if (completionTerms.Count == 1) completionWord = completionTerms[0].Name;
                }

                if (completionWord != "")
                {
                    if (term.Contains(".")) term = term.Substring(term.LastIndexOf(".") + 1);

                    // Finish off the term...
                    editor.Text =
                        editor.Text.Substring(0, editor.Text.Length - term.Length) +
                        completionWord;
                    editor.SelectionStart = editor.Text.Length;
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
                if (completionPanel != null && completionPanel.IsOpen) HidePopup();
                LostAggregateFocus?.Invoke(sender, EventArgs.Empty);
            }
        }
        /// <summary>Occurs when the text in editor text-box has changed.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        private void Editor_TextChanged(object sender, EventArgs e)
        {
            Text = editor.Text;

            if (completionPanel == null)
            {
                if (editor.Text == "" || editor.Text.EndsWith(" "))
                {
                    // Display the root auto-completion list and the VB language keywords as 
                    // the auto-completion options.
                    ShowRootAutoCompletionList();
                }

                return;
            }

            string term = GetCurrentTerm();

            List<ExpressionNode> rootNodes =
                ExpressionNode.SubsetAutoCompletionList(AutoCompletionList, term);

            if (!term.Contains("."))
            {
                // Add the VB language keywords as we aren't definitely targeting a 
                // namespace or type yet...
                rootNodes.AddRange(GetKeywordsInScope(term));
            }

            if (rootNodes.Count == 0)
            {
                HidePopup();
                return;
            }

            completionPanel.DataContext = rootNodes;
            TextChanged?.Invoke(sender, e);
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

            string term = GetCurrentTerm();
            string output = editor.Text;

            if (term.Contains("."))
            {
                output = output.Substring(0, output.Length - term.Length) +
                    term.Substring(0, term.LastIndexOf(".") + 1) + node.Name;
            }
            else
            {
                output = output.Substring(0, output.Length - term.Length) + node.Name;
            }

            editor.Text = output;
            editor.SelectionStart = editor.Text.Length;
            editor.Focus();
        }
    }
}
