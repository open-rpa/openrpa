using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAPBridge
{
    partial class SAPEventElement
    {
        public SAPEventElement(SAPFEWSELib.GuiComponent Element, string SystemName, string Parent, bool all)
        {
            Id = Element.Id;
            Name = Element.Name;
            this.SystemName = SystemName;
            ContainerType = Element.ContainerType;
            type = Element.Type;
            this.Parent = Parent;
            LoadProperties(Element, all);
        }
        private static string[] limitedProperties = {"Changeable", "Modified", "Text", "ScreenTop", "ScreenLeft", "Height", "Width", "Top", "Left",};
        public void LoadProperties(SAPFEWSELib.GuiComponent Element, bool all)
        {
            var _type = GetDetailType(Element);
            var t = GetSAPTypeInfo(_type);
            var props = new List<SAPElementProperty>();
            foreach (var _p in t.GetProperties().Where(x => x.IsSpecialName == false))
            {
                if (!all && !limitedProperties.Contains(_p.Name)) continue;
                SAPElementProperty prop = new SAPElementProperty();
                prop.Name = _p.Name;
                prop.IsReadOnly = !_p.CanWrite;
                
                try
                {
                    prop.Value = _p.GetValue(Element).ToString();
                    props.Add(prop);
                    //Program.log(_p.Name);
                }
                catch
                {
                    if(!all) Program.log("*" + _p.Name);
                    prop.Value = "";
                }
            }
            Properties = props.ToArray();
        }
        public static string GetDetailType(SAPFEWSELib.GuiComponent comp)
        {
            if (comp.Type == "GuiSplitterShell")
            {
                return "GuiSplit";
            }
            else if (comp is SAPFEWSELib.GuiShell)
            {
                string type = "Gui" + (comp as SAPFEWSELib.GuiShell).SubType;
                if (type == "GuiTextEdit")
                    type = "GuiTextedit";
                if (type == "GuiToolbar")
                    type = "GuiToolbarControl";
                return type;
            }
            else
            {
                return comp.Type;
            }
        }
        public static Type GetSAPTypeInfo(string typeName)
        {
            if (Program.SAPGuiApiAssembly == null)
                throw new ArgumentNullException("No SAP Library found, please mark sure you load the lib named Interop.SAPFEWSELib.dll");
            Type t = Program.SAPGuiApiAssembly.GetType("SAPFEWSELib." + typeName).GetInterfaces()[0];
            return t;
        }
    }
}
