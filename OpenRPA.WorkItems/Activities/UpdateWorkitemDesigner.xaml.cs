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

namespace OpenRPA.WorkItems
{
    public class workitemstatus
    {
        public workitemstatus(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public partial class UpdateWorkitemDesigner
    {
        public ObservableCollection<workitemstatus> WIQStates { get; set; } = new ObservableCollection<workitemstatus>();
        public UpdateWorkitemDesigner()
        {
            InitializeComponent();
            WIQStates.Add(new workitemstatus("Successful", "Successful"));
            WIQStates.Add(new workitemstatus("Failed", "Failed"));
            WIQStates.Add(new workitemstatus("Processing", "Processing"));
            WIQStates.Add(new workitemstatus("Retry", "Retry"));
        }
    }
}