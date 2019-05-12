using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IElement
    {
        void Focus();
        void Click();
        void Highlight(bool Blocking, System.Drawing.Color Color, TimeSpan Duration);
    }
}
