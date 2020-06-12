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
        public override string ToString()
        {
            return type + " " + Id;
        }
        public SAPEventElement(SAPFEWSELib.GuiComponent Element, string SystemName, string Parent, bool all)
        {
            if (String.IsNullOrEmpty(SystemName)) throw new ArgumentException("SystemName is mandatory");
            Id = Element.Id;
            Name = Element.Name;
            this.SystemName = SystemName;
            ContainerType = Element.ContainerType;
            type = Element.Type;
            this.Parent = Parent;
            LoadProperties(Element, all);
        }
        public SAPEventElement(SAPEventElement msg, SAPFEWSELib.GuiTree tree, string parentpath, string key, string SystemName)
        {
            if (String.IsNullOrEmpty(SystemName)) throw new ArgumentException("SystemName is mandatory");
            Id = tree.Id;
            // Path = parentpath + "/" + key;
            Path = key;
            if (string.IsNullOrEmpty(parentpath)) Path = key;
            this.SystemName = SystemName;
            ContainerType = false;
            type = "GuiTreeNode";
            Parent = tree.Id;
            Name = key;
            if (string.IsNullOrEmpty(Path))
            {
                var p = new List<SAPElementProperty>();
                p.Add(new SAPElementProperty("Left", tree.Left.ToString(), true));
                p.Add(new SAPElementProperty("Top", tree.Top.ToString(), true));
                p.Add(new SAPElementProperty("ScreenLeft", tree.ScreenLeft.ToString(), true));
                p.Add(new SAPElementProperty("ScreenTop", tree.ScreenTop.ToString(), true));
                p.Add(new SAPElementProperty("Width", tree.Width.ToString(), true));
                p.Add(new SAPElementProperty("Height", tree.Height.ToString(), true));

                _Rectangle = new Rectangle(tree.ScreenLeft, tree.ScreenTop, tree.Width, tree.Height);
                Properties = p.ToArray();
            }
            else
            {
                int Left = tree.GetNodeLeft(key);
                int Top = tree.GetNodeTop(key);
                int Width = tree.GetNodeWidth(key);
                int Height = tree.GetNodeHeight(key);
                var ScreenLeft = Left + msg.Rectangle.X;
                var ScreenTop = Top + msg.Rectangle.Y;
                var p = new List<SAPElementProperty>();
                p.Add(new SAPElementProperty("Left", Left.ToString(), true));
                p.Add(new SAPElementProperty("Top", Top.ToString(), true));
                p.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
                p.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
                p.Add(new SAPElementProperty("Width", Width.ToString(), true));
                p.Add(new SAPElementProperty("Height", Height.ToString(), true));
                _Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);
                Properties = p.ToArray();

            }


            // SAPFEWSELib.GuiCollection keys = tree.GetColumnNames() as SAPFEWSELib.GuiCollection;
            // SAPFEWSELib.GuiCollection keys = tree.GetColumnTitles() as SAPFEWSELib.GuiCollection;
            SAPFEWSELib.GuiCollection keys = tree.GetColumnHeaders() as SAPFEWSELib.GuiCollection;
            if (keys == null) keys = tree.GetColumnTitles() as SAPFEWSELib.GuiCollection;
            if (keys == null) keys = tree.GetColumnNames() as SAPFEWSELib.GuiCollection;
            var items = new List<SAPEventElement>();


            var temp = tree.GetNodeItemHeaders(key);
            var temp2 = temp as SAPFEWSELib.GuiCollection;
            if (temp2 != null)
            {
                keys = temp2;
            }

            //foreach (string _key in keys)
            if (keys != null)
                for (var i = 1; i <= keys.Count; i++)
                {
                    string _key = i.ToString();
                    string text = tree.GetItemText(key, _key);
                    var column = new SAPEventElement();
                    column.SystemName = SystemName;
                    column.Id = Id;
                    column.Path = Path;
                    column.type = "GuiTreeItem";
                    if (string.IsNullOrEmpty(column.Name)) column.Name = text;
                    if (string.IsNullOrEmpty(column.Name)) column.Name = _key;
                    if (i == 1 && !string.IsNullOrEmpty(text)) Name = text;

                    int Left = tree.GetItemLeft(key, _key);
                    int Top = tree.GetItemTop(key, _key);
                    int Width = tree.GetItemWidth(key, _key);
                    int Height = tree.GetItemHeight(key, _key);
                    var ScreenLeft = Left + msg.Rectangle.X;
                    var ScreenTop = Top + msg.Rectangle.Y;
                    column._Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);
                    var _p = new List<SAPElementProperty>();
                    _p.Add(new SAPElementProperty("Text", text, true));
                    _p.Add(new SAPElementProperty("Key", _key, true));
                    _p.Add(new SAPElementProperty("Left", Left.ToString(), true));
                    _p.Add(new SAPElementProperty("Top", Top.ToString(), true));
                    _p.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
                    _p.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
                    _p.Add(new SAPElementProperty("Width", Width.ToString(), true));
                    _p.Add(new SAPElementProperty("Height", Height.ToString(), true));
                    column.Properties = _p.ToArray();
                    items.Add(column);
                }
            Items = items.ToArray();

        }
        public SAPEventElement(SAPEventElement msg, SAPFEWSELib.GuiGridView grid, string parentpath, int Row, string SystemName)
        {
            if (String.IsNullOrEmpty(SystemName)) throw new ArgumentException("SystemName is mandatory");
            Id = grid.Id;
            // Path = parentpath + "/" + key;
            Path = Row.ToString();
            if (string.IsNullOrEmpty(parentpath)) Path = Row.ToString();
            this.SystemName = SystemName;
            ContainerType = false;
            type = "GuiGridView";
            Parent = grid.Id;
            Name = Row.ToString();


            var p = new List<SAPElementProperty>();
            p.Add(new SAPElementProperty("Left", grid.Left.ToString(), true));
            p.Add(new SAPElementProperty("Top", grid.Top.ToString(), true));
            p.Add(new SAPElementProperty("ScreenLeft", grid.ScreenLeft.ToString(), true));
            p.Add(new SAPElementProperty("ScreenTop", grid.ScreenTop.ToString(), true));
            p.Add(new SAPElementProperty("Width", grid.Width.ToString(), true));
            p.Add(new SAPElementProperty("Height", grid.Height.ToString(), true));

            _Rectangle = new Rectangle(grid.ScreenLeft, grid.ScreenTop, grid.Width, grid.Height);

            var keys = grid.ColumnOrder as SAPFEWSELib.GuiCollection;
            var items = new List<SAPEventElement>();
            foreach (string key in keys)
            {
                var column = new SAPEventElement();
                column.SystemName = SystemName;
                column.Name = key;
                column.type = "GuiGridCell";
                int Left = grid.GetCellLeft(Row, key);
                int Top = grid.GetCellTop(Row, key);
                int Width = grid.GetCellWidth(Row, key);
                int Height = grid.GetCellHeight(Row, key);
                var ScreenLeft = Left + msg.Rectangle.X;
                var ScreenTop = Top + msg.Rectangle.Y;

                string Value = grid.GetCellValue(Row, key);

                var properties = new List<SAPElementProperty>();
                properties.Add(new SAPElementProperty("Left", Left.ToString(), true));
                properties.Add(new SAPElementProperty("Top", Top.ToString(), true));
                properties.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
                properties.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
                properties.Add(new SAPElementProperty("Width", Width.ToString(), true));
                properties.Add(new SAPElementProperty("Height", Height.ToString(), true));
                properties.Add(new SAPElementProperty("Value", Value, false));
                column._Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);
                column.Properties = properties.ToArray();
                items.Add(column);
            }
            Items = items.ToArray();

        }
        private static string[] limitedProperties = { "Changeable", "Modified", "Text", "ScreenTop", "ScreenLeft", "Height", "Width", "Top", "Left", };
        public void LoadProperties(SAPFEWSELib.GuiComponent Element, bool all)
        {
            int X = 0; int Y = 0; int Width = 0; int Height = 0;
            var _type = GetDetailType(Element);
            var t = GetSAPTypeInfo(_type);
            var props = new List<SAPElementProperty>();
            if (t != null)
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
                        var value = _p.GetValue(Element);
                        if (value != null) prop.Value = value.ToString();
                        if (value == null) prop.Value = "";
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
            get
            {
                return _Rectangle;
            }
        }
    }
}
