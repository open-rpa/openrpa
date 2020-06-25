using Microsoft.VisualBasic.Activities;
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
using OpenRPA.Interfaces;

namespace OpenRPA.Utilities
{
    public partial class SetAllRowsAddedDesigner
    {
        public SetAllRowsAddedDesigner()
        {
            InitializeComponent();
        }
        private void ExpressionTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            ((System.Activities.Presentation.View.ExpressionTextBox)sender).ExpressionType = typeof(object[]);
        }
    }
}