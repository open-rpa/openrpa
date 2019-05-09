using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using Serilog;

namespace OpenRPA
{
    public class UIElement : IElement
    {
        public UIElement(AutomationElement Element)
        {
            rawElement = Element;
            ProcessId = Element.Properties.ProcessId.ValueOrDefault;
            Id = Element.Properties.AutomationId.ValueOrDefault;
            Name = Element.Properties.Name.ValueOrDefault;
            ClassName = Element.Properties.ClassName.ValueOrDefault;
            Type = Element.Properties.ControlType.ValueOrDefault.ToString();
        }
        public void Refresh()
        {
            try
            {
                int pendingCounter = 0;
                while (!rawElement.Properties.BoundingRectangle.IsSupported && pendingCounter < 6)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(50);
                    pendingCounter++;
                }
                ProcessId = rawElement.Properties.ProcessId.ValueOrDefault;
                Id = rawElement.Properties.AutomationId.ValueOrDefault;
                Name = rawElement.Properties.Name.ValueOrDefault;
                ClassName = rawElement.Properties.ClassName.ValueOrDefault;
                Type = rawElement.Properties.ControlType.ValueOrDefault.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
        }

        public AutomationElement rawElement { get; private set; }
        public int ProcessId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public string Type { get; set; }
        public string Path => string.Format("{0}/{1}", Parent == null ? string.Empty : Parent.Path, Type);

        public string ControlType
        {
            get
            {
                try
                {
                    return rawElement.Properties.ControlType.ToString();
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
                    return rawElement.ControlType == FlaUI.Core.Definitions.ControlType.Edit
                        || rawElement.ControlType == FlaUI.Core.Definitions.ControlType.Document;
                }
                catch (Exception)
                {
                }
                return false;
            }
        }

        public UIElement Parent
        {
            get
            {
                //if (TreeWalker.RawViewWalker.GetParent(rawElement) is AutomationElement rawParent)
                //{
                //    return new UIElement(rawParent);
                //}
                return new UIElement(rawElement.Parent);
                //return null;
            }
        }
        public void Focus()
        {
            UntilResponsive();
            try
            {
                //rawElement.SetFocus();
                rawElement.Focus();
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

        public void Click()
        {
            try
            {
                if (rawElement.Patterns.Invoke.IsSupported)
                {
                    var invokePattern = rawElement.Patterns.Invoke.Pattern;
                    invokePattern.Invoke();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string Value
        {
            get
            {
                if (rawElement.Properties.IsPassword.TryGetValue(out var isPassword) && isPassword)
                {
                    throw new FlaUI.Core.Exceptions.MethodNotSupportedException($"Text from element '{ToString()}' cannot be retrieved because it is set as password.");
                }
                if (rawElement.Patterns.Value.TryGetPattern(out var valuePattern) &&
                    valuePattern.Value.TryGetValue(out var value))
                {
                    return value;
                }
                if (rawElement.Patterns.Text.TryGetPattern(out var textPattern))
                {
                    return textPattern.DocumentRange.GetText(Int32.MaxValue);
                }
                throw new FlaUI.Core.Exceptions.MethodNotSupportedException($"AutomationElement '{ToString()}' supports neither ValuePattern or TextPattern");
            }
            set
            {
                if (rawElement.Patterns.Value.TryGetPattern(out var valuePattern))
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
            rawElement.Focus();
            var valuePattern = rawElement.Patterns.Value.PatternOrDefault;
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
            return "id:" + Id + " Name:" + Name + " ClassName: " + ClassName;
        }
        public override bool Equals(object obj)
        {
            var e = obj as UIElement;
            if (e == null) return false;
            if (e.ProcessId != ProcessId) return false;
            if (e.Id != Id) return false;
            if (e.Name != Name) return false;
            if (e.ClassName != ClassName) return false;
            if (e.Type != Type) return false;
            return true;
        }
    }
}
