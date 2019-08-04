//using System;
//using System.Activities;
//using OpenRPA.Interfaces;
//using System.Activities.Presentation.PropertyEditing;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Data;

//namespace OpenRPA.Forms.Activities
//{
//    [System.ComponentModel.Designer(typeof(AlertDesigner), typeof(System.ComponentModel.Design.IDesigner))]
//    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.prompt.png")]
//    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
//    public class Alert : CodeActivity
//    {
//        [RequiredArgument]
//        public InArgument<string> Message { get; set; }

//        protected override void Execute(CodeActivityContext context)
//        {
//            var message = Message.Get(context);
//            GenericTools.RunUI(() =>
//            {
//                GenericTools.restore();
//                //await Forge.Forms.Show.Dialog().For(new Alert("Hello world!"));
//                // Forge.Forms.Show.Dialog().For(new Forge.Forms.Alert(message)).Wait();
//                var options = new Forge.Forms.WindowOptions();
//                options.CanResize = true; options.TopMost = true; options.ShowCloseButton = true;
//                options.BringToFront = true;
//                Forge.Forms.Show.Window(options).For(new Forge.Forms.Alert(message)).Wait();
//                GenericTools.minimize();
//            });
//        }
//    }
//}