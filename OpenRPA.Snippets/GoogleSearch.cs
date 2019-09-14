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
    public class GoogleSearch : Interfaces.ISnippet
    {
        public string Name => "GoogleSearch";
        public string Category => "Chrome";
        public string Xaml => Extensions.ResourceAsString("GoogleSearch.xaml");
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
