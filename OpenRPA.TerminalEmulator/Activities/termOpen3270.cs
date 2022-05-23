using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Documents;
using OpenRPA.Interfaces;
using System.Drawing;
using OpenRPA.Interfaces.VT;

namespace OpenRPA.TerminalEmulator
{
    public class termOpen3270Config : ITerminalConfig
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string TermType { get; set; }
        public bool UseSSL { get; set; }
    }
    public class termOpen3270 : ITerminal
    {
        public event EventHandler CursorPositionSet;
        Open3270.TNEmulator emu = new Open3270.TNEmulator();
        bool isConnected;
        bool isConnecting;
        public termOpen3270()
        {
            emu.CursorLocationChanged += Emu_CursorLocationChanged;
            emu.Disconnected += Emu_Disconnected;
        }
        private static object _lock = new object();
        private void Emu_Disconnected(Open3270.TNEmulator where, string Reason)
        {
            IsConnected = false;
            IsConnecting = false;
            NotifyPropertyChanged("Connect");
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            GenericTools.RunUI(Redraw);
        }
        private void Emu_CursorLocationChanged(object sender, EventArgs e)
        {
            CursorX = emu.CursorX;
            CursorY = emu.CursorY;
            CursorPositionSet?.Invoke(this, new EventArgs());
        }
        public void Connect(ITerminalConfig config)
        {
            emu.Config.FastScreenMode = true;
            emu.Config.HostName = config.Hostname;
            emu.Config.HostPort = config.Port;
            emu.Config.TermType = config.TermType;
            emu.Config.UseSSL = config.UseSSL;
            emu.Config.ThrowExceptionOnLockedScreen = false;
            IsConnecting = true;
            Task.Factory.StartNew(ConnectToHost).ContinueWith((t) =>
            {
                IsConnecting = false;
                IsConnected = emu.IsConnected;
                var ScreenText = emu.CurrentScreenXML?.Dump();
            });
        }
        public void Close()
        {
            emu.Close();
            IsConnected = false;
            IsConnecting = false;
        }
        private void ConnectToHost()
        {
            emu.Connect();
            // emu.Refresh(true, 1000);
            isConnected = emu.IsConnected;
            Refresh(1000);
        }
        public bool IsConnecting
        {
            get
            {
                return isConnecting;
            }
            set
            {
                isConnecting = value;
                NotifyPropertyChanged("IsConnecting");
            }
        }
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                isConnected = value;
                NotifyPropertyChanged("IsConnected");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            GenericTools.RunUI(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
        public Open3270.TN3270.XMLScreenField[] _Fields;
        public Open3270.TN3270.XMLScreenField[] Fields
        {
            get
            {
                if (_Fields != null) return _Fields;
                lock (_lock)
                {
                    if (emu.CurrentScreenXML?.Fields == null) return new Open3270.TN3270.XMLScreenField[] { };
                    _Fields = emu.CurrentScreenXML.Fields.Where(x => !x.Attributes.Protected).ToArray();
                }
                return _Fields;
            }
        }
        public Open3270.TN3270.XMLScreenField[] _Strings;
        public Open3270.TN3270.XMLScreenField[] Strings
        {
            get
            {
                if (_Strings != null) return _Strings;
                lock (_lock)
                {
                    if (emu.CurrentScreenXML?.Fields == null) return new Open3270.TN3270.XMLScreenField[] { };
                    _Strings = emu.CurrentScreenXML.Fields.Where(x => x.Attributes.Protected).ToArray();
                }
                return _Strings;
            }
        }
        public void SendText(string text)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {
                Log.Debug("SetText::begin");
                lock (_lock) emu.SetText(text);
                Log.Verbose("SetText::end");
                Refresh();
                // Redraw();
                // CursorPositionSet?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return;
            }
            finally
            {
                Log.Verbose(string.Format("SendText::END {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
        }
        public void SendText(int field, string text)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {

                var index = -1;
                for (var i = 0; i < Fields.Length; i++)
                {
                    if (!Fields[i].Attributes.Protected) index++;

                    if (i == field) break;
                }
                Fields[field].Text = text;
                Log.Verbose("SetField::begin");
                Log.Debug("SendText " + text + " to field #" + field);
                lock (_lock) emu.SetField(index, text);
                Log.Verbose("SetField::end");
                Refresh();
                // Redraw();
                // CursorPositionSet?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return;
            }
            finally
            {
                Log.Verbose(string.Format("SendText::END {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
        }
        public void SendKey(System.Windows.Input.Key key)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {
                // if (key != System.Windows.Input.Key.Enter && key != System.Windows.Input.Key.Tab) return;
                if (key == System.Windows.Input.Key.Back)
                {
                    var bindex = GetFieldByLocation(CursorX, CursorY);
                    if (bindex == -1) return;
                    var _f = GetField(bindex);
                    if (_f == null) return;
                    string text = _f.Text;
                    if (string.IsNullOrEmpty(_f.Text))
                    {
                        var sindex = GetStringByLocation(CursorX, CursorY);
                        if (sindex != -1)
                        {
                            var _sf = GetString(sindex);
                            text = _sf?.Text;
                        }
                    }
                    if (!string.IsNullOrEmpty(text)) text = text.Substring(0, text.Length - 1);
                    text = text.PadRight(_f.Location.Length);
                    Log.Debug("Send '" + text + "' to field #" + bindex);
                    SendText(bindex, text);
                    Refresh();
                    return;
                }
                Log.Verbose("SendKey::begin");
                lock (_lock)
                    switch (key)
                    {
                        case System.Windows.Input.Key.Back: emu.SendKey(true, Open3270.TnKey.Backspace, 2000); break;
                        case System.Windows.Input.Key.Delete: emu.SendKey(true, Open3270.TnKey.Delete, 2000); break;
                        case System.Windows.Input.Key.Down: emu.SendKey(true, Open3270.TnKey.Down, 2000); break;
                        case System.Windows.Input.Key.Enter: emu.SendKey(true, Open3270.TnKey.Enter, 2000); break;
                        case System.Windows.Input.Key.F1: emu.SendKey(true, Open3270.TnKey.F1, 2000); break;
                        case System.Windows.Input.Key.F2: emu.SendKey(true, Open3270.TnKey.F2, 2000); break;
                        case System.Windows.Input.Key.F3: emu.SendKey(true, Open3270.TnKey.F3, 2000); break;
                        case System.Windows.Input.Key.F4: emu.SendKey(true, Open3270.TnKey.F4, 2000); break;
                        case System.Windows.Input.Key.F5: emu.SendKey(true, Open3270.TnKey.F5, 2000); break;
                        case System.Windows.Input.Key.F6: emu.SendKey(true, Open3270.TnKey.F6, 2000); break;
                        case System.Windows.Input.Key.F7: emu.SendKey(true, Open3270.TnKey.F7, 2000); break;
                        case System.Windows.Input.Key.F8: emu.SendKey(true, Open3270.TnKey.F8, 2000); break;
                        case System.Windows.Input.Key.F9: emu.SendKey(true, Open3270.TnKey.F9, 2000); break;
                        case System.Windows.Input.Key.F10: emu.SendKey(true, Open3270.TnKey.F10, 2000); break;
                        case System.Windows.Input.Key.F11: emu.SendKey(true, Open3270.TnKey.F11, 2000); break;
                        case System.Windows.Input.Key.F12: emu.SendKey(true, Open3270.TnKey.F12, 2000); break;
                        case System.Windows.Input.Key.F13: emu.SendKey(true, Open3270.TnKey.F13, 2000); break;
                        case System.Windows.Input.Key.F14: emu.SendKey(true, Open3270.TnKey.F14, 2000); break;
                        case System.Windows.Input.Key.F15: emu.SendKey(true, Open3270.TnKey.F15, 2000); break;
                        case System.Windows.Input.Key.F16: emu.SendKey(true, Open3270.TnKey.F16, 2000); break;
                        case System.Windows.Input.Key.F17: emu.SendKey(true, Open3270.TnKey.F17, 2000); break;
                        case System.Windows.Input.Key.F18: emu.SendKey(true, Open3270.TnKey.F18, 2000); break;
                        case System.Windows.Input.Key.F19: emu.SendKey(true, Open3270.TnKey.F19, 2000); break;
                        case System.Windows.Input.Key.F20: emu.SendKey(true, Open3270.TnKey.F20, 2000); break;
                        case System.Windows.Input.Key.F21: emu.SendKey(true, Open3270.TnKey.F21, 2000); break;
                        case System.Windows.Input.Key.F22: emu.SendKey(true, Open3270.TnKey.F22, 2000); break;
                        case System.Windows.Input.Key.F23: emu.SendKey(true, Open3270.TnKey.F23, 2000); break;
                        case System.Windows.Input.Key.F24: emu.SendKey(true, Open3270.TnKey.F24, 2000); break;
                        case System.Windows.Input.Key.Home: emu.SendKey(true, Open3270.TnKey.Home, 2000); break;
                        case System.Windows.Input.Key.Insert: emu.SendKey(true, Open3270.TnKey.Insert, 2000); break;
                        case System.Windows.Input.Key.Left: emu.SendKey(true, Open3270.TnKey.Left, 2000); break;
                        case System.Windows.Input.Key.Right: emu.SendKey(true, Open3270.TnKey.Right, 2000); break;
                        case System.Windows.Input.Key.Tab: emu.SendKey(true, Open3270.TnKey.Tab, 2000); break;
                        case System.Windows.Input.Key.Up: emu.SendKey(true, Open3270.TnKey.Up, 2000); break;
                        default: return;
                    }
                Log.Verbose("SendKey::end");
                Log.Debug("SendKey " + key);
                // if (key != System.Windows.Input.Key.Tab) Refresh();
                Refresh();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return;
            }
            finally
            {
                Log.Verbose(string.Format("SendKey::END {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
        }
        public void Refresh()
        {
            Refresh(500);
        }
        public void Refresh(int screenCheckInterval)
        {
            Log.Verbose("Refresh::BEGIN");
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            //This line keeps checking to see when we've received a valid screen of data from the mainframe.
            //It loops until the TNEmulator.Refresh() method indicates that waiting for the screen did not time out.
            //This helps prevent blank screens, etc.
            lock (_lock)
            {
                while (!emu.Refresh(false, screenCheckInterval)) { }
            }
            _Fields = null;
            _Strings = null;
            Log.Verbose(string.Format("Refresh::END {0:mm\\:ss\\.fff}", sw.Elapsed));
            Redraw();
            CursorPositionSet?.Invoke(this, new EventArgs());
        }
        public bool WaitForText(string Text, TimeSpan Timeout)
        {
            try
            {
                return emu.WaitForTextOnScreen((int)Timeout.TotalMilliseconds, Text) > -1;
            }
            finally
            {
                GenericTools.RunUI(Refresh);
            }
        }
        public bool WaitForKeyboardUnlocked(TimeSpan Timeout)
        {
            emu.WaitTillKeyboardUnlocked((int)Timeout.TotalMilliseconds);
            return emu.KeyboardLocked != 0;
        }
        private int _CursorY;
        public int CursorY
        {
            get
            {
                return _CursorY;
            }
            set
            {
                _CursorY = value;
                NotifyPropertyChanged("CursorY");
                var findex = GetFieldByLocation(CursorX, CursorY);
                if (findex > -1 && findex != _CurrentField) CurrentField = findex;
            }
        }
        private int _CursorX;
        public int CursorX
        {
            get
            {
                return _CursorX;
            }
            set
            {
                _CursorX = value;
                NotifyPropertyChanged("CursorX");
                var findex = GetFieldByLocation(CursorX, CursorY);
                if (findex > -1 && findex != _CurrentField) CurrentField = findex;

            }
        }
        private int _CurrentField;
        public int CurrentField
        {
            get
            {
                return _CurrentField;
            }
            set
            {
                _CurrentField = value;
                NotifyPropertyChanged("CurrentField");
            }
        }
        public int HighlightCursorY { get; set; }
        public int HighlightCursorX { get; set; }
        //public Xceed.Wpf.Toolkit.RichTextBox rtb { get; set; }
        public System.Windows.Controls.RichTextBox rtb { get; set; }
        public void UpdateCaret()
        {
            GenericTools.RunUI(() =>
            {
                try
                {
                    var _p = rtb.Document.Blocks.ElementAt(CursorY);
                    if (_p == null) return;
                    TextPointer myTextPointer1 = _p.ContentStart.GetPositionAtOffset(CursorX);
                    TextPointer myTextPointer2 = _p.ContentStart.GetPositionAtOffset(CursorX + 2);
                    if (myTextPointer2 == null) return;
                    // rtb.CaretPosition = myTextPointer2;
                    // Redraw();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return;
                }
            });
        }
        public void Redraw()
        {
            if (!IsConnected)
            {
                Log.Verbose("");
            }
                var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Verbose("Redraw::BEGIN");
            GenericTools.RunUI(() =>
            {
                try
                {
                    bool foundHighlight = false;
                    bool HighlightEdit = false;
                    if (rtb.Document == null) return;
                    rtb.Document.Blocks.Clear();
                    //var noSpaceStyle = new System.Windows.Style(typeof(Paragraph));
                    //noSpaceStyle.Setters.Add(new System.Windows.Setter(Paragraph.MarginProperty, new System.Windows.Thickness(0)));
                    //rtb.Resources.Add(typeof(Paragraph), noSpaceStyle);

                    for (var y = 1; y <= 25; y++)
                    {
                        var p = new Paragraph();
                        p.LineHeight = 1;
                        p.Margin = new System.Windows.Thickness(0);
                        p.FontStretch = System.Windows.FontStretches.UltraExpanded;
                        p.FontSize = 16;
                        var text = "";
                        var fields = new List<Open3270.TN3270.XMLScreenField>();
                        fields.AddRange(Strings.Where(l => l.Location.top == y));
                        fields.AddRange(Fields.Where(l => l.Location.top == y));
                        fields = fields.OrderBy(x => x.Location.left).ToList();
                        foreach (var field in fields)
                        {
                            if (field.Location.left > (text.Length + 1))
                            {
                                var subtext = string.Format("{0," + (field.Location.left - text.Length) + "}", "");
                                var subr = new Run(subtext);
                                // subr.FontStretch = System.Windows.FontStretch.FromOpenTypeStretch(9);
                                subr.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                                p.Inlines.Add(subr);
                                text += subtext;
                            }
                            if (string.IsNullOrEmpty(field.Text)) field.Text = "";
                            var newtext = field.Text.PadRight(field.Location.length);
                            // var newtext = string.Format("{0," + field.Location.Length + "}", field.Text);
                            if (text.Length > field.Location.left)
                            {
                                continue;
                            }
                            text += newtext;
                            var r = new Run(newtext);
                            r.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                            if (string.IsNullOrEmpty(field.Attributes.Foreground)) field.Attributes.Foreground = "Green";
                            if (string.IsNullOrEmpty(field.Attributes.Background)) field.Attributes.Background = "Background";
                            // Color clr = Color.FromName(field.Attributes.Foreground);
                            Color clr = Color.LimeGreen;
                            if (field.Attributes.Foreground == "Background") clr = Color.Black;
                            if (field.Attributes.Foreground == "Green") clr = Color.LimeGreen;
                            if (field.Attributes.Foreground == "Turquoise") clr = Color.Cyan;
                            if (field.Attributes.Foreground == "Pink") clr = Color.Fuchsia;
                            if (field.Attributes.Foreground == "Blue") clr = Color.CornflowerBlue;
                            r.Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(clr.R, clr.G, clr.B));

                            // Color bck = Color.FromName(field.Attributes.Background);
                            Color bck = Color.Black;
                            if (field.Attributes.Background == "Background") bck = Color.Black;
                            if (field.Attributes.Background == "Green") bck = Color.LimeGreen;
                            if (field.Attributes.Background == "Turquoise") bck = Color.Cyan;
                            if (field.Attributes.Background == "Pink") bck = Color.Fuchsia;
                            if (field.Attributes.Background == "Blue") bck = Color.CornflowerBlue;

                            r.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(bck.R, bck.G, bck.B));

                            p.Inlines.Add(r);
                            if (field.Location.top == HighlightCursorY)
                            {
                                if (HighlightCursorX >= field.Location.left && HighlightCursorX <= (field.Location.left + field.Location.length))
                                {
                                    if (!foundHighlight || (!HighlightEdit && !field.Attributes.Protected))
                                    {
                                        foundHighlight = true;
                                        HighlightEdit = !field.Attributes.Protected;
                                        bck = Color.Red;
                                        clr = Color.Yellow;
                                        if (!field.Attributes.Protected)
                                        {
                                            bck = Color.Green;
                                            clr = Color.Yellow;
                                        }
                                        r.Background = new System.Windows.Media.SolidColorBrush(
                                            System.Windows.Media.Color.FromRgb(bck.R, bck.G, bck.B));
                                        r.Foreground = new System.Windows.Media.SolidColorBrush(
                                            System.Windows.Media.Color.FromRgb(clr.R, clr.G, clr.B));

                                    }
                                }
                            }
                            if (!IsConnected)
                            {
                                bck = Color.Black;
                                clr = Color.Gray;
                                r.Background = new System.Windows.Media.SolidColorBrush(
                                    System.Windows.Media.Color.FromRgb(bck.R, bck.G, bck.B));
                                r.Foreground = new System.Windows.Media.SolidColorBrush(
                                    System.Windows.Media.Color.FromRgb(clr.R, clr.G, clr.B));
                            }
                        }
                        rtb.Document.Blocks.Add(p);
                    }
                    CursorPositionSet?.Invoke(this, new EventArgs());
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return;
                }
                finally
                {
                    Log.Verbose(string.Format("Redraw::END {0:mm\\:ss\\.fff}", sw.Elapsed));
                }
            });
        }
        public class Location : ILocation
        {
            public int Position { get; set; }
            public int Column { get; set; }
            public int Row { get; set; }
            public int Length { get; set; }
        }
        public class Field : IField
        {
            public Field() { }

            public Field(Open3270.TN3270.XMLScreenField f)
            {
                Allocated = true;
                IsInputField = !f.Attributes.Protected; // f.Attributes.FieldType ?
                Text = f.Text;
                Location.Column = f.Location.left;
                Location.Row = f.Location.top;
                Location.Length = f.Location.length;
                Location.Position = f.Location.position;
                ForeColor = f.Attributes.Foreground;
                BackColor = f.Attributes.Background;
                if (f.Attributes.FieldType == "Hidden") Allocated = false;
            }
            public bool Allocated { get; set; }
            public ILocation Location { get; set; } = new Location();
            public bool IsInputField { get; set; }
            public string Text { get; set; }
            public string ForeColor { get; set; }
            public string BackColor { get; set; }
            public bool UpperCase { get; set; }

        }
        public IField GetField(int index)
        {
            if (Fields == null) return null;
            if (Fields.Length <= index) return null;
            var f = Fields[index];
            return new Field(f);
        }
        public IField GetString(int index)
        {
            var f = Strings[index];
            return new Field(f);
        }
        public int GetFieldByLocation(int Column, int Row)
        {
            if (Fields == null) return -1;
            for (var i = 0; i < Fields.Length; i++)
            {
                var field = Fields[i];
                var ColumnStart = field.Location.left;
                var ColumnEnd = field.Location.left + field.Location.length;
                if (Column >= ColumnStart && Column <= ColumnEnd && field.Location.top == Row)
                {
                    return i;
                }
            }
            return -1;
        }
        public int GetStringByLocation(int Column, int Row)
        {
            if (Strings == null) return -1;
            for (var i = 0; i < Strings.Length; i++)
            {
                var field = Strings[i];
                var ColumnStart = field.Location.left;
                var ColumnEnd = field.Location.left + field.Location.length;
                if (Column >= ColumnStart && Column <= ColumnEnd && field.Location.top == Row)
                {
                    return i;
                }
            }
            return -1;
        }

    }
}
