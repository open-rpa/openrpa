using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
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

namespace OpenRPA.Script.Activities
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Editor : Window
    {
        public Editor()
        {
            InitializeComponent();
            DataContext = this;
            foreach(var hi in HighlightingManager.Instance.HighlightingDefinitions)
            {
                if (hi.Name == "VB" || hi.Name == "C#" || hi.Name == "PowerShell") Languages.Add(hi);
            }

        }
        public System.Collections.ObjectModel.ObservableCollection<IHighlightingDefinition> Languages { get; set; } = new System.Collections.ObjectModel.ObservableCollection<IHighlightingDefinition> ();

    }
}
