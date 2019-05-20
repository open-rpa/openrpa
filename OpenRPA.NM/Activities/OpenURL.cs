//using OpenRPA.Interfaces;
//using System;
//using System.Activities;
//using System.Activities.Presentation.PropertyEditing;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OpenRPA.NM
//{
//    [System.ComponentModel.Designer(typeof(OpenURLDesigner), typeof(System.ComponentModel.Design.IDesigner))]
//    [System.Drawing.ToolboxBitmap(typeof(OpenURL), "Resources.toolbox.gethtmlelement.png")]
//    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
//    public class OpenURL : NativeActivity
//    {
//        [RequiredArgument]
//        public InArgument<string> Url { get; set; }


//        public OpenURL()
//        {
//        }

//        protected override void Execute(NativeActivityContext context)
//        {
//            var url = Url.Get(context);
//            var timeout = TimeSpan.FromSeconds(3);

//        }
//        protected override void CacheMetadata(NativeActivityMetadata metadata)
//        {
//            base.CacheMetadata(metadata);
//        }
//    }
//}