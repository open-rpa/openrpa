using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.PowerPoint;
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(RunSlideShowDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.runslideshow.png")]
    [LocalizedToolboxTooltip("activity_runslideshow_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_runslideshow", typeof(Resources.strings))]
    public class RunSlideShow : CodeActivity
    {
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Filename { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<int> StartingSlide { get; set; }
        public InArgument<int> EndingSlide { get; set; }
        public InArgument<TimeSpan> AdvanceTime { get; set; }
        public InArgument<bool> KioskMode { get; set; }
        public InArgument<bool> CloseAtEnd { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var filename = Filename.Get(context);
            var startingslide = StartingSlide.Get(context);
            var kioskmode = KioskMode.Get(context);
            var endingslide = EndingSlide.Get(context);
            var advancetime = AdvanceTime.Get(context);
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
            document.Close();
            document = activeObject.Presentations.Open(filename);

            if (startingslide < 1) startingslide = 1;
            if (endingslide < 1 || endingslide > document.Slides.Count) endingslide = document.Slides.Count;
            if(kioskmode) document.SlideShowSettings.ShowType = PpSlideShowType.ppShowTypeKiosk;
            document.SlideShowSettings.ShowType = PpSlideShowType.ppShowTypeSpeaker;
            document.SlideShowSettings.RangeType =  PpSlideShowRangeType.ppShowSlideRange;
            document.SlideShowSettings.AdvanceMode = PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
            document.Slides.Range().SlideShowTransition.AdvanceTime = 0;
            document.Slides.Range().SlideShowTransition.AdvanceOnTime = Microsoft.Office.Core.MsoTriState.msoFalse;
            document.Slides.Range().SlideShowTransition.AdvanceOnClick = Microsoft.Office.Core.MsoTriState.msoTrue;
            //activeObject.ActivePresentation.Application.ActivePresentation.Slides[startingslide].Select();

            // StartingSlide does not seem to work the first time we open a powerpoint, so lets manually move forward
            document.SlideShowSettings.StartingSlide = startingslide;
            //activeObject.ActivePresentation.Application.ActiveWindow.Panes[2].Activate();


            SlideShowWindow obj = document.SlideShowSettings.Run();
            SlideShowView objSlideShow = document.SlideShowWindow.View;
            try
            {
                //while (objSlideShow.CurrentShowPosition < startingslide)
                //{
                //    objSlideShow.Next();
                //}
                while (objSlideShow.CurrentShowPosition < endingslide || objSlideShow.State != PpSlideShowState.ppSlideShowDone)
                {
                    if (advancetime != TimeSpan.Zero)
                    {
                        System.Threading.Thread.Sleep(advancetime);
                        objSlideShow.Next();
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            catch (COMException)
            {
            }
            catch (Exception)
            {
                throw;
            }
            try
            {
                if(objSlideShow!=null && advancetime != TimeSpan.Zero) objSlideShow.Exit();
            }
            catch (Exception)
            {
            }
            if(CloseAtEnd.Get(context))
            {
                try
                {
                    document.Close();
                    if(activeObject.Presentations.Count == 0)
                    {
                        activeObject.Quit();
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
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
