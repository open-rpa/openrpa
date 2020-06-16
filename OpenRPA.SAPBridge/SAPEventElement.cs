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
        public void Load(bool VisibleOnly)
        {
            LoadChildren = true;
            var session = SAPHook.Instance.GetSession(SystemName);
            var comp = session.GetSAPComponentById<SAPFEWSELib.GuiComponent>(Id);
            Load(comp, SystemName, all, Path, Cell, flat, LoadChildren, MaxItem, VisibleOnly);
        }
        public void Load(SAPFEWSELib.GuiComponent comp, string SystemName, bool all, string Path, string Cell, bool flat, bool LoadChildren, int MaxItem, bool VisibleOnly)
        {
            this.Path = Path;
            this.Cell = Cell;
            this.Flat = flat;
            this.MaxItem = MaxItem;
            Id = comp.Id;
            this.SystemName = SystemName;
            if (comp.Parent != null) Parent = ((SAPFEWSELib.GuiComponent)comp.Parent).Id;
            ContainerType = comp.ContainerType;
            type = comp.Type;
            if (comp is SAPFEWSELib.GuiTree tree)
            {
                type = "GuiTree";
                SAPFEWSELib.GuiCollection keys = null;

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
                    if (Flat)
                    {
                        keys = tree.GetAllNodeKeys() as SAPFEWSELib.GuiCollection;
                    }
                    else
                    {
                        keys = tree.GetNodesCol() as SAPFEWSELib.GuiCollection;
                    }
                    if (keys != null && LoadChildren)
                    {
                        var _keys = new List<string>();
                        foreach (string key in keys) _keys.Add(key);
                        var children = new List<SAPEventElement>();
                        foreach (string key in keys)
                        {
                            var _msg = new SAPEventElement(comp, SystemName, all, key, null, Flat, true, MaxItem, VisibleOnly);
                            children.Add(_msg);
                            if (MaxItem > 0)
                            {
                                if (children.Count >= MaxItem) break;
                            }
                        }
                        Children = children.ToArray();
                    }

                }
                else
                {
                    ContainerType = false;
                    type = "GuiTreeNode";
                    int Left = 0;
                    int Top = 0;
                    int Width = 0;
                    int Height = 0;
                    int ScreenLeft = 0;
                    int ScreenTop = 0;
                    if (string.IsNullOrEmpty(Cell))
                    {
                        Height = tree.GetNodeHeight(Path);
                        if(Height > 0)
                        {
                            Left = tree.GetNodeLeft(Path);
                            Top = tree.GetNodeTop(Path);
                            Width = tree.GetNodeWidth(Path);
                            ScreenLeft = Left + tree.ScreenLeft;
                            ScreenTop = Top + tree.ScreenTop;
                        }
                    }
                    else
                    {
                        Height = tree.GetItemHeight(Path, Cell);
                        if(Height> 0)
                        {
                            Left = tree.GetItemLeft(Path, Cell);
                            Top = tree.GetItemTop(Path, Cell);
                            Width = tree.GetItemWidth(Path, Cell);
                            ScreenLeft = Left + tree.ScreenLeft;
                            ScreenTop = Top + tree.ScreenTop;
                        }
                    }
                    var p = new List<SAPElementProperty>();
                    var text = tree.GetNodeTextByKey(Path);

                    p.Add(new SAPElementProperty("Left", Left.ToString(), true));
                    p.Add(new SAPElementProperty("Top", Top.ToString(), true));
                    p.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
                    p.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
                    p.Add(new SAPElementProperty("Width", Width.ToString(), true));
                    p.Add(new SAPElementProperty("Height", Height.ToString(), true));
                    p.Add(new SAPElementProperty("Key", Path, true));
                    if (!string.IsNullOrEmpty(Cell)) p.Add(new SAPElementProperty("Cell", Cell, true));
                    p.Add(new SAPElementProperty("Text", text, true));
                    Properties = p.ToArray();
                    _Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);
                    if (string.IsNullOrEmpty(Cell))
                    {
                        keys = tree.GetColumnHeaders() as SAPFEWSELib.GuiCollection;
                        if (keys == null)
                        {
                            keys = tree.GetNodeItemHeaders(Path) as SAPFEWSELib.GuiCollection;
                        }
                        if (keys != null)
                        {
                            var children = new List<SAPEventElement>();
                            foreach (string key in keys)
                            {
                                var _msg = new SAPEventElement(comp, SystemName, all, Path, key, Flat, true, MaxItem, VisibleOnly);
                                children.Add(_msg);
                                if (MaxItem > 0)
                                {
                                    if (children.Count >= MaxItem) break;
                                }
                            }
                            Items = children.ToArray();
                        }

                    }
                    if (!flat && LoadChildren && string.IsNullOrEmpty(Cell))
                    {
                        keys = tree.GetSubNodesCol(Path) as SAPFEWSELib.GuiCollection;
                        if (keys != null && LoadChildren)
                        {
                            var children = new List<SAPEventElement>();
                            foreach (string key in keys)
                            {
                                var _msg = new SAPEventElement(comp, SystemName, all, key, null, Flat, false, MaxItem, VisibleOnly);
                                children.Add(_msg);
                                if (MaxItem > 0)
                                {
                                    if (children.Count >= MaxItem) break;
                                }
                            }
                            Children = children.ToArray();
                        }
                    }

                }
            }
            else if (comp is SAPFEWSELib.GuiTableControl table)
            {
                type = "GuiTable";
                throw new Exception("Unknown role type GuiTable");
            }
            else if (comp is SAPFEWSELib.GuiGridView grid)
            {
                type = "GuiGrid";
                if (string.IsNullOrEmpty(Path))
                {
                    var p = new List<SAPElementProperty>();
                    p.Add(new SAPElementProperty("Left", grid.Left.ToString(), true));
                    p.Add(new SAPElementProperty("Top", grid.Top.ToString(), true));
                    p.Add(new SAPElementProperty("ScreenLeft", grid.ScreenLeft.ToString(), true));
                    p.Add(new SAPElementProperty("ScreenTop", grid.ScreenTop.ToString(), true));
                    p.Add(new SAPElementProperty("Width", grid.Width.ToString(), true));
                    p.Add(new SAPElementProperty("Height", grid.Height.ToString(), true));
                    _Rectangle = new Rectangle(grid.ScreenLeft, grid.ScreenTop, grid.Width, grid.Height);
                    Properties = p.ToArray();
                }
                else if (string.IsNullOrEmpty(Cell))
                {
                    
                    ContainerType = false;
                    type = "GuiGridNode";
                    var p = new List<SAPElementProperty>();
                    int Left = 0;
                    int Top = 0;
                    int Width = 0;
                    int Height = 0;
                    int ScreenLeft = 0;
                    int ScreenTop = 0;
                    var index = int.Parse(Path);
                    Height = grid.GetCellHeight(index, Cell);
                    if (Height > 0)
                    {
                        //Width = grid.GetCellWidth(index, Cell);
                        Width = grid.Width;
                        Left = grid.GetCellLeft(index, Cell);
                        Top = grid.GetCellTop(index, Cell);
                        ScreenLeft = Left + grid.ScreenLeft;
                        ScreenTop = Top + grid.ScreenTop;
                    }
                    p.Add(new SAPElementProperty("Left", Left.ToString(), true));
                    p.Add(new SAPElementProperty("Top", Top.ToString(), true));
                    p.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
                    p.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
                    p.Add(new SAPElementProperty("Width", Width.ToString(), true));
                    p.Add(new SAPElementProperty("Height", Height.ToString(), true));
                    p.Add(new SAPElementProperty("Key", Path, true));
                    if (!string.IsNullOrEmpty(Cell)) p.Add(new SAPElementProperty("Cell", Cell, true));
                    //var text = tree.GetNodeTextByKey(Path);
                    //p.Add(new SAPElementProperty("Text", text, true));
                    Properties = p.ToArray();
                    _Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);

                }
                else
                {
                    ContainerType = false;
                    type = "GuiGridCell";

                    var properties = new List<SAPElementProperty>();
                    properties.Add(new SAPElementProperty("Key", Cell, true));

                    var index = int.Parse(Path);
                    properties.Add(new SAPElementProperty("Index", index.ToString(), true));
                    //var tooltip = grid.GetCellTooltip(index, Cell);
                    //properties.Add(new SAPElementProperty("Title", title, true));
                    string Value = null;
                    int Left = 0;
                    int Top = 0;
                    int Width = 0;
                    int Height = 0;
                    var ScreenLeft = 0;
                    var ScreenTop = 0;
                    try
                    {
                        Value = grid.GetCellValue(index, Cell);
                        if (string.IsNullOrEmpty(Value))
                        {
                            grid.SetCurrentCell(index, Cell);
                            //grid.PressButton(index, Cell);
                            //grid.SelectedRows = Path;
                            Value = grid.GetCellValue(index, Cell);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    properties.Add(new SAPElementProperty("Value", Value, false));
                    //string title = grid.GetDisplayedColumnTitle(Cell);
                    //properties.Add(new SAPElementProperty("ToolTip", tooltip, true));
                    try
                    {
                        Height = grid.GetCellHeight(index, Cell);
                        if(Height>0)
                        {
                            Left = grid.GetCellLeft(index, Cell);
                            Top = grid.GetCellTop(index, Cell);
                            Width = grid.GetCellWidth(index, Cell);
                            ScreenLeft = Left + grid.ScreenLeft;
                            ScreenTop = Top + grid.ScreenTop;
                        }
                    }
                    catch (Exception)
                    {
                    }
                    properties.Add(new SAPElementProperty("Left", Left.ToString(), true));
                    properties.Add(new SAPElementProperty("Top", Top.ToString(), true));
                    properties.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
                    properties.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
                    properties.Add(new SAPElementProperty("Width", Width.ToString(), true));
                    properties.Add(new SAPElementProperty("Height", Height.ToString(), true));

                    _Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);
                    Properties = properties.ToArray();
                }

                if (string.IsNullOrEmpty(Path))
                {
                    var children = new List<SAPEventElement>();
                    if (string.IsNullOrEmpty(Cell) && LoadChildren)
                    {

                        int from = 0;
                        int to  = grid.RowCount;

                        if (VisibleOnly)
                        {
                            from = grid.FirstVisibleRow;
                            to = grid.FirstVisibleRow + grid.VisibleRowCount;
                        }
                        for (var i = from; i < to; i++)
                        {
                            var _msg = new SAPEventElement(comp, SystemName, all, i.ToString(), null, Flat, true, MaxItem, VisibleOnly);
                            //var _msg = new SAPEventElement(comp, SystemName, Parent, false);
                            children.Add(_msg);
                            if (MaxItem > 0)
                            {
                                if (children.Count >= MaxItem) break;
                            }
                        }
                    }
                    Children = children.ToArray();
                }
                else if (LoadChildren)
                {
                    var children = new List<SAPEventElement>();
                    var keys = grid.ColumnOrder as SAPFEWSELib.GuiCollection;
                    foreach (string key in keys)
                    {
                        var _msg = new SAPEventElement(comp, SystemName, all, Path, key, Flat, false, MaxItem, VisibleOnly);
                        children.Add(_msg);
                        if (MaxItem > 0)
                        {
                            if (children.Count >= MaxItem) break;
                        }
                    }
                    Items = children.ToArray();
                }
            }
            else if (comp is SAPFEWSELib.GuiRadioButton radio && LoadChildren)
            {
                var p = new List<SAPElementProperty>();
                p.Add(new SAPElementProperty("Selected", radio.Selected.ToString(), false));
                
                int Left = 0;
                int Top = 0;
                int Width = 0;
                int Height = 0;
                int ScreenLeft = 0;
                int ScreenTop = 0;
                Height = radio.Height;
                if (Height > 0)
                {
                    Left = radio.Left;
                    Top = radio.Top;
                    Width = radio.Width;
                    ScreenLeft = radio.ScreenLeft;
                    ScreenTop = radio.ScreenTop;
                }
                p.Add(new SAPElementProperty("Left", Left.ToString(), true));
                p.Add(new SAPElementProperty("Top", Top.ToString(), true));
                p.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
                p.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
                p.Add(new SAPElementProperty("Width", Width.ToString(), true));
                p.Add(new SAPElementProperty("Height", Height.ToString(), true));
                Properties = p.ToArray();
                _Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);
                var children = new List<SAPEventElement>();
                var keys = radio.GroupMembers as SAPFEWSELib.GuiCollection;
                if(keys!=null)
                    for (var i = 0; i < keys.Count; i++)
                    {
                        var ele = keys.ElementAt(i) as SAPFEWSELib.GuiComboBoxEntry;
                        var _msg = new SAPEventElement(ele, SystemName, radio.Id, all);
                        children.Add(_msg);
                        if (MaxItem > 0)
                        {
                            if (children.Count >= MaxItem) break;
                        }
                    }
                Items = children.ToArray();
            }
            else if (comp is SAPFEWSELib.GuiComboBox combobox && LoadChildren)
            {
                var children = new List<SAPEventElement>();
                var keys = combobox.Entries as SAPFEWSELib.GuiCollection;
                var p = new List<SAPElementProperty>();
                p.Add(new SAPElementProperty("Value", combobox.Value, false));

                int Left = 0;
                int Top = 0;
                int Width = 0;
                int Height = 0;
                int ScreenLeft = 0;
                int ScreenTop = 0;
                Height = combobox.Height;
                if (Height > 0)
                {
                    Left = combobox.Left;
                    Top = combobox.Top;
                    Width = combobox.Width;
                    ScreenLeft = combobox.ScreenLeft;
                    ScreenTop = combobox.ScreenTop;
                }
                p.Add(new SAPElementProperty("Left", Left.ToString(), true));
                p.Add(new SAPElementProperty("Top", Top.ToString(), true));
                p.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
                p.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
                p.Add(new SAPElementProperty("Width", Width.ToString(), true));
                p.Add(new SAPElementProperty("Height", Height.ToString(), true));
                Properties = p.ToArray();
                _Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);
                for (var i=0; i < keys.Count; i++)
                {
                    var ele = keys.ElementAt(i) as SAPFEWSELib.GuiComboBoxEntry;
                    var _msg = new SAPEventElement(ele, SystemName, combobox.Id, all);
                    children.Add(_msg);
                    if (MaxItem > 0)
                    {
                        if (children.Count >= MaxItem) break;
                    }
                }
                Items = children.ToArray();
            }
            else if (comp.ContainerType)
            {
                if (comp is SAPFEWSELib.GuiVContainer vcon && LoadChildren)
                {
                    var children = new List<SAPEventElement>();
                    for (var i = 0; i < vcon.Children.Count; i++)
                    {
                        SAPFEWSELib.GuiComponent Element = vcon.Children.ElementAt(i);
                        var p = Element.Parent as SAPFEWSELib.GuiComponent;
                        var parent = (p != null) ? p.Id : null;
                        var _newchild = new SAPEventElement(Element, SystemName, parent, false);
                        children.Add(_newchild);
                        if (MaxItem > 0)
                        {
                            if (children.Count >= MaxItem) break;
                        }
                    }
                    Children = children.ToArray();
                }
                else if (comp is SAPFEWSELib.GuiSession session && LoadChildren)
                {
                    var children = new List<SAPEventElement>();
                    if(VisibleOnly)
                    {
                        SAPFEWSELib.GuiComponent Element = session.ActiveWindow as SAPFEWSELib.GuiComponent;
                        var p = Element.Parent as SAPFEWSELib.GuiComponent;
                        var parent = (p != null) ? p.Id : null;
                        children.Add(new SAPEventElement(Element, SystemName, parent, false));
                    }
                    else
                    {
                        for (var i = 0; i < session.Children.Count; i++)
                        {
                            SAPFEWSELib.GuiComponent Element = session.Children.ElementAt(i);
                            var p = Element.Parent as SAPFEWSELib.GuiComponent;
                            var parent = (p != null) ? p.Id : null;
                            children.Add(new SAPEventElement(Element, SystemName, parent, false));
                            if (MaxItem > 0)
                            {
                                if (children.Count >= MaxItem) break;
                            }
                        }
                    }
                    Children = children.ToArray();

                }
                else if (comp is SAPFEWSELib.GuiContainer con && LoadChildren)
                {
                    var children = new List<SAPEventElement>();
                    for (var i = 0; i < con.Children.Count; i++)
                    {
                        SAPFEWSELib.GuiComponent Element = con.Children.ElementAt(i);
                        var p = Element.Parent as SAPFEWSELib.GuiComponent;
                        var parent = (p != null) ? p.Id : null;
                        children.Add(new SAPEventElement(Element, SystemName, parent, false));
                        if (MaxItem > 0)
                        {
                            if (children.Count >= MaxItem) break;
                        }
                    }
                    Children = children.ToArray();
                }
                else if (comp is SAPFEWSELib.GuiStatusbar sbar && LoadChildren)
                {
                    var children = new List<SAPEventElement>();
                    type = "GuiStatusbar";
                    for (var i = 0; i < sbar.Children.Count; i++)
                    {
                        SAPFEWSELib.GuiComponent Element = sbar.Children.ElementAt(i);
                        var p = Element.Parent as SAPFEWSELib.GuiComponent;
                        var parent = (p != null) ? p.Id : null;
                        children.Add(new SAPEventElement(Element, SystemName, parent, false));
                        if (MaxItem > 0)
                        {
                            if (children.Count >= MaxItem) break;
                        }
                    }
                    Children = children.ToArray();
                }
                //else
                //{
                //    throw new Exception("Unknown container type " + comp.Type + "!");
                //}
            }
            else
            {
                LoadProperties(comp, all);
            }
            if(string.IsNullOrEmpty(Name))
            {
                if(Properties!=null)
                {
                    var name = Name;
                    var p = Properties.Where(x => x.Name == "Key").FirstOrDefault();
                    if (p != null) name = p.Value;
                    p = Properties.Where(x => x.Name == "Cell").FirstOrDefault();
                    if (p != null) name = p.Value;
                    p = Properties.Where(x => x.Name == "Text").FirstOrDefault();
                    if (p != null) name = p.Value;
                    Name = name;
                }
            }
        }
        private bool all { get; set; }
        private bool flat { get; set; }
        private bool LoadChildren { get; set; }
        public SAPEventElement(SAPFEWSELib.GuiComponent comp, string SystemName, bool all, string Path, string Cell, bool flat, bool LoadChildren, int MaxItem, bool VisibleOnly)
        {
            this.all = all;
            this.flat = flat;
            this.LoadChildren = LoadChildren;
            this.MaxItem = MaxItem;
            Load(comp, SystemName, all, Path, Cell, flat, LoadChildren, MaxItem, VisibleOnly);
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
        public SAPEventElement(SAPFEWSELib.GuiComboBoxEntry Element, string SystemName, string Parent, bool all)
        {
            if (String.IsNullOrEmpty(SystemName)) throw new ArgumentException("SystemName is mandatory");
            Id = Parent;
            Name = Element.Key;
            this.SystemName = SystemName;
            ContainerType = false;
            type = "GuiComboBoxEntry";
            this.Parent = Parent;
            var p = new List<SAPElementProperty>();
            p.Add(new SAPElementProperty("Key", Element.Key.ToString(), true));
            p.Add(new SAPElementProperty("Cell", Element.Pos.ToString(), true));
            p.Add(new SAPElementProperty("Value", Element.Value, true));
            Properties = p.ToArray();
        }
        //public SAPEventElement(SAPEventElement msg, SAPFEWSELib.GuiGridView grid, string parentpath, int Row, string SystemName)
        //{
        //    if (String.IsNullOrEmpty(SystemName)) throw new ArgumentException("SystemName is mandatory");
        //    Id = grid.Id;
        //    // Path = parentpath + "/" + key;
        //    Path = Row.ToString();
        //    if (string.IsNullOrEmpty(parentpath)) Path = Row.ToString();
        //    this.SystemName = SystemName;
        //    ContainerType = false;
        //    type = "GuiGridView";
        //    Parent = grid.Id;
        //    Name = Row.ToString();



        //    var keys = grid.ColumnOrder as SAPFEWSELib.GuiCollection;
        //    var items = new List<SAPEventElement>();
        //    foreach (string key in keys)
        //    {
        //        var column = new SAPEventElement();
        //        column.SystemName = SystemName;
        //        column.Name = key;
        //        column.type = "GuiGridCell";
        //        int Left = grid.GetCellLeft(Row, key);
        //        int Top = grid.GetCellTop(Row, key);
        //        int Width = grid.GetCellWidth(Row, key);
        //        int Height = grid.GetCellHeight(Row, key);
        //        var ScreenLeft = Left + msg.Rectangle.X;
        //        var ScreenTop = Top + msg.Rectangle.Y;

        //        var tooltip = grid.GetCellTooltip(Row, key);

        //        string Value = grid.GetCellValue(Row, key);

        //        var properties = new List<SAPElementProperty>();
        //        properties.Add(new SAPElementProperty("Left", Left.ToString(), true));
        //        properties.Add(new SAPElementProperty("Top", Top.ToString(), true));
        //        properties.Add(new SAPElementProperty("ScreenLeft", ScreenLeft.ToString(), true));
        //        properties.Add(new SAPElementProperty("ScreenTop", ScreenTop.ToString(), true));
        //        properties.Add(new SAPElementProperty("Width", Width.ToString(), true));
        //        properties.Add(new SAPElementProperty("Height", Height.ToString(), true));
        //        properties.Add(new SAPElementProperty("Key", key, true));
        //        properties.Add(new SAPElementProperty("Value", Value, false));
        //        properties.Add(new SAPElementProperty("ToolTip", tooltip, true));

        //        column._Rectangle = new Rectangle(ScreenLeft, ScreenTop, Width, Height);
        //        column.Properties = properties.ToArray();
        //        items.Add(column);
        //    }
        //    Items = items.ToArray();

        //}
        private static string[] limitedProperties = { "Changeable", "Modified", "Text", "ScreenTop", "ScreenLeft", "Height", "Width", "Top", "Left", };
        private static Dictionary<Type, System.Reflection.PropertyInfo[]> typeProperties = new Dictionary<Type, System.Reflection.PropertyInfo[]>();
        public void LoadProperties(SAPFEWSELib.GuiComponent Element, bool all)
        {
            int X = 0; int Y = 0; int Width = 0; int Height = 0;
            var _type = GetDetailType(Element);
            var t = GetSAPTypeInfo(_type);
            var props = new List<SAPElementProperty>();
            if (t != null)
            {
                System.Reflection.PropertyInfo[] tproperties = null;
                if (typeProperties.ContainsKey(t))
                {
                    tproperties = typeProperties[t];
                } else
                {
                    tproperties = t.GetProperties().Where(x => x.IsSpecialName == false).ToArray();
                    typeProperties.Add(t, tproperties);
                }
                foreach (var _p in tproperties)
                {
                    if (!all && !limitedProperties.Contains(_p.Name)) continue;
                    SAPElementProperty prop = new SAPElementProperty();
                    prop.Name = _p.Name;
                    prop.IsReadOnly = !_p.CanWrite;
                    try
                    {
                        if (_p.Name == "Children") continue;
                        if (_p.Name == "Items") continue;
                        if (_p.Name == "Properties") continue;
                        if (type == "GuiButton" && _p.Name == "Modified") continue;
                        if (type == "GuiTitlebar" && _p.Name == "Changeable") continue;
                        if(type == "GuiTitlebar" && _p.Name == "Modified") continue;
                        if (type == "GuiShell" && _p.Name == "AccTooltip") continue;
                        if (type == "GuiShell" && _p.Name == "AccLabelCollection") continue;
                        if (type == "GuiShell" && _p.Name == "HierarchyHeaderWidth") continue;
                        if (type == "GuiShell" && _p.Name == "IsSymbolFont") continue;
                        if (type == "GuiShell" && _p.Name == "ColumnOrder") continue;
                        if (type == "GuiStatusPane" && _p.Name == "Modified") continue;
                        if (type == "GuiShell" && _p.Name == "Parent") continue;
                        if (type == "GuiStatusPane" && _p.Name == "Modified") continue;
                        if (type == "GuiStatusPane" && _p.Name == "Modified") continue;
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
                        System.Diagnostics.Trace.WriteLine(type + " " + _p.Name);
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
