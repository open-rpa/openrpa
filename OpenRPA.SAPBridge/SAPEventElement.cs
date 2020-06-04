using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAPBridge
{
    public partial class SAPEventElement
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
        private static string[] limitedProperties = { "Changeable", "Modified", "Text", "ScreenTop", "ScreenLeft", "Height", "Width", "Top", "Left", };
        public void LoadProperties(SAPFEWSELib.GuiComponent Element, bool all)
        {
            int X = 0; int Y = 0; int Width = 0; int Height = 0;
            var _type = GetDetailType(Element);
            var t = GetSAPTypeInfo(_type);
            var props = new List<SAPElementProperty>();
            if(t!=null)
            {
                foreach (var _p in t.GetProperties().Where(x => x.IsSpecialName == false))
                {
                    if (!all && !limitedProperties.Contains(_p.Name)) continue;
                    SAPElementProperty prop = new SAPElementProperty();
                    prop.Name = _p.Name;
                    prop.IsReadOnly = !_p.CanWrite;
                    try
                    {
                        if (type == "GuiButton" && _p.Name == "Modified") continue;
                        if (type == "GuiTitlebar" && _p.Name == "Changeable") continue;
                        prop.Value = _p.GetValue(Element).ToString();
                        props.Add(prop);
                        if (_p.Name == "ScreenLeft") X = int.Parse(prop.Value);
                        if (_p.Name == "ScreenTop") Y = int.Parse(prop.Value);
                        if (_p.Name == "Width") Width = int.Parse(prop.Value);
                        if (_p.Name == "Height") Height = int.Parse(prop.Value);
                        //Program.log(_p.Name);
                    }
                    catch
                    {
                        // if (!all) Program.log("*" + _p.Name);
                        prop.Value = "";
                    }
                }
            }
            if (X > 0 && Y > 0) _Rectangle = new Rectangle(X, Y, Width, Height);
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
            var tt = Program.SAPGuiApiAssembly.GetType("SAPFEWSELib." + typeName);
            if (tt == null) return null;
            Type t = tt.GetInterfaces()[0];
            return t;
        }

        private Rectangle _Rectangle;
        [JsonIgnore]
        public Rectangle Rectangle
        {
            get {
                return _Rectangle;
            }
        }
    }
}
