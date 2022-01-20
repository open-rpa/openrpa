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
    public class GoogleSearchPaging : ISnippet
    {
        public string Name => "Google Search Pagination";
        public string Category => "NM";
        public string Xaml => Extensions.ResourceAsString(typeof(GoogleSearch), "GoogleSearchPaging.xaml");
        public snippet Snippet { get; set; }
    }
}
