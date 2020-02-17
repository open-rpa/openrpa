using OpenRPA.Interfaces;
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

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for OpenProject.xaml
    /// </summary>
    public partial class OpenProject : UserControl
    {
        public DelegateCommand DockAsDocumentCommand = new DelegateCommand((e) => { }, (e) => false);
        public DelegateCommand AutoHideCommand { get; set; } = new DelegateCommand((e) => { }, (e) => false);
        public bool CanClose { get; set; } = false;
        public bool CanHide { get; set; } = false;
        public event Action<Workflow> onOpenWorkflow;
        public event Action<Project> onOpenProject;
        //public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; }
        private MainWindow main = null;
        public ICommand PlayCommand { get { return new RelayCommand<object>(MainWindow.instance.OnPlay, MainWindow.instance.CanPlay); } }
        public ICommand ExportCommand { get { return new RelayCommand<object>(MainWindow.instance.OnExport, MainWindow.instance.CanExport); } }
        public ICommand RenameCommand { get { return new RelayCommand<object>(MainWindow.instance.OnRename, MainWindow.instance.CanRename); } }
        public ICommand DeleteCommand { get { return new RelayCommand<object>(MainWindow.instance.OnDelete2, MainWindow.instance.CanDelete); } }
        // public ICommand DeleteCommand { get { return new RelayCommand<object>(MainWindow.instance.OnDelete, MainWindow.instance.CanDelete); } }
        public ICommand CopyIDCommand { get { return new RelayCommand<object>(MainWindow.instance.OnCopyID, MainWindow.instance.CanCopyID); } }
        public ICommand CopyRelativeFilenameCommand { get { return new RelayCommand<object>(MainWindow.instance.OnCopyRelativeFilename, MainWindow.instance.CanCopyID); } }
        public System.Collections.ObjectModel.ObservableCollection<Project> Projects
        {
            get
            {
                return main.Projects;
            }
        }
        public OpenProject(MainWindow main)
        {
            InitializeComponent();
            this.main = main;
            DataContext = this;
        }
        private void ListWorkflows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listWorkflows.SelectedItem is Workflow f)
            {
                onOpenWorkflow?.Invoke(f);
                return;
            }
            var p = (Project)listWorkflows.SelectedItem;
            if (p == null) return;
            onOpenProject?.Invoke(p);
        }
        private async void ButtonEditXAML(object sender, RoutedEventArgs e)
        {
            if (listWorkflows.SelectedItem is Workflow workflow)
            {
                try
                {
                    var f = new EditXAML();
                    f.XAML = workflow.Xaml;
                    f.ShowDialog();
                    workflow.Xaml = f.XAML;
                    await workflow.Save(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    System.Windows.MessageBox.Show("ButtonEditXAML: " + ex.Message);
                }
            }
        }
    }
}
