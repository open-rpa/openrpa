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
    public class ImageForm : ISnippet
    {
        public string Name => "ImageForm";
        public string Category => "Form";
        public string Xaml => Interfaces.Extensions.ResourceAsString(typeof(ImageForm), "ImageForm.xaml");
        public snippet Snippet { get; set; }
    }
}
