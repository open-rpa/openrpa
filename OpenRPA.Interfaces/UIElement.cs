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
            try
            {
                RawElement = Element;
                ProcessId = Element.Properties.ProcessId.ValueOrDefault;
                // if(Element.Properties.AutomationId.IsSupported) Id = Element.Properties.AutomationId.ValueOrDefault;
                Name = Element.Properties.Name.ValueOrDefault;
                ClassName = Element.Properties.ClassName.ValueOrDefault;
                Type = Element.Properties.ControlType.ValueOrDefault.ToString();
            }
            catch (Exception)
            {
            }
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
                try
                {
                    if (RawElement == null) return System.Drawing.Rectangle.Empty;
                    if (!RawElement.Properties.BoundingRectangle.IsSupported) return System.Drawing.Rectangle.Empty;
                    return new System.Drawing.Rectangle((int)RawElement.Properties.BoundingRectangle.Value.X,
                        (int)RawElement.Properties.BoundingRectangle.Value.Y, (int)RawElement.Properties.BoundingRectangle.Value.Width,
                        (int)RawElement.Properties.BoundingRectangle.Value.Height);
                }
                catch (Exception)
                {
                    return System.Drawing.Rectangle.Empty;
                }
            }
            set { }
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
        public bool SupportSelect
        {
            get
            {
                try
                {
                    return RawElement.ControlType == FlaUI.Core.Definitions.ControlType.ComboBox;
                        // || RawElement.ControlType == FlaUI.Core.Definitions.ControlType.RadioButton;
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
            }
            catch
            {
            }
            try
            {
                RawElement.FocusNative();
            }
            catch
            {
            }
            try
            {
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
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY, bool DoubleClick, bool AnimateMouse)
        {
            try
            {
                if (Button != Input.MouseButton.Left) { VirtualClick = false; }
                if (VirtualClick)
                {
                    if (RawElement.Patterns.Invoke.IsSupported)
                        if(RawElement.Patterns.Invoke.TryGetPattern(out var InvokePattern))
                    {
                        InvokePattern.Invoke();
                        // Log.Selector(string.Format("UIElement.LegacyIAccessible.set::SetValue::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                        return;
                    }
                    VirtualClick = false;
                }
                if (!VirtualClick)
                {
                    //if (RawElement.Properties.IsOffscreen.IsSupported && RawElement.Properties.IsOffscreen.Value == true)
                    //{
                    //    try
                    //    {
                    //        var automation = AutomationUtil.getAutomation();
                    //        var pc = new FlaUI.Core.Conditions.PropertyCondition(automation.PropertyLibrary.Element.ClassName, "Windows.UI.Core.CoreWindow");
                    //        var _el = RawElement.FindFirstChild(pc);
                    //        RawElement.SetForeground();
                    //    }
                    //    catch (Exception)
                    //    {
                    //    }
                    //}
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
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
        public string Value
        {
            get
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                try
                {
                    Log.Selector(string.Format("UIElement.Value.get::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
                    if (RawElement.Properties.IsPassword.TryGetValue(out var isPassword) && isPassword)
                    {
                        throw new FlaUI.Core.Exceptions.MethodNotSupportedException($"Text from element '{ToString()}' cannot be retrieved because it is set as password.");
                    }
                    if (RawElement.Patterns.Value.TryGetPattern(out var valuePattern) &&
                        valuePattern.Value.TryGetValue(out var value))
                    {
                        Log.Selector(string.Format("UIElement.Value.get::valuePattern::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                        return value;
                    }
                    if (RawElement.Patterns.Text.TryGetPattern(out var textPattern))
                    {
                        Log.Selector(string.Format("UIElement.Value.get::textPattern::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                        return textPattern.DocumentRange.GetText(Int32.MaxValue);
                    }
                }
                catch (Exception)
                {
                }
                Log.Selector(string.Format("UIElement.Value.get::failed::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                return null;
                // throw new FlaUI.Core.Exceptions.MethodNotSupportedException($"AutomationElement '{ToString()}' supports neither ValuePattern or TextPattern");
            }
            set
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                Log.Selector(string.Format("UIElement.Value.set::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
                if (RawElement.Patterns.LegacyIAccessible.TryGetPattern(out var LegacyPattern))
                {
                    LegacyPattern.SetValue(value);
                    Log.Selector(string.Format("UIElement.LegacyIAccessible.set::SetValue::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                }
                else if (RawElement.Patterns.Value.TryGetPattern(out var valuePattern))
                {
                    valuePattern.SetValue(value);
                    Log.Selector(string.Format("UIElement.Value.set::SetValue::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                }
                else
                {
                    Enter(value);
                    Log.Selector(string.Format("UIElement.Value.set::Enter::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                }
            }
        }
        public string SendKeys
        {
            get
            {
                return Value;
            }
            set
            {
                Focus();
                // Click(false, Input.MouseButton.Left, 5, 5 , false, true);
                // UntilResponsive();
                // System.Threading.Thread.Sleep(250);
                TypeText(value);
                UntilResponsive();
            }
        }
        public void TypeText(string text)
        {
            var disposes = new List<IDisposable>();
            var enddisposes = new List<IDisposable>();
            if (string.IsNullOrEmpty(text)) return;

            //var clickdelay = ClickDelay.Get(context);
            //var linedelay = LineDelay.Get(context);
            //var predelay = PreDelay.Get(context);
            //var postdelay = PostDelay.Get(context);
            var clickdelay = TimeSpan.FromMilliseconds(5);
            var linedelay = TimeSpan.FromMilliseconds(5);
            var predelay = TimeSpan.FromMilliseconds(0);
            var postdelay = TimeSpan.FromMilliseconds(100);
            System.Threading.Thread.Sleep(predelay);

            // string[] lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (var i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{')
                {
                    int indexEnd = text.IndexOf('}', i + 1);
                    int indexNextStart = text.IndexOf('{', indexEnd + 1);
                    int indexNextEnd = text.IndexOf('}', indexEnd + 1);
                    if (indexNextStart > indexNextEnd || (indexNextStart == -1 && indexNextEnd > -1)) indexEnd = indexNextEnd;
                    var sub = text.Substring(i + 1, (indexEnd - i) - 1);
                    i = indexEnd;
                    foreach (var k in sub.Split(','))
                    {
                        string key = k.Trim();
                        bool down = false;
                        bool up = false;
                        if (key.EndsWith("down"))
                        {
                            down = true;
                            key = key.Replace(" down", "");
                        }
                        else if (key.EndsWith("up"))
                        {
                            up = true;
                            key = key.Replace(" up", "");
                        }
                        //Keys specialkey;
                        Enum.TryParse<FlaUI.Core.WindowsAPI.VirtualKeyShort>(key, true, out FlaUI.Core.WindowsAPI.VirtualKeyShort vk);
                        if (down)
                        {
                            if (vk > 0)
                            {
                                enddisposes.Add(FlaUI.Core.Input.Keyboard.Pressing(vk));
                            }
                            else
                            {
                                FlaUI.Core.Input.Keyboard.Type(key);
                            }
                        }
                        else if (up)
                        {
                            if (vk > 0)
                            {
                                FlaUI.Core.Input.Keyboard.Release(vk);
                            }
                            else
                            {
                                FlaUI.Core.Input.Keyboard.Type(key);
                            }
                        }
                        else
                        {
                            if (vk > 0)
                            {
                                disposes.Add(FlaUI.Core.Input.Keyboard.Pressing(vk));
                            }
                            else
                            {
                                FlaUI.Core.Input.Keyboard.Type(key);
                            }
                        }
                        System.Threading.Thread.Sleep(clickdelay);
                    }
                    disposes.ForEach(x => { x.Dispose(); });
                }
                else
                {
                    FlaUI.Core.Input.Keyboard.Type(c);
                    System.Threading.Thread.Sleep(clickdelay);
                }
            }
            enddisposes.ForEach(x => { x.Dispose(); });
            System.Threading.Thread.Sleep(postdelay);
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
            // FlaUI.Core.Input.Wait.UntilInputIsProcessed();
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
            if (!(obj is UIElement e)) return false;
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
                return Interfaces.Image.Util.Bitmap2Base64(image);
            }
        }
        public IElement[] Items { 
            get 
            {
                var result = new List<IElement>();
                if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.ComboBox)
                {
                    var combo = RawElement.AsComboBox();
                    foreach (var c in combo.Items) result.Add(new UIElement(c));
                }
                return result.ToArray();
            } 
        }
    }
}
