using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenRPA.Interfaces;
using System.Collections.ObjectModel;
using System.Activities.Presentation;

namespace OpenRPA.WorkItems
{
    public partial class BulkAddWorkitemsDesigner
    {
        public BulkAddWorkitemsDesigner()
        {
            InitializeComponent();
            DataContext = this;
        }
        public ObservableCollection<IWorkitemQueue> WorkItemQueues { get { return global.OpenRPAClient.WorkItemQueues;  } }
    }
}