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

namespace OpenRPA.TerminalEmulator
{
    public partial class GetTextAtDesigner
    {
        public GetTextAtDesigner()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var Row = ModelItem.GetValue<int>("Row");
            var Column = ModelItem.GetValue<int>("Column");
            var Length = ModelItem.GetValue<int>("Length");
            if(Length > 0 && Column > -1 && Row > -1)
            {
                if(RunPlugin.Sessions.Count == 0) { Log.Output("No active sessions to test in."); return; }
                var session = RunPlugin.Sessions.First();
                if (RunPlugin.Sessions.Count > 1) { Log.Output("Found more than one session, getting from session " + session.WorkflowInstanceId); }
                var result = session.Terminal.GetTextAt(Column, Row, Length);
                Log.Output(result);
                if (string.IsNullOrEmpty(result)) MessageBox.Show("Empty string returned");
                if (!string.IsNullOrEmpty(result)) MessageBox.Show(result);
            }

        }
    }
}