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

namespace OpenRPA.WorkItems.Activities
{
    public partial class DeleteWorkitemDesigner
    {
        public DeleteWorkitemDesigner()
        {
            InitializeComponent();
            _ = RobotInstance.instance;
        }
    }
}