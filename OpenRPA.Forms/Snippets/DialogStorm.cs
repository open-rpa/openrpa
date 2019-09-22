using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;

namespace OpenRPA.Forms.Snippets
{
    public class DialogStorm : ISnippet
    {
        public string Name => "Dialog Storm";
        public string Category => "Form";
        public string Xaml => Interfaces.Extensions.ResourceAsString(typeof(DialogStorm), "DialogStorm.xaml");
        public snippet Snippet { get; set; }
    }
}
