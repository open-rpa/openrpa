using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(CopyClipboardDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.copyclipboard.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class CopyClipboard : CodeActivity
    {
        public OutArgument<string> StringResult { get; set; }
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
    }
}