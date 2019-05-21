using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenRPA.ExpressionEditor
{
    /// <summary>
    /// Interaction logic for AutoCompletionPopup.xaml
    /// </summary>
    public partial class AutoCompletionPopup : ToolTip
    {
        public AutoCompletionPopup()
        {
            InitializeComponent();
        }

        /// <summary>Occurs when an item in the auto-completion list is double clicked.</summary>
        public event MouseButtonEventHandler ListBoxItemDoubleClick;

        /// <summary>Gets or sets the selected item in the auto-completion list</summary>
        public ExpressionNode SelectedItem
        {
            get { return (ExpressionNode)this.AutoCompletionListBox.SelectedItem; }
            set { this.AutoCompletionListBox.SelectedItem = value; }
        }

        /// <summary>Gets or sets the index of the selected item in the auto-completion list.</summary>
        public int SelectedIndex
        {
            get { return this.AutoCompletionListBox.SelectedIndex; }
            set
            {
                // Protect against the index under- or over-ranging.
                if (value < -1 || value > this.AutoCompletionListBox.Items.Count - 1) return;

                this.AutoCompletionListBox.SelectedIndex = value;
                this.AutoCompletionListBox.ScrollIntoView(this.AutoCompletionListBox.SelectedItem);
            }
        }

        /// <summary>Gets the number of items in the auto-completion list.</summary>
        public int Count
        {
            get { return this.AutoCompletionListBox.Items.Count; }
        }
        /// <summary>Occurs when an item in the auto-completion list is double-clicked.</summary>
        /// <param name="sender">The object from which the event initiated.</param>
        /// <param name="e">The object that contains the event data.</param>
        protected void OnListBoxItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.ListBoxItemDoubleClick != null) this.ListBoxItemDoubleClick.Invoke(sender, e);
        }
    }
}
