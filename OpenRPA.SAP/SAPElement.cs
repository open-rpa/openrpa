using Newtonsoft.Json;
using OpenRPA.Interfaces;
using SAPFEWSELib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAP
{
    public class SAPElement : IElement
    {
        object IElement.RawElement { get => Component; set => Component = value as GuiComponent; }
        private GuiComponent Component;
        public SAPElement()
        {
        }
        public SAPElement(GuiComponent Component)
        {
            this.Component = Component;
            id = Component.Id;
            Name = Component.Name;
            Role = Component.GetDetailType();
            if (Component is GuiVComponent)
            {
                var vc = Component as GuiVComponent;
                try
                {
                    Tip = vc.DefaultTooltip == "" ? vc.Tooltip : vc.DefaultTooltip;
                }
                catch
                {

                }
            }
        }

        public SAPElement(GuiSession session)
        {
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
        public string id { get; set; }
        public string Tip { get; set; }
        public int Action { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsVisible { get; set; }
        public Dictionary<string, object> properties { get; set; }
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
                return null;
            }
            set
            {
            }
        }

        [JsonIgnore]
        public SAPElement Parent
        {
            get
            {
                //return new SAPElement(_parent);
                return null;
            }
        }
        [JsonIgnore]
        public SAPElement[] Children
        {
            get
            {
                var result = new List<SAPElement>();
                //foreach (var c in this.c.GetChildren())
                //{
                //    result.Add(new SAPElement(c));
                //}
                return result.ToArray();
            }
        }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY, bool DoubleClick, bool AnimateMouse)
        {
            if (Button != Input.MouseButton.Left) { VirtualClick = false; }
            if (!VirtualClick)
            {
                if (AnimateMouse)
                {
                    FlaUI.Core.Input.Mouse.MoveTo(new System.Drawing.Point(Rectangle.X + OffsetX, Rectangle.Y + OffsetY));
                }
                else
                {
                    NativeMethods.SetCursorPos(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                }
                Input.InputDriver.Click(Button);
                if (DoubleClick) Input.InputDriver.Click(Button);
                return;
            } 
        }
        public void Focus()
        {
            throw new NotImplementedException();
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
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
            {
                _overlayWindow.BackColor = Color;
                _overlayWindow.Visible = true;
                _overlayWindow.SetTimeout(Duration);
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
            return "id:" + id + " Role:" + Role + " Name: " + Name;
        }
        public override bool Equals(object obj)
        {
            var e = obj as SAPElement;
            if (e == null) return false;
            if (e.Name != Name) return false;
            if (e.Role != Role) return false;
            if (e.id != id) return false;
            if (e.Tip != Tip) return false;
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
        public IElement[] Items
        {
            get
            {
                var result = new List<IElement>();
                return result.ToArray();
            }
        }

    }
}
