using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OpenRPA.TerminalEmulator
{
    public static class Extensions
    {
            public static TextPointer GetEdgeTextPointer(this TextPointer position, LogicalDirection direction)
            {
                string pattern = @" ,;.!""?"; // Delimiters 
                int step = direction == LogicalDirection.Forward ? 1 : -1;
                for (; position != null;)
                {
                    var text = position.GetTextInRun(direction);
                    int offset = 0;
                    int i = direction == LogicalDirection.Forward ? 0 : text.Length - 1;

                    for (; i >= 0 && i < text.Length; offset++, i += step)
                    {
                        if (pattern.Contains(text[i]))
                        {
                            return position.GetPositionAtOffset(offset * step, LogicalDirection.Forward);
                        }
                    }

                    position = position.GetPositionAtOffset(offset * step, LogicalDirection.Forward);
                    for (TextPointer latest = position; ;)
                    {
                        if ((position = position.GetNextContextPosition(direction)) == null)
                            return latest;

                        var context = position.GetPointerContext(direction);
                        var adjacent = position.GetAdjacentElement(direction);
                        if (context == TextPointerContext.Text)
                        {
                            if (position.GetTextInRun(direction).Length > 0)
                                break;
                        }
                        else if (context == TextPointerContext.ElementStart && adjacent is Paragraph)
                        {
                            return latest;
                        }
                    }
                }
                return position;
            }
    }
}
