using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Database
{
    public partial class ExecuteScalarDesigner
    {
        public ExecuteScalarDesigner()
        {
            InitializeComponent();
        }
        protected override void OnModelItemChanged(object newItem)
        {
            base.OnModelItemChanged(newItem);
            GenericArgumentTypeUpdater.Attach(ModelItem);
        }
    }
}