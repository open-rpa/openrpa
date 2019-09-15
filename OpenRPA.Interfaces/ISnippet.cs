using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface ISnippet
    {
        string Name { get; }
        string Category { get; }
        string Xaml { get; }
        entity.snippet Snippet { get; set; }
    }
}
