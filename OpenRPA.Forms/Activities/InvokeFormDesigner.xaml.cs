using Forge.Forms.FormBuilding;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
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
            if(form != f.XmlString)
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
    }
}