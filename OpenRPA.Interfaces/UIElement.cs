using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using FlaUI.Core.AutomationElements;
using Newtonsoft.Json;

namespace OpenRPA
{
    public class UIElement : IElement
    {
        public UIElement(AutomationElement Element)
        {
            RawElement = Element;
            ProcessId = Element.Properties.ProcessId.ValueOrDefault;
            // if(Element.Properties.AutomationId.IsSupported) Id = Element.Properties.AutomationId.ValueOrDefault;
            Name = Element.Properties.Name.ValueOrDefault;
            ClassName = Element.Properties.ClassName.ValueOrDefault;
            Type = Element.Properties.ControlType.ValueOrDefault.ToString();
        }
        public void Refresh()
        {
            try
            {
                int pendingCounter = 0;
                while (!RawElement.Properties.BoundingRectangle.IsSupported && pendingCounter < 6)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(50);
                    pendingCounter++;
                }
                ProcessId = RawElement.Properties.ProcessId.ValueOrDefault;
                // Id = RawElement.Properties.AutomationId.ValueOrDefault;
                Name = RawElement.Properties.Name.ValueOrDefault;
                ClassName = RawElement.Properties.ClassName.ValueOrDefault;
                Type = RawElement.Properties.ControlType.ValueOrDefault.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
        }
        [JsonIgnore]
        public AutomationElement RawElement { get; private set; }
        object IElement.RawElement { get => RawElement; set => RawElement = value as AutomationElement; }
        public System.Drawing.Rectangle Rectangle
        {
            get
            {
                if (RawElement == null) return System.Drawing.Rectangle.Empty;
                if (!RawElement.Properties.BoundingRectangle.IsSupported) return System.Drawing.Rectangle.Empty;
                return new System.Drawing.Rectangle((int)RawElement.Properties.BoundingRectangle.Value.X,
                    (int)RawElement.Properties.BoundingRectangle.Value.Y, (int)RawElement.Properties.BoundingRectangle.Value.Width,
                    (int)RawElement.Properties.BoundingRectangle.Value.Height);
            }
        }
        public int ProcessId { get; set; }
        // public string Id { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public string Type { get; set; }
        public string ControlType
        {
            get
            {
                try
                {
                    return RawElement.Properties.ControlType.ToString();
                }
                catch (Exception)
                {
                }
                return FlaUI.Core.Definitions.ControlType.Custom.ToString();
            }
        }
        public bool SupportInput
        {
            get
            {
                try
                {
                    //return rawElement.Patterns.TextEdit.IsSupported || rawElement.Patterns.Text.IsSupported || rawElement.Patterns.Text2.IsSupported
                    return RawElement.ControlType == FlaUI.Core.Definitions.ControlType.Edit
                        || RawElement.ControlType == FlaUI.Core.Definitions.ControlType.Document;
                }
                catch (Exception)
                {
                }
                return false;
            }
        }
        [JsonIgnore]
        public UIElement Parent
        {
            get
            {
                //if (TreeWalker.RawViewWalker.GetParent(rawElement) is AutomationElement rawParent)
                //{
                //    return new UIElement(rawParent);
                //}
                return new UIElement(RawElement.Parent);
                //return null;
            }
        }
        public void Focus()
        {
            UntilResponsive();
            try
            {
                RawElement.SetForeground();
                RawElement.FocusNative();
                RawElement.Focus();
            }
            catch
            {
            }
        }
        private void UntilResponsive()
        {
            if (ProcessId <= 0) return;
            var process = System.Diagnostics.Process.GetProcessById(ProcessId);
            while (!process.Responding) { }
        }
        public void Click(bool VirtualClick, int OffsetX, int OffsetY)
        {
            Click(VirtualClick, Input.MouseButton.Left, OffsetX, OffsetY);
        }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY)
        {
            try
            {
                //var automation = AutomationUtil.getAutomation();
                //var pc = new FlaUI.Core.Conditions.PropertyCondition(automation.PropertyLibrary.Element.ClassName, "Windows.UI.Core.CoreWindow");
                //var _el = element.FindFirstChild(pc);
                // RawElement.SetForeground();
            }
            catch (Exception)
            {
            }
            try
            {
                if (Button != Input.MouseButton.Left) { VirtualClick = false; }
                if (VirtualClick && !RawElement.Patterns.Invoke.IsSupported) VirtualClick = false;
                if (VirtualClick)
                {
                    try
                    {
                        var invokePattern = RawElement.Patterns.Invoke.Pattern;
                        invokePattern.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("UIElement VirtualClick failed: " + ex.Message);
                        VirtualClick = false;
                    }
                }
                if (!VirtualClick)
                {
                    //Log.Debug("MouseMove to " + Rectangle.X + "," + Rectangle.Y + " and click");
                    //Input.InputDriver.Instance.MouseMove(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                    //Input.InputDriver.DoMouseClick();
                    //Log.Debug("Click done");
                    var point = new FlaUI.Core.Shapes.Point(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                    //FlaUI.Core.Input.Mouse.MoveTo(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                    FlaUI.Core.Input.MouseButton flabuttun = FlaUI.Core.Input.MouseButton.Left;
                    if (Button == Input.MouseButton.Middle) flabuttun = FlaUI.Core.Input.MouseButton.Middle;
                    if (Button == Input.MouseButton.Right) flabuttun = FlaUI.Core.Input.MouseButton.Right;
                    FlaUI.Core.Input.Mouse.Click(flabuttun, point);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public Task Highlight(bool Blocking, System.Drawing.Color Color, TimeSpan Duration)
        {
            if (!Blocking) {
                Task.Run(() => _Highlight(Color, Duration));
                return Task.CompletedTask;
            }
            return _Highlight(Color, Duration);
        }
        public Task _Highlight(System.Drawing.Color Color, TimeSpan Duration)
        {
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow())
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
        public string Value
        {
            get
            {
                try
                {
                    if (RawElement.Properties.IsPassword.TryGetValue(out var isPassword) && isPassword)
                    {
                        throw new FlaUI.Core.Exceptions.MethodNotSupportedException($"Text from element '{ToString()}' cannot be retrieved because it is set as password.");
                    }
                    if (RawElement.Patterns.Value.TryGetPattern(out var valuePattern) &&
                        valuePattern.Value.TryGetValue(out var value))
                    {
                        return value;
                    }
                    if (RawElement.Patterns.Text.TryGetPattern(out var textPattern))
                    {
                        return textPattern.DocumentRange.GetText(Int32.MaxValue);
                    }
                }
                catch (Exception)
                {
                }
                return null;
                // throw new FlaUI.Core.Exceptions.MethodNotSupportedException($"AutomationElement '{ToString()}' supports neither ValuePattern or TextPattern");
            }
            set
            {
                if (RawElement.Patterns.Value.TryGetPattern(out var valuePattern))
                {
                    valuePattern.SetValue(value);
                }
                else
                {
                    Enter(value);
                }
            }
        }
        public void Enter(string value)
        {
            RawElement.Focus();
            var valuePattern = RawElement.Patterns.Value.PatternOrDefault;
            valuePattern?.SetValue(String.Empty);
            if (String.IsNullOrEmpty(value)) return;

            var lines = value.Replace("\r\n", "\n").Split('\n');
            FlaUI.Core.Input.Keyboard.Type(lines[0]);
            foreach (var line in lines.Skip(1))
            {
                FlaUI.Core.Input.Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
                FlaUI.Core.Input.Keyboard.Type(line);
            }
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
        }
        public override string ToString()
        {
            string result = "";
            if (!string.IsNullOrEmpty(ControlType)) result += " " + ControlType;
            // if (!string.IsNullOrEmpty(Id)) result += " id: " + Id;
            if (!string.IsNullOrEmpty(Name)) result += " Name: " + Name;
            if (!string.IsNullOrEmpty(ClassName)) result += " ClassName: " + ClassName;
            return result.Trim();
        }
        public override bool Equals(object obj)
        {
            var e = obj as UIElement;
            if (e == null) return false;
            if (e.ProcessId != ProcessId) return false;
            // if (e.Id != Id) return false;
            if (e.Name != Name) return false;
            if (e.ClassName != ClassName) return false;
            if (e.Type != Type) return false;
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
                // Interfaces.Image.Util.SaveImageStamped(image, System.IO.Directory.GetCurrentDirectory(), "UIElement");
                return Interfaces.Image.Util.Bitmap2Base64(image);
            }
        }

    }
}
