using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    class Plugin : IPlugin
    {
        public string Name => "Image";
        public string Status => "Online";
        public event Action<IPlugin, IRecordEvent> OnUserAction;
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
            throw new NotImplementedException();
        }
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
        }
        public void LaunchBySelector(Selector selector, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        public bool Match(SelectorItem item, IElement m)
        {
            throw new NotImplementedException();
        }
        public bool parseUserAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;

            if(e.UIElement.Type != "Pane") return false;
            var element = e.UIElement.RawElement;

            var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
            var Processname = p.ProcessName;

            int newOffsetX; int newOffsetY; System.Drawing.Rectangle resultrect;
            var image = getrectangle.GuessContour(element, e.OffsetX, e.OffsetY, out newOffsetX, out newOffsetY, out resultrect);
            e.OffsetX = newOffsetX;
            e.OffsetY = newOffsetY;
            e.Element = new ImageElement(resultrect, image);

            var a = new GetElement();
            a.Threshold = 0.9;
            a.Image = Interfaces.Image.Util.Bitmap2Base64(image);
            a.Processname = Processname;
            e.SupportInput = false;
            e.a = new GetElementResult(a);

            return true;
        }
        public void Start()
        {
        }
        public void Stop()
        {
        }
    }
    public class GetElementResult : IBodyActivity
    {
        public GetElementResult(GetElement activity)
        {
            Activity = activity;
        }
        public System.Activities.Activity Activity { get; set; }
        public void AddActivity(System.Activities.Activity a, string Name)
        {
            var aa = new System.Activities.ActivityAction<ImageElement>();
            var da = new System.Activities.DelegateInArgument<ImageElement>();
            da.Name = Name;
            aa.Handler = a;
            ((GetElement)Activity).Body = aa;
            aa.Argument = da;
        }
        public void AddInput(string value, IElement element)
        {
            AddActivity(new System.Activities.Statements.Assign<string>
            {
                To = new Microsoft.VisualBasic.Activities.VisualBasicReference<string>("item.value"),
                Value = value
            }, "item");
            element.Value = value;
        }
    }

}
