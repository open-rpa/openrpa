using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using Newtonsoft.Json;
using FlaUI.Core.Tools;
using System.Drawing;
using System.Runtime.InteropServices;
using FlaUI.UIA3.Patterns;
using FlaUI.Core.Patterns;
using System.Threading;
using System.Windows.Documents;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using FlaUI.Core.Input;
using System.ServiceModel.Configuration;

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
                Name = "";
                if (Element.Properties.Name.IsSupported)
                {
                    string name = "";
                    Element.Properties.Name.TryGetValue(out name);
                    if (!string.IsNullOrEmpty(name)) Name = name;
                }
                ClassName = "";
                if (Element.Properties.ClassName.IsSupported)
                {
                    string classname = "";
                    Element.Properties.ClassName.TryGetValue(out classname);
                    if (!string.IsNullOrEmpty(classname)) ClassName = classname;
                }
                ClassName = "";
                if (Element.Properties.ClassName.IsSupported)
                {
                    string classname = "";
                    Element.Properties.ClassName.TryGetValue(out classname);
                    if (!string.IsNullOrEmpty(classname)) ClassName = classname;
                }
                Type = "";
                if (Element.Properties.ControlType.IsSupported)
                {
                    FlaUI.Core.Definitions.ControlType _type = FlaUI.Core.Definitions.ControlType.Unknown;
                    Element.Properties.ControlType.TryGetValue(out _type);
                    if (_type != FlaUI.Core.Definitions.ControlType.Unknown) Type = _type.ToString();
                }
                FrameworkId = "";
                if (Element.Properties.FrameworkId.IsSupported)
                {
                    string frameworkId = "";
                    Element.Properties.FrameworkId.TryGetValue(out frameworkId);
                    if (!string.IsNullOrEmpty(frameworkId)) FrameworkId = frameworkId;
                }
            }
            catch (Exception)
            {
            }
            try
            {
                FrameworkId = Element.Properties.FrameworkId.ValueOrDefault;
            }
            catch (Exception)
            {
            }
            try
            {
                _ = Rectangle;
                _ = Position;
            }
            catch (Exception)
            {
            }
        }
        public UIElement(System.Windows.Automation.AutomationElement Element)
        {
            RawElement = Element;
            ProcessId = Element.Current.ProcessId;
            Name = Element.Current.Name;
            if (string.IsNullOrEmpty(Name)) Name = "";
            ClassName = Element.Current.ClassName;
            Type = Element.Current.ControlType.ToString();
            FrameworkId = Element.Current.FrameworkId;
            _ = Rectangle;
            _ = Position;

        }
        public void Refresh()
        {
            try
            {
                int pendingCounter = 0;
                if (RawElement is AutomationElement fla)
                {
                    while (!fla.Properties.BoundingRectangle.IsSupported && pendingCounter < 6)
                    {
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(50);
                        pendingCounter++;
                    }
                    _Rectangle = Rectangle;
                    _Position = Position;
                    ProcessId = fla.Properties.ProcessId.ValueOrDefault;
                    Name = fla.Properties.Name.ValueOrDefault;
                    ClassName = fla.Properties.ClassName.ValueOrDefault;
                    Type = fla.Properties.ControlType.ValueOrDefault.ToString();
                    FrameworkId = fla.Properties.FrameworkId.ValueOrDefault;
                }
                if (RawElement is System.Windows.Automation.AutomationElement wae)
                {
                    while (wae.Current.BoundingRectangle == null && pendingCounter < 6)
                    {
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(50);
                        pendingCounter++;
                    }
                    _Rectangle = Rectangle;
                    _Position = Position;
                    ProcessId = wae.Current.ProcessId;
                    Name = wae.Current.Name;
                    ClassName = wae.Current.ClassName;
                    Type = wae.Current.ControlType.ToString();
                    FrameworkId = wae.Current.FrameworkId;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        //[JsonIgnore]
        //public AutomationElement RawElement { get; private set; }
        //object IElement.RawElement { get => RawElement; set => RawElement = value as AutomationElement; }
        public object RawElement { get; set; }
        private System.Drawing.Rectangle _Rectangle = System.Drawing.Rectangle.Empty;
        public System.Drawing.Rectangle Rectangle
        {
            get
            {
                try
                {
                    if (RawElement == null) return Rectangle.Empty;
                    if (RawElement is AutomationElement fla)
                    {
                        if (!fla.Properties.BoundingRectangle.IsSupported) return Rectangle.Empty;
                        if (_Rectangle == Rectangle.Empty)
                        {
                            _Rectangle = new Rectangle(fla.Properties.BoundingRectangle.Value.X,
                            fla.Properties.BoundingRectangle.Value.Y, fla.Properties.BoundingRectangle.Value.Width,
                            fla.Properties.BoundingRectangle.Value.Height);
                        }
                    }
                    if (RawElement is System.Windows.Automation.AutomationElement wae)
                    {
                        if (_Rectangle == Rectangle.Empty)
                        {
                            _Rectangle = new Rectangle((int)wae.Current.BoundingRectangle.X,
                            (int)wae.Current.BoundingRectangle.Y, (int)wae.Current.BoundingRectangle.Width,
                            (int)wae.Current.BoundingRectangle.Height);
                        }

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
        public string _ControlType = null;
        public string ControlType
        {
            get
            {
                if (!string.IsNullOrEmpty(_ControlType)) return _ControlType;
                if (RawElement is AutomationElement fla)
                {
                    try
                    {
                        _ControlType = fla.Properties.ControlType.ToString();
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (RawElement is System.Windows.Automation.AutomationElement wae)
                {
                    try
                    {
                        _ControlType = wae.Current.ControlType.ToString();
                    }
                    catch (Exception)
                    {
                    }
                }
                return _ControlType;
            }
        }
        public bool SupportInput
        {
            get
            {
                try
                {
                    if (ControlType == "Edit" || ControlType == "Document") return true;
                    return false;
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
                    return ControlType == "ComboBox"; // || ControlType == "RadioButton"
                }
                catch (Exception)
                {
                }
                return false;
            }
        }
        private string _ProcessName = null;
        public string ProcessName
        {
            get
            {
                if (ProcessId < 1) return "system";
                if (!string.IsNullOrEmpty(_ProcessName)) return _ProcessName;
                try
                {
                    using (var p = System.Diagnostics.Process.GetProcessById(ProcessId))
                    {
                        _ProcessName = p.ProcessName;
                        return _ProcessName;
                    }
                }
                catch (Exception)
                {
                }
                return null;
            }
        }
        [JsonIgnore]
        public UIElement Parent
        {
            get
            {
                if (RawElement is AutomationElement fla) return new UIElement(fla.Parent);
                if (RawElement is System.Windows.Automation.AutomationElement wae) return new UIElement(wae.GetParent());
                return null;
            }
        }
        public void Focus()
        {
            UntilResponsive();
            try
            {
                if (RawElement is AutomationElement fla) fla.SetForeground();
                if (RawElement is System.Windows.Automation.AutomationElement wae) wae.SetForeground();
            }
            catch
            {
            }
            try
            {
                if (RawElement is AutomationElement fla) fla.FocusNative();
                if (RawElement is System.Windows.Automation.AutomationElement wae) wae.FocusNative();
            }
            catch
            {
            }
            try
            {
                if (RawElement is AutomationElement fla) fla.Focus();
                if (RawElement is System.Windows.Automation.AutomationElement wae) wae.SetFocus();
            }
            catch
            {
            }
        }
        private void UntilResponsive()
        {
            if (ProcessId <= 0) return;
            using (var process = System.Diagnostics.Process.GetProcessById(ProcessId))
                while (!process.Responding) { }
        }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY, bool DoubleClick, bool AnimateMouse)
        {
            try
            {
                if (Button != Input.MouseButton.Left) { VirtualClick = false; }
                if (VirtualClick)
                {
                    if (RawElement is AutomationElement fla)
                    {
                        if (fla.Patterns.Invoke.IsSupported)
                            if (fla.Patterns.Invoke.TryGetPattern(out var InvokePattern))
                            {
                                if (fla.IsEnabled)
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
                    if (RawElement is System.Windows.Automation.AutomationElement wae)
                    {
                        var InvokePattern = wae.GetSpecifiedPattern<System.Windows.Automation.InvokePattern>();
                        if (InvokePattern != null)
                        {
                            if (wae.Current.IsEnabled)
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
                        }
                    }
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
                        Input.InputDriver.Instance.AnimateMouseMove(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                    }
                    else
                    {
                        Input.InputDriver.Instance.MouseMove(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
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
        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("User32.dll")]
        public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public Task _Highlight(System.Drawing.Color Color, TimeSpan Duration)
        {
            System.Threading.Thread.CurrentThread.Name = "UIHighlighter";
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
            {
                _overlayWindow.BackColor = Color;
                _overlayWindow.Visible = true;
                //_overlayWindow.SetTimeout(Duration);
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
            //var r = new Rectangle(Rectangle.Location, Rectangle.Size);
            //IntPtr desktopPtr = GetDC(IntPtr.Zero);
            //IntPtr hWnd = rectutil.WindowFromPoint(r.Location);
            //try
            //{
            //    using (Graphics g = Graphics.FromHdc(desktopPtr))
            //    {
            //        SolidBrush b = new SolidBrush(Color);
            //        Pen p = new Pen(Color, 5);
            //        var sw = new System.Diagnostics.Stopwatch();
            //        sw.Start();
            //        do
            //        {
            //            g.DrawRectangle(p, r);
            //            // System.Threading.Thread.Sleep(1);
            //            // await Task.Delay(1);
            //            // g.DrawEllipse(p, r);
            //            // g.FillRectangle(b, r);
            //        } while (sw.Elapsed < Duration);
            //        sw.Stop();
            //        // rectutil.InvalidateRect(IntPtr.Zero, r, true);
            //        // await Task.Delay(Duration);
            //    }
            //    // rectutil.InvalidateRect(hWnd, IntPtr.Zero, true);
            //    rectutil.InvalidateRect(IntPtr.Zero, r, true);
            //}
            //finally
            //{
            //    ReleaseDC(IntPtr.Zero, desktopPtr);
            //}
        }
        public string Value
        {
            get
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                try
                {
                    if (RawElement is AutomationElement fla)
                    {
                        Log.Selector(string.Format("UIElement.Value.get::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
                        if (fla.Properties.IsPassword.TryGetValue(out var isPassword) && isPassword)
                        {
                            throw new FlaUI.Core.Exceptions.MethodNotSupportedException($"Text from element '{ToString()}' cannot be retrieved because it is set as password.");
                        }
                        if (fla.Patterns.Value.TryGetPattern(out var valuePattern) &&
                            valuePattern.Value.TryGetValue(out var value))
                        {
                            Log.Selector(string.Format("UIElement.Value.get::valuePattern::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                            return value;
                        }
                        if (fla.Patterns.Text.TryGetPattern(out var textPattern))
                        {
                            Log.Selector(string.Format("UIElement.Value.get::textPattern::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                            return textPattern.DocumentRange.GetText(Int32.MaxValue);
                        }
                        if (fla.ControlType == FlaUI.Core.Definitions.ControlType.List)
                        {
                            var combo = fla.AsListBox();
                            if (combo.SelectedItem != null)
                            {
                                return combo.SelectedItem.Name;
                            }
                        }
                        if (fla.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox)
                        {
                            var combo = fla.AsCheckBox();
                            if (combo.IsChecked.HasValue && combo.IsChecked.Value)
                            {
                                return "true";
                            }
                            return "false";
                        }
                    }
                    else if (RawElement is System.Windows.Automation.AutomationElement wae)
                    {
                        if (wae.Current.IsPassword)
                        {
                            throw new FlaUI.Core.Exceptions.MethodNotSupportedException($"Text from element '{ToString()}' cannot be retrieved because it is set as password.");
                        }
                        var valuePattern = wae.GetSpecifiedPattern<System.Windows.Automation.ValuePattern>();
                        if (valuePattern != null)
                        {
                            return valuePattern.Current.Value;
                        }
                        var textPattern = wae.GetSpecifiedPattern<System.Windows.Automation.TextPattern>();
                        if (textPattern != null)
                        {
                            return textPattern.DocumentRange.GetText(Int32.MaxValue);
                        }
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
                if (RawElement is AutomationElement fla)
                {
                    if (fla.Patterns.LegacyIAccessible.TryGetPattern(out var LegacyPattern))
                    {
                        LegacyPattern.SetValue(value);
                        Log.Selector(string.Format("UIElement.LegacyIAccessible.set::SetValue::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                    }
                    else if (fla.Patterns.Value.TryGetPattern(out var valuePattern))
                    {
                        valuePattern.SetValue(value);
                        Log.Selector(string.Format("UIElement.Value.set::SetValue::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                    }
                    else
                    {
                        Enter(value);
                        Log.Selector(string.Format("UIElement.Value.set::Enter::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                    }
                    if (fla.ControlType == FlaUI.Core.Definitions.ControlType.List)
                    {
                        var combo = fla.AsListBox();
                        combo.Select(value);
                    }
                    if (fla.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox)
                    {
                        var combo = fla.AsCheckBox();
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
                else if (RawElement is System.Windows.Automation.AutomationElement wae)
                {
                    var valuePattern = wae.GetSpecifiedPattern<System.Windows.Automation.ValuePattern>();
                    if (valuePattern != null)
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
        }
        public bool IsChecked
        {
            get
            {
                if (RawElement is AutomationElement fla)
                {
                    if (fla.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox)
                    {
                        var combo = fla.AsCheckBox();
                        if (combo.IsChecked.HasValue && combo.IsChecked.Value)
                        {
                            return true;
                        }
                    }
                    else if (fla.ControlType == FlaUI.Core.Definitions.ControlType.RadioButton)
                    {
                        var radio = fla.AsRadioButton();
                        if (radio.IsChecked) return true;
                    }
                }
                return false;
            }
            set
            {
                if (RawElement is AutomationElement fla)
                {
                    if (fla.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox)
                    {
                        var combo = fla.AsCheckBox();
                        combo.IsChecked = value;
                    }
                    else if (fla.ControlType == FlaUI.Core.Definitions.ControlType.RadioButton)
                    {
                        var radio = fla.AsRadioButton();
                        radio.IsChecked = value;
                    }
                }
            }
        }
        public void SelectItem(UIElement element)
        {
            if (RawElement is AutomationElement fla)
            {
                if (fla.ControlType == FlaUI.Core.Definitions.ControlType.List)
                {
                    var combo = fla.AsListBox();
                    for (var i = 0; i < combo.Items.Length; i++)
                    {
                        if (combo.Items[i].Name == element.Value)
                        {
                            combo.AddToSelection(i);
                        }

                    }
                }
            }
        }
        public int SelectedIndex
        {
            set
            {
                if (RawElement is AutomationElement fla)
                {
                    if (fla.ControlType == FlaUI.Core.Definitions.ControlType.List)
                    {
                        var combo = fla.AsListBox();
                        combo.Select(value);
                    }
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
            Focus();
            if (RawElement is AutomationElement fla)
            {
                var valuePattern = fla.Patterns.Value.PatternOrDefault;
                valuePattern?.SetValue(String.Empty);
                if (string.IsNullOrEmpty(value)) return;
            }
            if (RawElement is System.Windows.Automation.AutomationElement wae)
            {
                var valuePattern = wae.GetSpecifiedPattern<System.Windows.Automation.ValuePattern>();
                valuePattern?.SetValue(String.Empty);
                if (string.IsNullOrEmpty(value)) return;
            }
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
                if (RawElement is AutomationElement fla)
                {
                    if (fla.ControlType == FlaUI.Core.Definitions.ControlType.ComboBox)
                    {
                        var combo = fla.AsComboBox();
                        foreach (var c in combo.Items) result.Add(new UIElement(c));
                    }
                    else if (fla.ControlType == FlaUI.Core.Definitions.ControlType.List)
                    {
                        var combo = fla.AsListBox();
                        foreach (var c in combo.Items)
                        {
                            var tt = c.AsListBoxItem();
                            result.Add(new UIElement(tt));
                        }
                    }
                }
                if (RawElement is System.Windows.Automation.AutomationElement wae)
                {
                    var listItems = wae.FindAll(System.Windows.Automation.TreeScope.Children,
                        new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.ListItem)
                        );
                    foreach (System.Windows.Automation.AutomationElement c in listItems)
                    {
                        result.Add(new UIElement(c));
                    }
                }
                return result.ToArray();
            }
        }
        public T GetWindow<T>()
        {
            if (RawElement is AutomationElement fla)
            {
                AutomationElement last = fla;
                AutomationElement current = fla;
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
                if (window is T w) return w;
                return default;
            }
            if (RawElement is System.Windows.Automation.AutomationElement wae)
            {
                System.Windows.Automation.AutomationElement last = wae;
                System.Windows.Automation.AutomationElement current = wae;
                do
                {
                    try
                    {
                        last = current;
                        current = current.GetParent();
                    }
                    catch (Exception)
                    {
                        // throw;
                    }
                } while (current != null && current.GetParent() != null);
                if (last is T w) return w;
                return default;
            }
            return default;
        }
        public void SetPosition(int X, int Y)
        {
            if (RawElement is AutomationElement fla)
            {
                Window window = GetWindow<Window>();
                if (window == null) return;
                var size = WindowSize;
                NativeMethods.MoveWindow(window.Properties.NativeWindowHandle.Value, X, Y, size.Width, size.Height, true);
            }
            if (RawElement is System.Windows.Automation.AutomationElement wae)
            {
                var window = GetWindow<System.Windows.Automation.AutomationElement>();
                if (window == null) return;
                var size = WindowSize;
                NativeMethods.MoveWindow(new IntPtr(window.Current.NativeWindowHandle), X, Y, size.Width, size.Height, true);
            }
        }
        public void UntilResponsive(TimeSpan timeout)
        {
            if (RawElement is AutomationElement fla)
            {
                Wait.UntilResponsive(fla, timeout);
            }
        }
        public void Close()
        {
            if (RawElement is AutomationElement fla)
            {
                Window window = GetWindow<Window>();
                if (window == null) return;
                window.Close();
            }
            if (RawElement is System.Windows.Automation.AutomationElement wae)
            {
                var window = GetWindow<System.Windows.Automation.AutomationElement>();
                if (window == null) return;
                var TitleBar = window.FindFirst(System.Windows.Automation.TreeScope.Children,
                    new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.TitleBar));
                if (TitleBar != null)
                {
                    var CloseButton = window.FindFirst(System.Windows.Automation.TreeScope.Children,
                        new System.Windows.Automation.AndCondition(
                            new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Button),
                            new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.NameProperty, "Close")
                            )
                        );
                    if (CloseButton != null)
                    {
                        var invoke = CloseButton.GetSpecifiedPattern<System.Windows.Automation.InvokePattern>();
                        if (invoke != null)
                        {
                            invoke.Invoke();
                            return;
                        }
                    }
                }
                var windowpatt = window.GetSpecifiedPattern<System.Windows.Automation.WindowPattern>();
                if (windowpatt != null)
                {
                    windowpatt.Close();
                    return;
                }
            }
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
                    if (RawElement is AutomationElement fla)
                    {
                        _Position = new System.Drawing.Point(fla.BoundingRectangle.X, fla.BoundingRectangle.Y);
                    }
                    else if (RawElement is System.Windows.Automation.AutomationElement wae)
                    {
                        _Position = new System.Drawing.Point((int)wae.Current.BoundingRectangle.X, (int)wae.Current.BoundingRectangle.Y);
                    }

                }
                return _Position;
            }
            set
            {
                if (RawElement is AutomationElement fla)
                {
                    if (fla.Patterns.Transform.TryGetPattern(out var tranPattern))
                    {
                        if (tranPattern.CanMove)
                        {
                            tranPattern.Move(value.X, value.Y);
                        }
                    }
                    if (fla.Patterns.Transform2.TryGetPattern(out var tran2Pattern))
                    {
                        if (tran2Pattern.CanMove)
                        {
                            tran2Pattern.Move(value.X, value.Y);
                        }
                    }
                }
                else if (RawElement is System.Windows.Automation.AutomationElement wae)
                {
                    var patt = wae.GetSpecifiedPattern<System.Windows.Automation.TransformPattern>();
                    if (patt != null && patt.Current.CanMove)
                    {
                        patt.Move(value.X, value.Y);
                    }
                }
            }
        }
        System.Drawing.Point WindowPosition
        {
            get
            {
                if (RawElement is AutomationElement)
                {
                    var window = GetWindow<Window>();
                    return new Point(window.BoundingRectangle.X, window.BoundingRectangle.Y);
                }
                if (RawElement is System.Windows.Automation.AutomationElement)
                {
                    var window = GetWindow<System.Windows.Automation.AutomationElement>();
                    return new Point((int)window.Current.BoundingRectangle.X, (int)window.Current.BoundingRectangle.Y);
                }
                return Point.Empty;
            }
            set
            {
                SetPosition(value.X, value.Y);
            }
        }
        public Rectangle WindowRectangle
        {
            get
            {
                try
                {
                    if (RawElement is AutomationElement)
                    {
                        var window = GetWindow<Window>();
                        return new Rectangle((int)window.Properties.BoundingRectangle.Value.X,
                            (int)window.Properties.BoundingRectangle.Value.Y, (int)window.Properties.BoundingRectangle.Value.Width,
                            (int)window.Properties.BoundingRectangle.Value.Height);

                    }
                    if (RawElement is System.Windows.Automation.AutomationElement)
                    {
                        var window = GetWindow<System.Windows.Automation.AutomationElement>();
                        return new Rectangle((int)window.Current.BoundingRectangle.X,
                            (int)window.Current.BoundingRectangle.Y, (int)window.Current.BoundingRectangle.Width,
                            (int)window.Current.BoundingRectangle.Height);
                    }
                }
                catch (Exception)
                {
                }
                return Rectangle.Empty;
            }
            set
            {
                if (RawElement is AutomationElement)
                {
                    var window = GetWindow<Window>();
                    NativeMethods.MoveWindow(window.Properties.NativeWindowHandle.Value, value.X, value.Y, value.Width, value.Height, true);
                }
                if (RawElement is System.Windows.Automation.AutomationElement)
                {
                    var window = GetWindow<System.Windows.Automation.AutomationElement>();
                    NativeMethods.MoveWindow(new IntPtr(window.Current.NativeWindowHandle), value.X, value.Y, value.Width, value.Height, true);
                }
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
                    if (RawElement is AutomationElement)
                    {
                        var window = GetWindow<Window>();
                        if (window == null) return System.Drawing.Size.Empty;
                        return new System.Drawing.Size(window.BoundingRectangle.Width, window.BoundingRectangle.Height);
                    }
                    if (RawElement is System.Windows.Automation.AutomationElement)
                    {
                        var window = GetWindow<System.Windows.Automation.AutomationElement>();
                        if (window == null) return System.Drawing.Size.Empty;
                        return new System.Drawing.Size((int)window.Current.BoundingRectangle.Width, (int)window.Current.BoundingRectangle.Height);
                    }
                }
                catch (Exception)
                {

                }
                return System.Drawing.Size.Empty;
            }
            set
            {
                if (RawElement is AutomationElement)
                {
                    var window = GetWindow<Window>();
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
                if (RawElement is System.Windows.Automation.AutomationElement)
                {
                    var window = GetWindow<System.Windows.Automation.AutomationElement>();
                    if (window == null) return;
                    var tranPattern = window.GetSpecifiedPattern<System.Windows.Automation.TransformPattern>();
                    if (tranPattern != null && tranPattern.Current.CanResize)
                    {
                        tranPattern.Resize(value.Width, value.Height);
                    }
                }
            }
        }
        public System.Data.DataTable AsDataTable()
        {
            var table = new System.Data.DataTable();
            if (RawElement is AutomationElement fla)
            {
                DataGridView view = null;
                Grid grid = null;
                AutomationElement element = fla;
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
            }
            return table;
        }
    }
}
