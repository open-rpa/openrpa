using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    public class Plugin : IPlugin
    {
        public string Name => throw new NotImplementedException();

        public event Action<IPlugin, IRecordEvent> OnUserAction;

        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            throw new NotImplementedException();
        }

        public treeelement[] GetRootElements(Selector anchor)
        {
            throw new NotImplementedException();
        }

        public Selector GetSelector(Selector anchor, treeelement item)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            NMHook.registreChromeNativeMessagingHost(false);
            NMHook.registreffNativeMessagingHost(false);
        }

        public void LaunchBySelector(Selector selector, TimeSpan timeout)
        {
        }

        public bool Match(SelectorItem item, IElement m)
        {
            throw new NotImplementedException();
        }

        public bool parseUserAction(ref IRecordEvent e)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}
