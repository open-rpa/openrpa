using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;

namespace OpenRPA.Office.Snippets
{
    public class ExcelLoop : ISnippet
    {
        public string Name => "ExcelLoop";
        public string Category => "Excel";
        public string Xaml => Extensions.ResourceAsString(typeof(ExcelLoop), "ExcelLoop.xaml");
        public snippet Snippet { get; set; }
    }
}
