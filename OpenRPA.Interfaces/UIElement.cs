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
using FlaUI.Core.Tools;

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
                FrameworkId = Element.Properties.FrameworkId.ValueOrDefault;
                _ = Rectangle;
                _ = Position;
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
                _Rectangle = Rectangle;
                _Position = Position;
                ProcessId = RawElement.Properties.ProcessId.ValueOrDefault;
                // Id = RawElement.Properties.AutomationId.ValueOrDefault;
                Name = RawElement.Properties.Name.ValueOrDefault;
                ClassName = RawElement.Properties.ClassName.ValueOrDefault;
                Type = RawElement.Properties.ControlType.ValueOrDefault.ToString();
                FrameworkId = RawElement.Properties.FrameworkId.ValueOrDefault;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
        }
        [JsonIgnore]
        public AutomationElement RawElement { get; private set; }
        object IElement.RawElement { get => RawElement; set => RawElement = value as AutomationElement; }
        private System.Drawing.Rectangle _Rectangle = System.Drawing.Rectangle.Empty;
        public System.Drawing.Rectangle Rectangle
        {
            get
            {
                try
                {
                    if (RawElement == null) return System.Drawing.Rectangle.Empty;
                    if (!RawElement.Properties.BoundingRectangle.IsSupported) return System.Drawing.Rectangle.Empty;
                    if (_Rectangle == System.Drawing.Rectangle.Empty)
                    {
                        _Rectangle = new System.Drawing.Rectangle((int)RawElement.Properties.BoundingRectangle.Value.X,
                        (int)RawElement.Properties.BoundingRectangle.Value.Y, (int)RawElement.Properties.BoundingRectangle.Value.Width,
                        (int)RawElement.Properties.BoundingRectangle.Value.Height);
                    }
                    return _Rectangle;
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
        public string FrameworkId { get; set; }
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
                    if (RawElement.Properties.ControlType.IsSupported && !string.IsNullOrEmpty(RawElement.Properties.ControlType.Value.ToString()))
                    {
                        return RawElement.ControlType == FlaUI.Core.Definitions.ControlType.Edit
                        || RawElement.ControlType == FlaUI.Core.Definitions.ControlType.Document;
                    }
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
                        if (RawElement.Patterns.Invoke.TryGetPattern(out var InvokePattern))
                        {
                            if (RawElement.IsEnabled)
                            {
                                InvokePattern.Invoke();
                            }
                            else
                            {
                                try
                                {
                                    InvokePattern.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Failed clicking disabled object", ex);
                                }
                            }
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
            if (!Blocking)
            {
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
                    if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.List)
                    {
                        var combo = RawElement.AsListBox();
                        if (combo.SelectedItem != null)
                        {
                            return combo.SelectedItem.Name;
                        }
                    }
                    if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox)
                    {
                        var combo = RawElement.AsCheckBox();
                        if (combo.IsChecked.HasValue && combo.IsChecked.Value)
                        {
                            return "true";
                        }
                        return "false";
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
                if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.List)
                {
                    var combo = RawElement.AsListBox();
                    combo.Select(value);
                }
                if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox)
                {
                    var combo = RawElement.AsCheckBox();
                    if (!string.IsNullOrEmpty(value) && value.ToLower() == "true")
                    {
                        combo.IsChecked = true;
                    }
                    else
                    {
                        combo.IsChecked = false;
                    }
                }

            }
        }
        public bool IsChecked
        {
            get
            {
                if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox)
                {
                    var combo = RawElement.AsCheckBox();
                    if (combo.IsChecked.HasValue && combo.IsChecked.Value)
                    {
                        return true;
                    }
                }
                else if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.RadioButton)
                {
                    var radio = RawElement.AsRadioButton();
                    if (radio.IsChecked) return true;
                }
                return false;
            }
            set
            {
                if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox)
                {
                    var combo = RawElement.AsCheckBox();
                    combo.IsChecked = value;
                }
                else if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.RadioButton)
                {
                    var radio = RawElement.AsRadioButton();
                    radio.IsChecked = value;
                }
            }
        }
        public void SelectItem(UIElement element)
        {
            if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.List)
            {
                var combo = RawElement.AsListBox();
                for (var i = 0; i < combo.Items.Length; i++)
                {
                    if (combo.Items[i].Name == element.Value)
                    {
                        combo.AddToSelection(i);
                    }

                }
            }
        }
        public int SelectedIndex
        {
            set
            {
                if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.List)
                {
                    var combo = RawElement.AsListBox();
                    combo.Select(value);
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
        public IElement[] Items
        {
            get
            {

                var result = new List<IElement>();
                if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.ComboBox)
                {
                    var combo = RawElement.AsComboBox();
                    foreach (var c in combo.Items) result.Add(new UIElement(c));
                }
                else if (RawElement.ControlType == FlaUI.Core.Definitions.ControlType.List)
                {
                    var combo = RawElement.AsListBox();
                    foreach (var c in combo.Items)
                    {
                        var tt = c.AsListBoxItem();
                        result.Add(new UIElement(tt));
                    }
                }
                return result.ToArray();
            }
        }
        public Window GetWindow()
        {
            AutomationElement last = RawElement;
            AutomationElement current = RawElement;
            do
            {
                try
                {
                    last = current;
                    current = current.Parent;
                }
                catch (Exception)
                {
                    // throw;
                }
            } while (current != null && current.Parent != null);
            Window window = last.AsWindow();
            return window;
            //Window window = RawElement.AsWindow();
            //FlaUI.Core.Definitions.ControlType ct = FlaUI.Core.Definitions.ControlType.Button;
            //if (RawElement.ControlType != FlaUI.Core.Definitions.ControlType.Window)
            //{
            //    AutomationElement item = RawElement.Parent;
            //    do
            //    {
            //        item = item.Parent;
            //        try
            //        {
            //            if(item!=null) ct = item.ControlType;
            //        }
            //        catch (Exception)
            //        {
            //            ct = FlaUI.Core.Definitions.ControlType.Button;
            //        }
            //    } while (item != null && ct != FlaUI.Core.Definitions.ControlType.Window);
            //    if (item != null && ct == FlaUI.Core.Definitions.ControlType.Window)
            //    {
            //        window = item.AsWindow();
            //    }
            //}
            //try
            //{
            //    if(window.Parent != null)
            //    {
            //        ct = window.Parent.ControlType;
            //    }
            //}
            //catch (Exception)
            //{
            //    ct = FlaUI.Core.Definitions.ControlType.Button;
            //}
            //if(window.Parent != null && ct == FlaUI.Core.Definitions.ControlType.Window)
            //{
            //    window = window.Parent.AsWindow();
            //}
            //return window;
        }
        public void SetPosition(int X, int Y)
        {
            Window window = GetWindow();
            if (window == null) return;

            //if(RawElement.Properties.ProcessId.IsSupported)
            //{
            //    var processId = RawElement.Properties.ProcessId.Value;
            //    var p = System.Diagnostics.Process.GetProcessById(processId);
            //    IntPtr handle = p.Handle;
            //    if(NativeMethods.IsImmersiveProcess(handle))
            //    {
            //        var automation = AutomationUtil.getAutomation();
            //        var pc = new FlaUI.Core.Conditions.PropertyCondition(automation.PropertyLibrary.Element.ClassName, "Windows.UI.Core.CoreWindow");
            //        var _el = RawElement.FindFirstChild(pc);
            //        if (_el != null)
            //        {
            //            window = _el.AsWindow();
            //        } else
            //        {
            //            window = automation.FromPoint(WindowPosition).AsWindow();
            //        }
            //    }
            //}
            //window.Move(X, Y);

            var size = WindowSize;
            NativeMethods.MoveWindow(window.Properties.NativeWindowHandle.Value, X, Y, size.Width, size.Height, true);



            //window.Move(X, Y);
            //NativeMethods.RECT rect;
            //IntPtr hWnd = window.Properties.NativeWindowHandle.Value;
            //if (NativeMethods.GetWindowRect(hWnd, out rect))
            //{
            //    // NativeMethods.MoveWindow(hWnd, X, Y, WindowSize.Width, WindowSize.Height, true);
            //}

        }
        public void SetWindowPosition(int X, int Y)
        {
            WindowPosition = new System.Drawing.Point(X, Y);
        }
        System.Drawing.Point _Position = System.Drawing.Point.Empty;
        public System.Drawing.Point Position
        {
            get
            {
                if (_Position == System.Drawing.Point.Empty)
                {
                    _Position = new System.Drawing.Point(RawElement.BoundingRectangle.X, RawElement.BoundingRectangle.Y);
                }
                return _Position;
            }
            set
            {
                if (RawElement.Patterns.Transform.TryGetPattern(out var tranPattern))
                {
                    if (tranPattern.CanMove)
                    {
                        tranPattern.Move(value.X, value.Y);
                    }
                }
                if (RawElement.Patterns.Transform2.TryGetPattern(out var tran2Pattern))
                {
                    if (tran2Pattern.CanMove)
                    {
                        tran2Pattern.Move(value.X, value.Y);
                    }
                }
            }
        }
        System.Drawing.Point WindowPosition
        {
            get
            {
                var window = GetWindow();
                return new System.Drawing.Point(window.BoundingRectangle.X, window.BoundingRectangle.Y);
            }
            set
            {
                SetPosition(value.X, value.Y);
            }
        }
        public System.Drawing.Rectangle WindowRectangle
        {
            get
            {
                try
                {
                    var window = GetWindow();
                    return new System.Drawing.Rectangle((int)window.Properties.BoundingRectangle.Value.X,
                        (int)window.Properties.BoundingRectangle.Value.Y, (int)window.Properties.BoundingRectangle.Value.Width,
                        (int)window.Properties.BoundingRectangle.Value.Height);
                }
                catch (Exception)
                {
                    return System.Drawing.Rectangle.Empty;
                }
            }
            set
            {
                var window = GetWindow();
                NativeMethods.MoveWindow(window.Properties.NativeWindowHandle.Value, value.X, value.Y, value.Width, value.Height, true);
            }
        }
        public static double MovePixelsPerMillisecond { get; } = 2; // 0.5;
        public static double MovePixelsPerStep { get; } = 10;
        public void MoveWindowTo(int newX, int newY)
        {
            // Get starting position
            var startPos = WindowPosition;
            var endPos = new System.Drawing.Point(newX, newY);

            // Break out if there is no positional change
            if (startPos == endPos)
            {
                return;
            }

            // Calculate some values for duration and interval
            var totalDistance = startPos.Distance(newX, newY);
            var duration = TimeSpan.FromMilliseconds(Convert.ToInt32(totalDistance / MovePixelsPerMillisecond));
            var steps = Math.Max(Convert.ToInt32(totalDistance / MovePixelsPerStep), 1); // Make sure to have et least one step
            var interval = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / steps);
            // Execute the movement
            FlaUI.Core.Input.Interpolation.Execute(point =>
            {
                WindowPosition = point;
            }, startPos, endPos, duration, interval, true);
        }
        public void MoveWindowTo(int newX, int newY, int newW, int newH)
        {
            var startPos = WindowRectangle;
            var endPos = new System.Drawing.Rectangle(newX, newY, newW, newH);

            // Break out if there is no positional change
            if (startPos == endPos)
            {
                return;
            }
            var steps = Config.local.move_animation_steps;
            var runtime = Config.local.move_animation_run_time;
            var xsteps = (newX - startPos.X) / steps;
            var ysteps = (newY - startPos.Y) / steps;
            var wsteps = (newW - startPos.Width) / steps;
            var hsteps = (newH - startPos.Height) / steps;
            System.Drawing.Rectangle newPos = new System.Drawing.Rectangle(startPos.X, startPos.Y, startPos.Width, startPos.Height);
            for (var i = 0; i < steps; i++)
            {
                newPos.X += xsteps;
                newPos.Y += ysteps;
                newPos.Width += wsteps;
                newPos.Height += hsteps;
                Log.Verbose(newPos.ToString());
                WindowRectangle = newPos;
                System.Threading.Thread.Sleep((int)(runtime.TotalMilliseconds / steps));
            }

            WindowRectangle = endPos;
        }
        //public void MoveWindowTo(int newX, int newY, int newW, int newH)
        //{
        //    var startPos = WindowRectangle;
        //    var endPos = new System.Drawing.Rectangle(newX, newY, newW, newH);

        //    // Break out if there is no positional change
        //    if (startPos == endPos)
        //    {
        //        return;
        //    }
        //    var steps = 10;
        //    var xsteps = newX - startPos.X / steps;
        //    System.Drawing.Rectangle newPos = new System.Drawing.Rectangle(startPos.X, startPos.Y, startPos.Width, startPos.Height);
        //    for(var i=0; i< steps; i++)
        //    {
        //        newPos.X += xsteps;
        //        WindowRectangle = newPos;
        //        System.Threading.Thread.Sleep(100);
        //    }

        //    WindowRectangle = endPos;
        //}
        public void SetWindowSize(int Width, int Height)
        {
            WindowSize = new System.Drawing.Size(Width, Height);
        }
        public System.Drawing.Size WindowSize
        {
            get
            {
                try
                {
                    var window = GetWindow();
                    if (window == null) return System.Drawing.Size.Empty;
                    return new System.Drawing.Size(window.BoundingRectangle.Width, window.BoundingRectangle.Height);
                }
                catch (Exception)
                {

                }
                return System.Drawing.Size.Empty;
            }
            set
            {
                var window = GetWindow();
                if (window == null) return;
                if (window.Patterns.Transform.TryGetPattern(out var tranPattern))
                {
                    if (tranPattern.CanResize)
                    {
                        tranPattern.Resize(value.Width, value.Height);
                        return;
                    }
                }
                if (window.Patterns.Transform2.TryGetPattern(out var tran2Pattern))
                {
                    if (tran2Pattern.CanResize)
                    {
                        tran2Pattern.Resize(value.Width, value.Height);
                    }
                }

            }
        }
        public System.Data.DataTable AsDataTable()
        {
            var table = new System.Data.DataTable();
            DataGridView view = null;
            Grid grid = null;
            AutomationElement element = RawElement;
            do
            {
                try
                {
                    view = element.AsDataGridView();
                    grid = element.AsGrid();
                    if (view.Header == null && view.Rows.Length == 0) { view = null; grid = null; }
                    if (!element.Properties.ControlType.IsSupported) { view = null; grid = null; } else if (element.ControlType == FlaUI.Core.Definitions.ControlType.DataItem) { view = null; grid = null; }
                    if (view == null) element = element.Parent;
                }
                catch (Exception)
                {
                    return table;
                }
            } while (view == null && element != null);
            if (view == null) return table;
            if (view.Rows == null) return table;
            if (view.Header != null)
            {
                foreach (var h in view.Header.Columns)
                {
                    table.Columns.Add(h.Text, typeof(string));
                }
            }
            else
            {
                if (view.Rows.Length == 0) return table;
                DataGridViewRow row = view.Rows[0];
                foreach (var cell in row.Cells)
                {
                    if (cell.Properties.AutomationId.IsSupported && !string.IsNullOrEmpty(cell.Properties.AutomationId.ToString()))
                    {
                        table.Columns.Add(cell.AutomationId, typeof(string));
                    }
                    else if (cell.Properties.Name.IsSupported && !string.IsNullOrEmpty(cell.Properties.Name.Value))
                    {
                        table.Columns.Add(cell.Name, typeof(string));
                    }
                    else
                    {
                        table.Columns.Add("", typeof(string));
                    }
                }

            }
            foreach (var _row in view.Rows)
            {
                var objs = new List<object>();
                foreach (var cell in _row.Cells)
                {
                    try
                    {
                        if (cell.Patterns.Value.IsSupported)
                        {
                            objs.Add(cell.Value);
                        }
                        else
                        {
                            objs.Add(null);
                        }

                    }
                    catch (Exception)
                    {
                        objs.Add(null);
                    }
                }
                table.Rows.Add(objs.ToArray());
            }
            table.AcceptChanges();
            return table;
        }
    }
}
