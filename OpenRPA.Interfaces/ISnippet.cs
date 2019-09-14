using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface ISnippet : System.Activities.Presentation.IActivityTemplateFactory
    {
        string Name { get; }
        string Category { get; }
        string Xaml { get; }
        entity.snippet Snippet { get; set; }
    }
}
