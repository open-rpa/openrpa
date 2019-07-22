//using OpenRPA.Interfaces;
//using System;
//using System.Activities;
//using System.Activities.Presentation.PropertyEditing;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OpenRPA.Image
//{
//    [System.ComponentModel.Designer(typeof(GetTextDesigner), typeof(System.ComponentModel.Design.IDesigner))]
//    [System.Drawing.ToolboxBitmap(typeof(GetText), "Resources.toolbox.getimage.png")]
//    [System.Windows.Markup.ContentProperty("Body")]
//    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
//    public class GetText : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
//    {
//        // I want this !!!!
//        // https://stackoverflow.com/questions/50669794/alternative-to-taking-rapid-screenshots-of-a-window
//        public GetText()
//        {
//            Element = new InArgument<ImageElement>()
//            {
//                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<ImageElement>("item")
//            };
//        }
//        [RequiredArgument]
//        public InArgument<ImageElement> Element { get; set; }
//        public OutArgument<Character[]> Result { get; set; }
//        [System.ComponentModel.Browsable(false)]
//        public ActivityAction<Character[]> Body { get; set; }
//        private Variable<Character[]> elements = new Variable<Character[]>("elements");
//        protected override void Execute(NativeActivityContext context)
//        {
//            var match = Element.Get(context);

//            var lang = Config.local.ocrlanguage;
//            string basepath = System.IO.Directory.GetCurrentDirectory();
//            string path = System.IO.Path.Combine(basepath, "tessdata");
//            ocr.TesseractDownloadLangFile(path, Config.local.ocrlanguage);
//            ocr.TesseractDownloadLangFile(path, "osd");
//            var _ocr = new Emgu.CV.OCR.Tesseract();
//            _ocr.Init(path, lang.ToString(), Emgu.CV.OCR.OcrEngineMode.TesseractLstmCombined);
//            // _ocr.SetVariable("tessedit_char_whitelist", "abcd0123456789.");
//            // _ocr.PageSegMode = Emgu.CV.OCR.PageSegMode.;

//            Emgu.CV.OCR.Tesseract.Character[] tempresult = null;
//            var result = new List<Character>();
//            OpenRPA.Interfaces.Image.Util.SaveImageStamped(match.element, "OCRGetText");
//            var stringres = string.Empty;
//            using (var img = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(match.element))
//            {
//                tempresult = ocr.OcrImageCharacters(_ocr, img.Mat);
//                tempresult.ForEach(x => result.Add(new Character() { Text = x.Text, Cost = x.Cost, Region = x.Region }));
//                tempresult.ForEach(x => stringres = stringres + x.Text); 
//            }
//            Console.WriteLine(stringres);
//            context.SetValue(Result, result.ToArray());

//            context.ScheduleAction(Body, result.ToArray(), OnBodyComplete);

//        }
//        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
//        {
//        }
//        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
//        {
//            Execute(context);
//        }
//        protected override void CacheMetadata(NativeActivityMetadata metadata)
//        {
//            metadata.AddDelegate(Body);

//            Interfaces.Extensions.AddCacheArgument(metadata, "Element", Element);

//            Interfaces.Extensions.AddCacheArgument(metadata, "Result", Result);

//            metadata.AddImplementationVariable(elements);
//            base.CacheMetadata(metadata);
//        }
//        public Activity Create(System.Windows.DependencyObject target)
//        {
//            var fef = new GetText();
//            var aa = new ActivityAction<Character[]>();
//            var da = new DelegateInArgument<Character[]>();
//            da.Name = "item";
//            fef.Body = aa;
//            aa.Argument = da;
//            return fef;
//        }
//    }
//    public class Character
//    {
//        public string Text { get; set; }
//        public float Cost { get; set; }
//        public Rectangle Region { get; set; }
//        public Character()
//        {

//        }
//        public override string ToString()
//        {
//            return Text + " " + Region.ToString() + " (" + Cost.ToString() + ")";
//        }
//    }
//}