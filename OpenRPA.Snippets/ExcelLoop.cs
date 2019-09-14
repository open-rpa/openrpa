using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;

namespace OpenRPA.Snippets
{
    public class ExcelLoop : Interfaces.ISnippet
    {
        public string Name => "ExcelLoop";
        public string Category => "Excel";
        public string Xaml => Extensions.ResourceAsString("ExcelLoop.xaml");
        public snippet Snippet { get; set; }
        public Activity Create(DependencyObject target)
        {
            try
            {
                return System.Xaml.XamlServices.Load(new System.IO.StringReader(Xaml)) as Activity;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
    }
}
