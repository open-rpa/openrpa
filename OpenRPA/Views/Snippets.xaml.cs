using OpenRPA.Interfaces;
using System;
using System.Activities.Presentation.Toolbox;
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

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for Snippets.xaml
    /// </summary>
    public partial class Snippets : UserControl
    {
        public Snippets()
        {
            InitializeComponent();
            DataContext = this;
            toolborder.Child = toolbox;
        }
        public void Reload()
        {
            InitializeSnippets();
        }
        public ToolboxControl toolbox = new ToolboxControl();
        // public static DynamicActivityGenerator dag = new DynamicActivityGenerator("Snippets");
        public static DynamicActivityGenerator dag;
        public void InitializeSnippets()
        {
            if (toolbox.Categories.Count > 0) return;
            try
            {
                if(dag == null)
                {
                    // dag = new DynamicActivityGenerator("Snippets", Interfaces.Extensions.ProjectsDirectory);
                    dag = new DynamicActivityGenerator("Snippets", System.IO.Path.GetTempPath());
                }
                var cs = new Dictionary<string, ToolboxCategory>();
                foreach(var s in Plugins.Snippets)
                {
                    try
                    {
                        if (!cs.ContainsKey(s.Category)) cs.Add(s.Category, new ToolboxCategory(s.Category));
                        ToolboxCategory cat = cs[s.Category];
                        var t = dag.AppendSubWorkflowTemplate(s.Name, s.Xaml);
                        cat.Add(new ToolboxItemWrapper(t, s.Name));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
                try
                {
                    dag.Save();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                if (cs == null || cs.Count == 0)
                {
                    Log.Warning("No snippets!");
                }
                foreach (var c in cs)
                {
                    try
                    {
                        toolbox.Categories.Add(c.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show("InitializeSnippets: " + ex.Message);
            }
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Reload();
        }
    }
}
