using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Activities
{
    public partial class InvokeOpenFlowDesigner : INotifyPropertyChanged
    {
        public InvokeOpenFlowDesigner()
        {
            InitializeComponent();
            workflows = new ObservableCollection<apibase>();
            DataContext = this;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<apibase> workflows { get; set; }
        private async void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (workflows.Count > 0) return;
                var _workflows = await global.webSocketClient.Query<openflowworkflow>("workflow", "{_type: 'workflow', rpa: true}");
                _workflows = _workflows.OrderBy(x => x.name).ToArray();
                workflows.Clear();
                foreach (var w in _workflows)
                {
                    workflows.Add(w);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModelItemDictionary dictionary = base.ModelItem.Properties["Arguments"].Dictionary;
            var options = new System.Activities.Presentation.DynamicArgumentDesignerOptions() { Title = "Map Arguments" };
            using (ModelEditingScope modelEditingScope = dictionary.BeginEdit())
            {
                if (System.Activities.Presentation.DynamicArgumentDialog.ShowDialog(base.ModelItem, dictionary, base.Context, base.ModelItem.View, options))
                {
                    modelEditingScope.Complete();
                }
                else
                {
                    modelEditingScope.Revert();
                }
            }
        }
    }
}