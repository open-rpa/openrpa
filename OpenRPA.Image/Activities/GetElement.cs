using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.foreachimage.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetElement : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetElement()
        {
            CompareGray = true;
            Threshold = 0.8;
            MaxResults = 10;
            MinResults = 1;
        }
        public InArgument<string> Processname { get; set; }
        public InArgument<bool> CompareGray { get; set; }
        public InArgument<double> Threshold { get; set; }
        [System.ComponentModel.Browsable(false)]
        public ActivityAction<ImageElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<int> MinResults { get; set; }
        public InArgument<ImageElement> From { get; set; }
        public OutArgument<ImageElement[]> Elements { get; set; }
        [Browsable(false)]
        public String Image { get; set; }
        private Variable<IEnumerator<ImageElement>> _elements = new Variable<IEnumerator<ImageElement>>("_elements");
        public Activity LoopAction { get; set; }
        private List<ImageElement> getBatch(long maxresults, Double Threshold, string Processname, TimeSpan Timeout, bool CompareGray, Rectangle limit)
        {
            var result = new List<ImageElement>();
            Bitmap b = null;
            MemoryStream stream = null;
            if (System.Text.RegularExpressions.Regex.Match(Image, "[a-f0-9]{24}").Success)
            {
                // b = image.util.loadWorkflowImage(Image);
            }
            else
            {
                stream = new MemoryStream(Convert.FromBase64String(Image));
                b = new Bitmap(stream);
            }

            if (Timeout.TotalMilliseconds > 100)
            {
                var match = ImageEvent.waitFor(b, Threshold, Processname, Timeout, CompareGray, limit);
                if (match == Rectangle.Empty)
                {
                    if (stream != null) stream.Dispose();
                    b.Dispose();
                    b = null;
                    return result;
                }
            }
            //var template = new Image<Gray, Byte>(b);

            var bitmap = Interfaces.Image.Util.Screenshot();
            //var graphics = Graphics.FromImage(bitmap as Image);
            //graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            //var imageFrame = new Image<Gray, Byte>(bitmap);


            var _list = Matches.FindMatches(bitmap, b, Threshold, (int)maxresults, CompareGray);
            if (stream != null) stream.Dispose();
            stream = null;
            b.Dispose();
            b = null;

            var _listcleaned = new List<Rectangle>();
            foreach (var _match in _list.ToList())
            {
                bool addit = true;
                foreach (var submatch in _listcleaned)
                {
                    if (submatch.IntersectsWith(_match)) addit = false;
                }
                //if(addit && !string.IsNullOrEmpty(Processname))
                //{
                //    var element = automationutil.getElementAt(_match.X, _match.Y);
                //    var p = Process.GetProcessById(element.Properties.ProcessId.Value);
                //    if(p.ProcessName!= Processname) addit = false;
                //}
                if (addit) _listcleaned.Add(_match);
            }


            foreach (var r in _listcleaned)
            {
                result.Add(new ImageElement(r));
            }

            // Log.Debug("getBatch,count: " + result.Count());
            // _Results.AddRange(result);
            return result;
        }

        protected override void Execute(NativeActivityContext context)
        {
            var timeout = TimeSpan.FromSeconds(3);
            var maxresults = MaxResults.Get(context);
            var processname = Processname.Get(context);
            var comparegray = CompareGray.Get(context);
            var threshold = Threshold.Get(context);
            var minresults = MinResults.Get(context);
            var from = From.Get(context);
            if (maxresults < 1) maxresults = 1;
            if (threshold < 0.1) threshold = 0.1;
            if (threshold > 1) threshold = 1;

            ImageElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                elements = getBatch(maxresults, threshold, processname, timeout, comparegray, Rectangle.Empty).ToArray();
            } while (elements.Count() == 0 && sw.Elapsed < timeout);
            // Log.Debug(string.Format("OpenRPA.Image::GetElement::found {1} elements in {0:mm\\:ss\\.fff}", sw.Elapsed, elements.Count()));
            context.SetValue(Elements, elements);
            IEnumerator<ImageElement> _enum = elements.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
            else if(elements.Count() < minresults)
            {
                throw new Interfaces.ElementNotFoundException("Failed locating item");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<ImageElement> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<ImageElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Execute(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            var aa = new ActivityAction<ImageElement>();
            var da = new DelegateInArgument<ImageElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
    }
}