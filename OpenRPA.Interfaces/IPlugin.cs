using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IPlugin
    {
        string Name { get; }
        void Initialize();
        void Start();
        void Stop();
        event Action<IPlugin, IRecordEvent> OnUserAction;
        bool parseUserAction(ref IRecordEvent e);
        Selector.treeelement[] GetRootElements();
        Selector.Selector GetSelector(Selector.treeelement item);
        IElement[] GetElementsWithSelector(Selector.Selector selector, IElement fromElement = null);

    }
}
