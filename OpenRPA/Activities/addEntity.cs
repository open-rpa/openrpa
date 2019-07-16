//using System;
//using System.Activities;
//using System.Activities.Validation;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using FlaUI.Core.AutomationElements.Infrastructure;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//namespace OpenRPA.Activities
//{
//    [System.ComponentModel.Designer(typeof(addEntityDesigner), typeof(System.ComponentModel.Design.IDesigner))]
//    [System.Drawing.ToolboxBitmap(typeof(addEntity), "Resources.toolbox.entity.png")]
//    //[System.Drawing.ToolboxBitmap(typeof(CopyClipboard), "Resources.toolbox.breakpoint.png")]
//    // [designer.ToolboxTooltip(Text = "Add a single object into the Zeniverse datastore")]
//    public class addEntity : CodeActivity
//    {
//        static addEntity()
//        {
//            var builder = new System.Activities.Presentation.Metadata.AttributeTableBuilder();
//            //builder.AddCustomAttributes(typeof(addEntity), "Collection",
//            //    new System.ComponentModel.EditorAttribute(typeof(SelectCollectionEditor),
//            //    typeof(System.Activities.Presentation.PropertyEditing.PropertyValueEditor)));
//            //builder.AddCustomAttributes(typeof(addEntity), "UpdateIfExists",
//            //    new System.ComponentModel.EditorAttribute(typeof(BooleanEditor),
//            //    typeof(System.Activities.Presentation.PropertyEditing.PropertyValueEditor)));
            
//            System.Activities.Presentation.Metadata.MetadataStore.AddAttributeTable(builder.CreateTable());
//        }
//        public addEntity()
//        {
//            IgnoreErrors = false;
//            UpdateIfExists = false;
//            Collection = "entities";
//        }
//        [RequiredArgument]
//        public InArgument<bool> IgnoreErrors { get; set; }
//        // https://weblog.west-wind.com/posts/2012/Aug/30/Using-JSONNET-for-dynamic-JSON-parsing
//        [RequiredArgument]
//        public InArgument<bool> UpdateIfExists { get; set; }

//        public InArgument<string> Type { get; set; }
//        [RequiredArgument]
//        public InArgument<string> Collection { get; set; }
//        [RequiredArgument]
//        public InArgument<Object> Item { get; set; }
        
//        public OutArgument<JObject> Result { get; set; }
//        public InArgument<string> EncryptFields { get; set; }

//        protected override void Execute(CodeActivityContext context)
//        {
//            var ignoreErrors = false;
//            if (IgnoreErrors != null) ignoreErrors = IgnoreErrors.Get(context);
//            var encrypt = EncryptFields.Get(context);
//            if (encrypt == null) encrypt = "";
//            var collection = Collection.Get(context);
//            var type = Type.Get(context);
//            JObject result = null;
//            var o = Item.Get(context);
//            if(o.GetType() != typeof(JObject))
//            {

//                var t = Task.Factory.StartNew(() =>
//                {
//                    result = JObject.FromObject(o);
//                });
//                t.Wait();

//            }
//            else
//            {
//                result = (JObject)o;
//            }

//            if(!string.IsNullOrEmpty(encrypt))
//            {
//                result["_encrypt"] = encrypt;
//            }
//            var name = result.GetValue("name", StringComparison.OrdinalIgnoreCase)?.Value<string>();
//            result["name"] = name;
//            if(!string.IsNullOrEmpty(type))
//            {
//                result["_type"] = type;
//            }
//            var id = result.GetValue("_id");
//            if (id != null)
//            {
//                var _id = id.ToString();
//                //var arr = _rpaExtension.socket.GET<JObject>("_id eq '" + _id + "'", 1, 0, null, false, collection);
//                //var _res = arr.FirstOrDefault();
//                //if(_res != null && !UpdateIfExists.Get(context))
//                //{
//                //    if(!ignoreErrors)
//                //    {
//                //        throw new AllreadyExistsException("Object allready exists");
//                //    }
                    
//                //}
//                //result = _rpaExtension.socket.PUT(result, collection);
//            }
//            else
//            {
//                //result = _rpaExtension.socket.POST(result, collection);
//            }
//            context.SetValue(Result, result);
//            System.Windows.Forms.Application.DoEvents();
//        }
//    }

//}