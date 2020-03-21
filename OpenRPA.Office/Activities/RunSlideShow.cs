using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.PowerPoint;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(RunSlideShowDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.readexcel.png")]
    public class RunSlideShow : CodeActivity
    {
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Filename { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<int> StartingSlide { get; set; }
        public InArgument<int> EndingSlide { get; set; }
        public InArgument<int> AdvanceTime { get; set; }
        public InArgument<bool> KioskMode { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var filename = Filename.Get(context);
            var startingslide = StartingSlide.Get(context);
            var kioskmode = KioskMode.Get(context);
            var endingslide = EndingSlide.Get(context);
            var advancetime = AdvanceTime.Get(context);
            if (startingslide < 1) startingslide = 1;
            filename = Environment.ExpandEnvironmentVariables(filename);
            Application activeObject = null;
            Presentation document = null;
            try
            {
                activeObject = (Application)Marshal.GetActiveObject("Powerpoint.Application");
                foreach (Presentation current in activeObject.Presentations)
                {
                    if (current.FullName == filename)
                    {
                        document = current;
                        break;
                    }
                }
            }
            catch
            {
                activeObject = null;
            }
            finally
            {
                if(activeObject==null) activeObject = (Application)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("91493441-5A91-11CF-8700-00AA0060263B")));
                activeObject.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
            }
            if(document==null) document = activeObject.Presentations.Open(filename);

            if (endingslide < 1 || endingslide > document.Slides.Count) endingslide = document.Slides.Count;
            if(kioskmode) document.SlideShowSettings.ShowType = PpSlideShowType.ppShowTypeKiosk;
            document.SlideShowSettings.ShowType = PpSlideShowType.ppShowTypeSpeaker;
            document.SlideShowSettings.StartingSlide = startingslide;
            document.SlideShowSettings.RangeType =  PpSlideShowRangeType.ppShowSlideRange;
            document.SlideShowSettings.AdvanceMode = PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
            document.Slides.Range().SlideShowTransition.AdvanceTime = 0;
            document.Slides.Range().SlideShowTransition.AdvanceOnTime = Microsoft.Office.Core.MsoTriState.msoFalse;
            document.Slides.Range().SlideShowTransition.AdvanceOnClick = Microsoft.Office.Core.MsoTriState.msoTrue;
            //activeObject.ActivePresentation.Application.ActiveWindow.Panes[2].Activate();
            //activeObject.ActivePresentation.Application.ActivePresentation.Slides[startingslide].Select();

            SlideShowWindow obj = document.SlideShowSettings.Run();
            SlideShowView objSlideShow = document.SlideShowWindow.View;
            try
            {
                while (objSlideShow.CurrentShowPosition < startingslide)
                {
                    objSlideShow.Next();
                }
                while (objSlideShow.CurrentShowPosition < endingslide || objSlideShow.State != PpSlideShowState.ppSlideShowDone)
                {
                    if (advancetime > 0)
                    {
                        System.Threading.Thread.Sleep(1000 * advancetime);
                        objSlideShow.Next();
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
            }
            catch (System.Exception ex)
            {
                throw;
            }


        }
    }
}
