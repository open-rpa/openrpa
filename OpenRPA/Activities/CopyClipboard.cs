using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    [Designer(typeof(CopyClipboardDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.copyclipboard.png")]
    [LocalizedToolboxTooltip("activity_copyclipboard_tooltip", typeof(Resources.strings))]
    public class CopyClipboard : CodeActivity
    {
        [LocalizedDisplayName("activity_sendctrlc", typeof(Resources.strings)), LocalizedDescription("activity_sendctrlc_help", typeof(Resources.strings))]
        public InArgument<bool> SendCtrlC { get; set; } = false;
        [LocalizedDisplayName("activity_stringresult", typeof(Resources.strings)), LocalizedDescription("activity_stringresult_help", typeof(Resources.strings))]
        public OutArgument<string> StringResult { get; set; }
        [LocalizedDisplayName("activity_imageresult", typeof(Resources.strings)), LocalizedDescription("activity_imageresult_help", typeof(Resources.strings))]
        public OutArgument<System.Windows.Media.Imaging.BitmapSource> ImageResult { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string text = "";
            System.Windows.Media.Imaging.BitmapSource image = null;
            int counter = 0;
            while (string.IsNullOrEmpty(text) && image == null)
            {
                counter++;
                try
                {
                    if(SendCtrlC.Get(context))
                    {
                        var keys = FlaUI.Core.Input.Keyboard.Pressing(FlaUI.Core.WindowsAPI.VirtualKeyShort.LCONTROL, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_C);
                        keys.Dispose();
                    }
                    System.Windows.IDataObject idat = null;
                    Exception threadEx = null;
                    System.Threading.Thread staThread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            if (System.Windows.Clipboard.ContainsText())
                            {
                                idat = System.Windows.Clipboard.GetDataObject();
                                text = (string)idat.GetData(typeof(string));
                            }
                            if (System.Windows.Clipboard.ContainsImage())
                            {
                                idat = System.Windows.Clipboard.GetDataObject();
                                image = System.Windows.Clipboard.GetImage();
                                // var tmp = System.Windows.Clipboard.GetImage();
                                // image = new ImageElement(tmp);
                                //image = (System.Drawing.Image)idat.GetData(typeof(System.Drawing.Image));
                            }
                        }

                        catch (Exception ex)
                        {
                            threadEx = ex;
                        }
                    });
                    staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                    staThread.Start();
                    staThread.Join();
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.Message);
                    System.Threading.Thread.Sleep(250);
                }
                if (counter == 3) break;
            }
            context.SetValue(StringResult, text);
            context.SetValue(ImageResult, image);
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public new string DisplayName
        {
            get
            {
                return base.DisplayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}