using Newtonsoft.Json;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
namespace OpenRPA.SAP
{
    public class SAPElementUpdatableProperty
    {
        [JsonIgnore]
        public SAPElement Element { get; set; }
        public string Name { get; set; }
        public string _Value;
        public SAPElementUpdatableProperty(SAPElement Element, SAPElementProperty p)
        {
            this.Element = Element;
            IsReadOnly = p.IsReadOnly;
            Name = p.Name;
            _Value = p.Value;
        }
        public string Value { 
            get 
            {
                return _Value;
            } 
            set 
            {
                if (IsReadOnly) throw new ArgumentException("Cannot update " + Name + ", the field is readonly");
                object[] _parameters = new object[] { value };
                var data = new SAPInvokeMethod(Element.SystemName, Element.id, Name, _parameters);
                var message = new SAPEvent("setproperty");
                message.Set(data);
                var result = SAPhook.Instance.SendMessage(message, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));

            }
        }
        public bool IsReadOnly { get; set; }
        public override string ToString()
        {
            if (IsReadOnly) return "*" + Name + " " + Value;
            return Name + " " + Value;
        }
    }
    public class SAPElement : IElement
    {
        object IElement.RawElement { get => raw; set => raw = value as object; }
        private object raw;
        private SAPEventElement sapelement;
        public SAPElement()
        {
        }
        public SAPElement(SAPConnection connection)
        {
            raw = connection;
            Name = connection.Name;
            id = connection.Id;
            ContainerType = true;
            Role = "GuiConnection";
        }
        public SAPElement(SAPSession session)
        {
            raw = session;
            Name = session.Info.SystemName;
            id = session.Id;
            SystemName = session.Info.SystemName;
            ContainerType = true;
            Role = "GUISession";
        }
        public SAPElement(SAPElement Parent, SAPEventElement Element)
        {
            raw = Element;
            sapelement = Element;
            if (Parent!=null)
            {
                RefreshParent = false;
                _Parent = Parent;
            }
            Name = Element.Name;
            id = Element.Id;
            SystemName = Element.SystemName;
            ContainerType = Element.ContainerType;
            Role = Element.type;
            Path = Element.Path;
            Cell = Element.Cell;
            if (Properties == null) Properties = new Dictionary<string, SAPElementUpdatableProperty>();
            if(Element.Properties!=null)
                foreach (var p in Element.Properties)
                {
                    Properties.Add(p.Name, new SAPElementUpdatableProperty(this,p));
                    //if (p.Name == "Left") X = int.Parse(p.Value);
                    //if (p.Name == "Top")  Y = int.Parse(p.Value);
                    if (p.Name == "ScreenLeft") X = int.Parse(p.Value);
                    if (p.Name == "ScreenTop") Y = int.Parse(p.Value);
                    if (p.Name == "Width") Width = int.Parse(p.Value);
                    if (p.Name == "Height") Height = int.Parse(p.Value);
                    if (p.Name == "Name") Name = p.Value;
                    if (p.Name == "Type") Role = p.Value;
                }
            if(Element.Items != null)
            {
                var items = new List<IElement>();
                foreach (var item in Element.Items) items.Add( new SAPElement(this, item));
                if (Element.Items != null) _items = items.ToArray();
            }
            if(Element.Children != null)
            {
                var children = new List<SAPElement>();
                foreach (var c in Element.Children)
                {
                    children.Add(new SAPElement(this, c));
                }
                _Children = children.ToArray();
                RefreshChildren = false;
            }
        }
        public System.Drawing.Rectangle Rectangle
        {
            get
            {
                return new System.Drawing.Rectangle(X, Y, Width, Height);
            }
            set { }
        }
        public string Name { get; set; }
        public string Role { get; set; }
        public bool ContainerType { get; set; }
        public string id { get; set; }
        public string Path { get; set; }
        public string Cell { get; set; }        
        public string SystemName { get; set; }        
        public string Tip { get; set; }
        public int Action { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Dictionary<string, SAPElementUpdatableProperty> Properties { get; set; }
        public bool SupportInput
        {
            get
            {
                if (Role == "GuiTextEdit") return false;
                return false;
            }
        }
        public string Value
        {
            get
            {
                if(Properties==null) return null;
                if (Properties.ContainsKey("Value")) return Properties["Value"].Value;
                if (Properties.ContainsKey("Text")) return Properties["Text"].Value;
                if (Properties.ContainsKey("Selected")) return Properties["Selected"].Value;
                return null;
            }
            set
            {
                if (Properties == null) return;
                if (Properties.ContainsKey("Text"))
                {
                    Properties["Text"].Value = value;
                }
                else if (Properties.ContainsKey("Value"))
                {
                    Properties["Value"].Value = value;
                }
                else if (Properties.ContainsKey("Selected"))
                {
                    Properties["Selected"].Value = value;
                }
                else
                {
                    throw new ArgumentException("Don't know how to set value for type " + Role);
                }
            }
        }
        private bool RefreshParent = true;
        private SAPElement _Parent = null;
        [JsonIgnore]
        public SAPElement Parent
        {
            get
            {
                if (!RefreshParent) return _Parent;
                try
                {
                    var msg = new SAPEvent("getitem");
                    msg.Set(new SAPEventElement() { Id = id, SystemName = SystemName, Path = Path, Cell = Cell });
                    msg = SAPhook.Instance.SendMessage(msg, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
                    if (msg != null)
                    {
                        var res = msg.Get<SAPEventElement>();
                        if(!string.IsNullOrEmpty(res.Id)) _Parent = new SAPElement(null, res);
                    }
                    RefreshParent = false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return _Parent;
            }
        }
        private bool RefreshChildren = true;
        [JsonIgnore, IgnoreDataMember]
        public bool VisibleOnly { get; set; } = false;
        private SAPElement[] _Children = new SAPElement[] { };
        [JsonIgnore]
        public SAPElement[] Children
        {
            get
            {
                if (!RefreshChildren) return _Children;
                if(!ContainerType && this.Role != "GuiTree" && this.Role != "GuiTable" && this.Role != "GuiGrid" && this.Role != "GuiTreeNode")
                {
                    System.Diagnostics.Trace.WriteLine(this.Role);
                    return _Children;
                }
                var result = new List<SAPElement>();
                try
                {
                    var msg = new SAPEvent("getitem");
                    msg.Set(new SAPEventElement() { Id = id, SystemName = SystemName, Path = Path, Cell = Cell, VisibleOnly = VisibleOnly, MaxItem = 50 });
                    msg = SAPhook.Instance.SendMessage(msg, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
                    if(msg!=null)
                    {
                        var ele = msg.Get<SAPEventElement>();
                        // var Parent = new SAPElement(this, ele);
                        if (ele !=null && !string.IsNullOrEmpty(ele.Id) && ele.Children != null)
                            foreach (var c in ele.Children)
                            {
                                result.Add(new SAPElement(this, c));
                            }
                    } 
                    RefreshChildren = false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                _Children = result.ToArray();
                return _Children;
            }
        }
        public void Refresh() { }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY, bool DoubleClick, bool AnimateMouse)
        {
            if (Button != Input.MouseButton.Left) { VirtualClick = false; }
            if (!VirtualClick)
            {
                if (AnimateMouse)
                {
                    Input.InputDriver.Instance.AnimateMouseMove(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                }
                else
                {
                    Input.InputDriver.Instance.MouseMove(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                }
                Input.InputDriver.Click(Button);
                if (DoubleClick) Input.InputDriver.Click(Button);
                return;
            }  
            else
            {
                object[] _parameters = new object[] { };
                // var Action = "Press";
                var Action = "";
                if (Role == "GuiButton" || Role == "GuiGridCell" || Role == "GuiGridNode")
                {
                    Action = "Press";
                } 
                else if(Role == "GuiTreeNode" || Role == "GuiTreeItem")
                {
                    // Action = "ExpandNode";
                    Action = "DoubleClickNode";
                    _parameters = new object[] { Path };
                }
                else if(Role == "GuiRadioButton")
                {
                    Action = "Select";
                }
                else if (Role == "GuiOkCodeField" || Role == "GuiTextField" || Role == "GuiPasswordField")
                {
                    Action = "SetFocus";
                }
                if (!string.IsNullOrEmpty(Action))
                {
                    var data = new SAPInvokeMethod(SystemName, id, Action, _parameters);
                    var message = new SAPEvent("invokemethod");
                    message.Set(data);
                    var result = SAPhook.Instance.SendMessage(message, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
                }
                else
                {
                    throw new Exception("Unknown click type " + Role);
                }
            }
        }
        public void EnsureVisible()
        {
            object[] _parameters = new object[] { };
            SAPInvokeMethod data = null;
            SAPEvent message;
            SAPEvent result;
            if (Items.Length > 0)
            {
                _parameters = new object[] { Path, Items[0].Name };
                data = new SAPInvokeMethod(SystemName, id, "ensureVisibleHorizontalItem", _parameters);
                message = new SAPEvent("invokemethod");
                message.Set(data);
                result = SAPhook.Instance.SendMessage(message, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            }
            _parameters = new object[] { };
            data = new SAPInvokeMethod(SystemName, id, "SetFocus", _parameters);
            message = new SAPEvent("invokemethod");
            message.Set(data);
            result = SAPhook.Instance.SendMessage(message, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));

        }
        public void Focus()
        {
            //REM start transaction
            //session.findById("wnd[0]/tbar[0]/okcd").text = "/nrsa1"
            //session.findById("wnd[0]").sendVKey 0
            //REM Search in the tree for object "ZCSG01"
            //session.findById("wnd[0]/shellcont[0]/shell/shellcont[1]/shell/shellcont[1]/shell").topNode = " 1"
            //session.findById("wnd[0]/shellcont[1]/shell/shellcont[0]/shell/shellcont[1]/shell/shellcont[1]/shell").topNode = " 1"
            //session.findById("wnd[0]/shellcont[1]/shell/shellcont[0]/shell/shellcont[1]/shell/shellcont[0]/shell").pressButton "%AWB_TREE_SEARCH"
            //session.findById("wnd[1]/usr/txtRSAWBN_S_DYNPRO_0500-SEARCH_TERM").text = "ZCSG01"
            //session.findById("wnd[1]/usr/txtRSAWBN_S_DYNPRO_0500-SEARCH_TERM").caretPosition = 6
            //session.findById("wnd[1]").sendVKey 0
            //REM "ZCSG01" is found as node 37
            //Dim strKey
            //Dim strTxt1, strTxt2
            //strKey = session.findById("wnd[0]/shellcont[1]/shell/shellcont[0]/shell/shellcont[1]/shell/shellcont[1]/shell").GetFocusedNodeKey
            //strTxt1 = Right(" " & Cstr(strKey), 11)
            //strTxt2 = Right(" " & Cstr(strKey + 1), 11)
            //session.findById("wnd[0]/shellcont[1]/shell/shellcont[0]/shell/shellcont[1]/shell/shellcont[1]/shell").expandNode strTxt1
            //session.findById("wnd[0]/shellcont[1]/shell/shellcont[0]/shell/shellcont[1]/shell/shellcont[1]/shell").topNode = " 1"
            //session.findById("wnd[0]/shellcont[1]/shell/shellcont[0]/shell/shellcont[1]/shell/shellcont[1]/shell").selectItem strTxt2,"COL1"
            //session.findById("wnd[0]/shellcont[1]/shell/shellcont[0]/shell/shellcont[1]/shell/shellcont[1]/shell").ensureVisibleHorizontalItem strTxt2,"COL1"

            //object[] _parameters = new object[] { };
            //var Action = "";
            //if (Role == "GuiButton")
            //{
            //    Action = "ensureVisibleHorizontalItem";
            //}
            //else if (Role == "GuiTreeNode" || Role == "GuiTreeItem")
            //{
            //    EnsureVisible();
            //}
            //if (!string.IsNullOrEmpty(Action))
            //{
            //    var data = new SAPInvokeMethod(SystemName, id, Action, _parameters);
            //    var message = new SAPEvent("invokemethod");
            //    message.Set(data);
            //    var result = SAPhook.Instance.SendMessage(message, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            //}
            //else
            //{
            //    throw new Exception("Unknown focus type " + Role);
            //}
            EnsureVisible();
        }
        public Task Highlight(bool Blocking, System.Drawing.Color Color, TimeSpan Duration)
        {
            if (!Blocking)
            {
                Task.Run(() => _Highlight(Color, Duration));
                return Task.CompletedTask;
            }
            return _Highlight(Color, Duration);
        }
        public Task _Highlight(System.Drawing.Color Color, TimeSpan Duration)
        {
            //var data = new SAPInvokeMethod(SystemName, id, null, null);
            //var message = new SAPEvent("highlight");
            //message.Set(data);
            //var result = SAPhook.Instance.SendMessage(message, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            Log.Output(Rectangle.ToString());
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
            {
                _overlayWindow.BackColor = Color;
                _overlayWindow.Visible = true;
                // _overlayWindow.SetTimeout(Duration);
                _overlayWindow.Bounds = Rectangle;
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                do
                {
                    System.Threading.Thread.Sleep(10);
                    _overlayWindow.TopMost = true;
                } while (_overlayWindow.Visible && sw.Elapsed < Duration);
                return Task.CompletedTask;
            }
        }
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Tip)) return Tip;
            var name = Name;
            if (Properties.ContainsKey("Key")) name = Properties["Key"].Value;
            if (Properties.ContainsKey("Cell")) name = Properties["Cell"].Value;

            return "id:" + id + " Role:" + Role + " Name: " + name;
        }
        public override bool Equals(object obj)
        {
            var e = obj as SAPElement;
            if (e == null) return false;
            if (e.Name != Name) return false;
            if (e.Role != Role) return false;
            if (e.id != id) return false;
            if (e.Tip != Tip) return false;
            if (e.Path != Path) return false;
            if (e.ToString() != ToString()) return false;
            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public string ImageString()
        {
            var AddedWidth = 10;
            var AddedHeight = 10;
            var ScreenImageWidth = Rectangle.Width + AddedWidth;
            var ScreenImageHeight = Rectangle.Height + AddedHeight;
            var ScreenImagex = Rectangle.X - (AddedWidth / 2);
            var ScreenImagey = Rectangle.Y - (AddedHeight / 2);
            if (ScreenImagex < 0) ScreenImagex = 0; if (ScreenImagey < 0) ScreenImagey = 0;
            using (var image = Interfaces.Image.Util.Screenshot(ScreenImagex, ScreenImagey, ScreenImageWidth, ScreenImageHeight, Interfaces.Image.Util.ActivityPreviewImageWidth, Interfaces.Image.Util.ActivityPreviewImageHeight))
            {
                return Interfaces.Image.Util.Bitmap2Base64(image);
            }
        }
        IElement[] _items = new IElement[] { };
        public IElement[] Items
        {
            get
            {
                return _items;
            }
        }
        public string[] ColumnNames(IElement[] Items)
        {
            var result = new List<string>();
            foreach (var item in Items)
            {
                var ele = item as SAPElement;
                var _name = item.Name;
                if (ele.Properties.ContainsKey("Key")) _name = ele.Properties["Key"].Value;
                if (ele.Properties.ContainsKey("Cell")) _name = ele.Properties["Cell"].Value;
                // if (ele.Properties.ContainsKey("Title")) _name = ele.Properties["Title"].Value;
                if (result.Contains(_name) && ele.Properties.ContainsKey("Title")) _name = item.Name;
                result.Add(_name);
            }
            return result.ToArray();
        }
        public DataTable ToDatatable()
        {
            var name = Name;
            if (string.IsNullOrEmpty(name)) name = "Table1";
            var result = new DataTable(name);
            if (Children == null || Children.Length == 0)
            {
                if(Items != null && Items.Length > 0)
                {
                    foreach(var item in Items)
                    {
                        var ele = item as SAPElement;
                        var _name = item.Name;
                        if (ele.Properties.ContainsKey("Key")) _name = ele.Properties["Key"].Value;
                        result.Columns.Add(_name);
                    }
                    result.Rows.Add(ToDataRow(result));
                    return result;
                }
                return null;
            }
            var _c = Children[0];
            var cc = Children[0].Children;
            var ci = Children[0].Items;
            if (Children[0].Items == null || Children[0].Items.Length == 0) return null;
            var columnNames = ColumnNames(Children[0].Items);
            foreach (var _name in columnNames)
            {
                result.Columns.Add(_name);
            }
            foreach (var c in Children)
            {
                var row = c.ToDataRow(result, columnNames);
                if(row!=null) result.Rows.Add(row);
            }
            result.AcceptChanges();
            return result;
        }
        public DataRow ToDataRow(DataTable dt)
        {
            return ToDataRow(dt, ColumnNames(Items));
        }
        public DataRow ToDataRow(DataTable dt, string[] ColumnNames)
        {
            if (Items == null) return null;
            var result = dt.NewRow();
            for(var i = 0; i < ColumnNames.Length; i++)
            {
                var ele = Items[i] as SAPElement;
                var name = ColumnNames[i];
                result[name] = ele.Value;
            }
            return result;
        }
    }
}
