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
    [System.ComponentModel.Designer(typeof(GetTextDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetText), "Resources.toolbox.getimage.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_gettext_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_gettext", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_gettext_helpurl", typeof(Resources.strings))]
    public class GetText : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetText()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
        }
        public InArgument<string> WordLimit { get; set; }
        [RequiredArgument]
        public InArgument<IElement> Element { get; set; }
        [RequiredArgument]
        public InArgument<bool> CaseSensitive { get; set; } = false;
        public OutArgument<ImageElement[]> Result { get; set; }
        [System.ComponentModel.Browsable(false)]
        public ActivityAction<ImageElement> Body { get; set; }
        private Variable<ImageElement[]> elements = new Variable<ImageElement[]>("elements");
        private Variable<IEnumerator<ImageElement>> _elements = new Variable<IEnumerator<ImageElement>>("_elements");
        public static ImageElement[] Execute(IElement ele, System.Activities.Presentation.Model.ModelItem model)
        {
            var wordlimit = model.GetValue<string>("WordLimit");
            var casesensitive = model.GetValue<bool>("CaseSensitive");
            var lang = Config.local.ocrlanguage;

            string basepath = Interfaces.Extensions.DataDirectory;
            string path = System.IO.Path.Combine(basepath, "tessdata");
            ocr.TesseractDownloadLangFile(path, Config.local.ocrlanguage);
            ocr.TesseractDownloadLangFile(path, "osd");

            ImageElement[] result;
            var _ocr = new Emgu.CV.OCR.Tesseract(path, lang.ToString(), Emgu.CV.OCR.OcrEngineMode.TesseractLstmCombined);
            _ocr.Init(path, lang.ToString(), Emgu.CV.OCR.OcrEngineMode.TesseractLstmCombined);
            _ocr.PageSegMode = Emgu.CV.OCR.PageSegMode.SparseText;

            // OpenRPA.Interfaces.Image.Util.SaveImageStamped(ele.element, "OCR");
            Bitmap sourceimg = null;
            if (ele is ImageElement)
            {
                sourceimg = ((ImageElement)ele).element;
            }
            else
            {
                sourceimg = Interfaces.Image.Util.Screenshot(ele.Rectangle.X, ele.Rectangle.Y, ele.Rectangle.Width, ele.Rectangle.Height);
            }
            using (var img = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(sourceimg))
            {
                result = ocr.OcrImage2(_ocr, img.Mat, wordlimit, casesensitive);
            }
            Log.Debug("adding element cords to results: " + ele.Rectangle.ToString());
            foreach (var R in result)
            {
                var rect = new System.Drawing.Rectangle(R.Rectangle.X + ele.Rectangle.X, R.Rectangle.Y + ele.Rectangle.Y, R.Rectangle.Width, R.Rectangle.Height);
                R.Rectangle = rect;
                Log.Debug("Found: '" + R.Text + "' at " + R.Rectangle.ToString());
            }
            return result;
        }
        protected override void StartLoop(NativeActivityContext context)
        {
            // var match = Element.Get(context);
            var wordlimit = WordLimit.Get(context);
            var lang = Config.local.ocrlanguage;
            var casesensitive = CaseSensitive.Get(context);
            string basepath = Interfaces.Extensions.DataDirectory;
            string path = System.IO.Path.Combine(basepath, "tessdata");
            ocr.TesseractDownloadLangFile(path, Config.local.ocrlanguage);
            ocr.TesseractDownloadLangFile(path, "osd");
            var ele = Element.Get(context);
            // ele.element.Save(@"c:\temp\dump.png", System.Drawing.Imaging.ImageFormat.Png);

            // var result = ocr.GetTextcomponents(path, Config.local.ocrlanguage, ele.element);
            // var result = ocr.GetTextcomponents(path, Config.local.ocrlanguage, @"c:\temp\dump.png");

            ImageElement[] result;
            var _ocr = new Emgu.CV.OCR.Tesseract(path, lang.ToString(), Emgu.CV.OCR.OcrEngineMode.TesseractLstmCombined);
            _ocr.Init(path, lang.ToString(), Emgu.CV.OCR.OcrEngineMode.TesseractLstmCombined);
            _ocr.PageSegMode = Emgu.CV.OCR.PageSegMode.SparseText;

            // OpenRPA.Interfaces.Image.Util.SaveImageStamped(ele.element, "OCR");
            Bitmap sourceimg = null;
            if (ele is ImageElement)
            {
                sourceimg = ((ImageElement)ele).element;
            }
            else
            {
                sourceimg = Interfaces.Image.Util.Screenshot(ele.Rectangle.X, ele.Rectangle.Y, ele.Rectangle.Width, ele.Rectangle.Height);
            }
            using (var img = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(sourceimg))
            {
                result = ocr.OcrImage2(_ocr, img.Mat, wordlimit, casesensitive);
            }
            Log.Debug("adding element cords to results: " + ele.Rectangle.ToString());
            foreach (var R in result)
            {
                var rect = new System.Drawing.Rectangle(R.Rectangle.X + ele.Rectangle.X, R.Rectangle.Y + ele.Rectangle.Y, R.Rectangle.Width, R.Rectangle.Height);
                R.Rectangle = rect;
                Log.Debug("Found: '" + R.Text + "' at " + R.Rectangle.ToString());
            }
            context.SetValue(Result, result);

            IEnumerator<ImageElement> _enum = result.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                IncIndex(context);
                SetTotal(context, result.Length);
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<ImageElement> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more && !breakRequested)
            {
                IncIndex(context);
                context.ScheduleAction<ImageElement>(Body, _enum.Current, OnBodyComplete);
            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Element", Element);
            Interfaces.Extensions.AddCacheArgument(metadata, "Result", Result);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetText();
            fef.Variables.Add(new Variable<int>("Index", 0));
            fef.Variables.Add(new Variable<int>("Total", 0));
            var aa = new ActivityAction<ImageElement>();
            var da = new DelegateInArgument<ImageElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}