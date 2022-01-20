using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;

namespace OpenRPA.NM.Snippets
{
    public class GoogleSearch : ISnippet
    {
        public string Name => "Google Search";
        public string Category => "NM";
        public string Xaml => Extensions.ResourceAsString(typeof(GoogleSearch), "GoogleSearch.xaml");
        public snippet Snippet { get; set; }
    }
}
