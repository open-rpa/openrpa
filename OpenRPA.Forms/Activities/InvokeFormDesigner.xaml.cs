using Forge.Forms.FormBuilding;
using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace OpenRPA.Forms.Activities
{
    public partial class InvokeFormDesigner
    {
        public InvokeFormDesigner()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string form = ModelItem.GetValue<string>("Form");
            var f = new FormDesigner(form);
            f.Owner = GenericTools.MainWindow;
            f.ShowDialog();
            if (form != f.XmlString)
            {
                ModelItem.Properties["Form"].SetValue(new InArgument<string>() { Expression = new Literal<string>(f.XmlString) });
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                string form = ModelItem.GetValue<string>("Form");
                var definition = Forge.Forms.FormBuilding.FormBuilder.Default.GetDefinition(form, freeze: false);
                // var definition = FormBuilder.Default.GetDefinition(xmlString, freeze: false);

                // var firstNameElement = (DataFormField)definition.GetElements().FirstOrDefault(e => e is DataFormField d && d.Key == "FirstName");
                var designer = GenericTools.Designer;
                foreach (DataFormField f in definition.GetElements().Where(x => x is DataFormField))
                {
                    // Type t = Type.GetType(p.type);
                    Type t = f.PropertyType;
                    Log.Information("Checking for variable " + f.Key + " of type " + f.PropertyType);
                    designer.GetVariable(f.Key, t);

                }
                // var firstNameElement = (DataFormField)definition.GetElements().FirstOrDefault(e => e is DataFormField d && d.Key == "FirstName");
                //if (firstNameElement != null)
                //{
                //    firstNameElement.DefaultValue = new LiteralValue("FirstName default value");
                //}
                //definition.FreezeAll();
                //CompiledDefinition = definition;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ModelItemDictionary dictionary = base.ModelItem.Properties["Arguments"].Dictionary;
            try
            {
                string form = ModelItem.GetValue<string>("Form");
                var definition = Forge.Forms.FormBuilding.FormBuilder.Default.GetDefinition(form, freeze: false);
                foreach (DataFormField f in definition.GetElements().Where(x => x is DataFormField))
                {
                    bool exists = false;
                    foreach (var key in dictionary.Keys)
                    {
                        if (key.ToString() == f.Key) exists = true;
                        if (key.GetValue<string>("AnnotationText") == f.Key) exists = true;
                        if (key.GetValue<string>("Name") == f.Key) exists = true;
                    }
                    if (!exists)
                    {

                        Type t = f.PropertyType;
                        Type atype = typeof(VisualBasicValue<>);
                        Type constructed = atype.MakeGenericType(t);
                        object o = Activator.CreateInstance(constructed, f.Key);

                        Argument a = null;
                        a = Argument.Create(t, ArgumentDirection.InOut);
                        dictionary.Add(f.Key, a);
                    }
                }
            }
            catch (Exception)
            {
            }
            var options = new System.Activities.Presentation.DynamicArgumentDesignerOptions() { Title = "Map Arguments" };
            using (ModelEditingScope modelEditingScope = dictionary.BeginEdit())
            {
                if (System.Activities.Presentation.DynamicArgumentDialog.ShowDialog(base.ModelItem, dictionary, base.Context, base.ModelItem.View, options))
                {
                    modelEditingScope.Complete();
                }
                else
                {
                    modelEditingScope.Revert();
                }
            }



        }
    }
}