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
using System.Windows.Input;
using IBM5250;

namespace OpenRPA.TerminalEmulator
{
    public class termVB5250Config : ITerminalConfig
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string TermType { get; set; }
        public bool UseSSL { get; set; }
    }
    public class termVB5250 : ITerminal
    {
        public event EventHandler CursorPositionSet;
        Telnet.De.Mud.Telnet.TelnetWrapper telnet = new Telnet.De.Mud.Telnet.TelnetWrapper();
        IBM5250.Emulator emulator;
        bool isConnected;
        bool isConnecting;
        public termVB5250()
        {
            telnet.Disconnected += Telnet_Disconnected;
            telnet.ConnectionAttemptCompleted += Telnet_ConnectionAttemptCompleted;
            telnet.DataAvailable += Telnet_DataAvailable;
        }
        public void Connect(ITerminalConfig config)
        {
            if (config.TermType == "IBM-3477-FC" || config.TermType == "IBM-3477-FG" || config.TermType == "IBM-3180-2")
            {
                emulator = new IBM5250.Emulator(true);
                telnet.Terminal_Type = "IBM-3477-FC";
                emulator.TerminalType = "3477";
                emulator.TerminalModel = "FC";
            }
            else
            {
                emulator = new IBM5250.Emulator(false);
                telnet.Terminal_Type = "IBM-3179-2";
                emulator.TerminalType = "3179";
                emulator.TerminalModel = "2";
            }
            emulator.DataReady += Emulator_DataReady;
            emulator.CursorPositionSet += Emulator_CursorPositionSet;

            telnet.IBM_AuthEncryptionMethod = Telnet.De.Mud.Telnet.TelnetProtocolHandler.IBMAuthEncryptionType.SHA1;
            telnet.Connect(config.Hostname, config.Port, config.UseSSL);

            //emu.Config.FastScreenMode = true;

            //         //Retrieve host settings
            //         emu.Config.HostName = Hostname;
            //         emu.Config.HostPort = port;
            //         emu.Config.TermType = TermType;
            //         emu.Config.UseSSL = UseSSL;

            //Begin the connection process asynchomously
            IsConnecting = true;
        }
        public void Close()
        {
            telnet.Close();
            IsConnected = false;
            IsConnecting = false;
        }
        private void Emulator_CursorPositionSet(byte Row, byte Column)
        {
            CursorX = Column;
            CursorY = Row;
            CursorPositionSet?.Invoke(this, new EventArgs());
        }
        private void Telnet_ConnectionAttemptCompleted(object sender, EventArgs e)
        {
            IsConnecting = false;
            IsConnected = telnet.Connected;
            if (IsConnected) telnet.Receive();
        }
        private void Telnet_Disconnected(object sender, Telnet.Net.Graphite.Telnet.DisconnectEventArgs e)
        {
            IsConnected = false;
            IsConnecting = false;
            GenericTools.RunUI(Redraw);
        }
        private void Telnet_DataAvailable(object sender, Telnet.Net.Graphite.Telnet.DataAvailableEventArgs e)
        {
            byte[] Buffer = e.Data;
            IBM5250.TN5250.Header h = new IBM5250.TN5250.Header(ref Buffer);
            switch (h.RecType)
            {
                case IBM5250.TN5250.RecordType.GDS:
                    {
                        bool Success = false;
                        switch (h.OpCode)
                        {
                            case IBM5250.TN5250.OpCodes.TurnOnMessageLight:
                                {

                                    Success = emulator.ReadDataBuffer(Buffer, h.Length(), h.RecLen - h.Length());
                                    break;
                                }
                            case IBM5250.TN5250.OpCodes.TurnOffMessageLight:
                                {
                                    Success = emulator.ReadDataBuffer(Buffer, h.Length(), h.RecLen - h.Length());
                                    break;
                                }
                            case IBM5250.TN5250.OpCodes.None:
                                {
                                    // When USERVAR IBMSENDCONFREC=YES is sent during telnet option negotiation, the AS400 will send a connection
                                    // confirmation record with h.Reserved = &H9000 and h.Flags = &H6006.
                                    Success = emulator.ReadDataBuffer(Buffer, h.Length(), h.RecLen - h.Length());
                                    break;
                                }
                            case IBM5250.TN5250.OpCodes.CancelInvite:
                                {
                                    emulator.Invited = false;
                                    var _buf = new byte[] { };
                                    SendBytes(ref _buf, IBM5250.TN5250.OpCodes.CancelInvite);
                                    Success = emulator.ReadDataBuffer(Buffer, h.Length(), h.RecLen - h.Length());
                                    break;
                                }

                            case IBM5250.TN5250.OpCodes.Invite:
                                {
                                    emulator.Invited = true;
                                    Success = emulator.ReadDataBuffer(Buffer, h.Length(), h.RecLen - h.Length());
                                    break;
                                }

                            case IBM5250.TN5250.OpCodes.Put:
                            case IBM5250.TN5250.OpCodes.PutOrGet:
                            case IBM5250.TN5250.OpCodes.SaveScreen:
                            case IBM5250.TN5250.OpCodes.RestoreScreen:
                            case IBM5250.TN5250.OpCodes.ReadScreen // , IBM5250.TN5250.OpCodes.ReadImmediate
                         :
                                {
                                    Success = emulator.ReadDataBuffer(Buffer, h.Length(), h.RecLen - h.Length());
                                    Redraw();
                                    break;
                                }

                            default:
                                {
                                    // XXX more opcodes to handle here instead of hitting the Else?
                                    Log.Error("Unknown TN5250 Opcode: &H" + Hex((ushort)h.OpCode));
                                    break;
                                }
                        }
                        if (!Success)
                            Log.Error("There was an error while parsing the 5250 data stream");
                        break;
                    }

                default:
                    {
                        Log.Error("Record Type is invalid: 0x" + Hex((ushort)h.RecType));
                        break;
                    }
            }
        }
        string Hex(int number)
        {
            return Convert.ToString(number, 16).ToUpper();
        }
        string Hex(ushort number)
        {
            return Convert.ToString(number, 16).ToUpper();
        }
        string Hex(byte number)
        {
            return Convert.ToString(number, 16).ToUpper();
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void SendText(string text)
        {
            byte[] b = System.Text.Encoding.UTF8.GetBytes(text);
            b = emulator.UTF8_To_EBCDIC(b);
            emulator.Screen.WriteTextBuffer(b);
            emulator.Screen.UpdateStrings(true);
            Redraw();
        }
        public void SendText(int field, string text)
        {
            var f = emulator.Screen.Fields[field];
            byte[] b = Encoding.UTF8.GetBytes(text);
            b = emulator.UTF8_To_EBCDIC(b);
            emulator.Screen.WriteTextBuffer( (byte)f.Location.Row, (byte)f.Location.Column, b);
            f.Flags.Modified = true;
            f.Text = text.TrimEnd();
            emulator.Screen.Column = f.Location.Column + text.TrimEnd().Length;
            CursorX = emulator.Screen.Column;
            emulator.Screen.UpdateStrings(true);
            Log.Debug("SendText " + text + " for field #" + field);
            Redraw();
        }
        public void SendKey(Key key)
        {
            switch (key)
            {
                case Key.Back:
                    Log.Debug("SendKey " + key);
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
                    SendText(bindex, text);
                    Refresh();
                    break;
                case Key.Escape:
                    {
                        //if (emulator.Screen.PopupKeyOfAddress(emulator.Screen.Row, emulator.Screen.Column) != null/* TODO Change to default(_) if this is not a reference type */ )
                        //    // XXX this is a lazy bug workaround.  To fix it right, remove this code and figure out how to serialize popups in SAVE_SCREEN.
                        //    Interaction.MsgBox("Pressing ESC from a popup window usually ends badly.  You should probably press F12 or F3 instead.");
                        //else
                        //    switch (e.Modifiers)
                        //    {
                        //        case object _ when Keys.Shift:
                        //            {
                        //                // System Request.  User types up to 78 characters and it's sent raw to the AS400.
                        //                // SC30-3533-04 pg. 15.1-1.

                        //                // XXX should display a line for the user to enter up to 78 characters to be sent to the AS400.
                        //                // Dim b() As Byte = System.Text.Encoding.Default.GetBytes("Something the user typed")
                        //                // b = IBM5250.Emulator.UTF8_To_EBCDIC(b)
                        //                byte[] b = new byte[] { };
                        //                SendBytes(b, IBM5250.TN5250.OpCodes.None, IBM5250.TN5250.Flag.SRQ); // system request
                        //                break;
                        //            }

                        //        case object _ when Keys.None:
                        //            {
                        //                // Attention key.
                        //                SendBytes(new byte[] { }, IBM5250.TN5250.OpCodes.None, IBM5250.TN5250.Flag.ATN);
                        //                break;
                        //            }
                        //    }
                        Log.Debug("SendKey " + key);
                        var buff = new byte[] { };
                        SendBytes(ref buff, IBM5250.TN5250.OpCodes.None, (ushort)IBM5250.TN5250.Flag.ATN);
                        break;
                    }

                case Key.Enter:
                case Key.PageDown:
                case Key.PageUp:
                case Key.Tab:
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key _ when Key.F1 <= key && key <= Key.F12:
                    {
                        if (emulator.Screen.Read.Pending)
                        {
                            // XXX need to also handle READ_INPUT_FIELDS, READ_MDT_ALTERNATE, READ_IMMEDIATE, READ_IMMEDIATE_ALTERNATE.

                            if (emulator.Screen.Header.Starting_Field_For_Reads > 0)
                            {
                                // XXX need to handle resequencing of fields if screen.header.starting_field_for_reads is > 0.  Sequence will be in the FCW for each field.
                                // this.OnDataStreamError(IBM5250.Emulator.NegativeResponse.Format_Table_Resequencing_Error);

                            }

                            if (emulator.Screen.Read.Command == IBM5250.Emulator.Command.READ_MDT_FIELDS)
                            {
                                byte[] b = new byte[3];
                                b[0] = (byte)emulator.Screen.Row;
                                b[1] = (byte)emulator.Screen.Column;
                                switch (key)
                                {
                                    case Key.Enter:
                                            b[2] = (byte)IBM5250.Emulator.AID.Enter;
                                            break;
                                    case Key.PageDown:
                                    case Key.Down:
                                        b[2] = (byte)IBM5250.Emulator.AID.RollUp;
                                            break;
                                    case Key.PageUp:
                                    case Key.Up:
                                        b[2] = (byte)IBM5250.Emulator.AID.RollDown;
                                            break;
                                    case Key.Tab:
                                        b[2] = 24;
                                        //var s = "";
                                        //s += '\t';
                                        //byte[] _b = System.Text.Encoding.UTF8.GetBytes(s);
                                        //_b = emulator.UTF8_To_EBCDIC(b);
                                        //emulator.Screen.WriteTextBuffer(_b);
                                        var orgindex = GetFieldByLocation(emulator.Screen.Column, emulator.Screen.Row);
                                        if (orgindex == -1) return;
                                        var newindex = emulator.Screen.NextVerticalFieldIndex(orgindex, false);
                                        if (orgindex == newindex) newindex = 0;
                                        var nfield = GetField(newindex);
                                        CursorY = nfield.Location.Row;
                                        CursorX = nfield.Location.Column + nfield.Text.Length;
                                        emulator.Screen.Column = CursorX;
                                        emulator.Screen.Row = CursorY;
                                        Log.Verbose("Selected field #" + newindex + " at " + CursorX + "," + CursorY + " i was at field #" + orgindex);
                                        Refresh();

                                        Log.Verbose(emulator.Screen.Row + "," + emulator.Screen.Column);

                                        //SendBytes(ref b, IBM5250.TN5250.OpCodes.PutOrGet);
                                        //emulator.Screen.Read.Pending = false;
                                        //byte[] _b = new byte[] { };
                                        //SendBytes(ref _b, IBM5250.TN5250.OpCodes.None, (ushort)IBM5250.TN5250.Flag.ATN);

                                        break;
                                    case Key.Left:
                                            b[2] = (byte)IBM5250.Emulator.AID.RollRight;
                                            break;
                                    case Key.Right:
                                            b[2] = (byte)IBM5250.Emulator.AID.RollLeft;
                                            break;
                                    case Key _ when Key.F1 <= key && key <= Key.F12:
                                        {
                                            //if (e.Shift)
                                            //    // AID.PF13 - AID.PF24 are 65 more than Keys.F1 - Keys.F12
                                            //    b[2] = e.KeyCode + 65;
                                            //else
                                            //    // AID.PF1 - AID.PF12 are 63 less than Keys.F1 - Keys.F12
                                            //    b[2] = e.KeyCode - 63;
                                            //break;
                                            // f1 == 112
                                            // key.f1 == 90
                                            b[2] = (byte)(key - 41);
                                            break;
                                        }
                                }
                                if (!emulator.Screen.Header.Inhibited_AID_Codes.Contains((Emulator.AID)b[2]))
                                {
                                    for (int i = 0; i <= emulator.Screen.Fields.Length - 1; i++)
                                    {
                                        if (emulator.Screen.Fields[i].Allocated)
                                        {
                                            if (emulator.Screen.Fields[i].Flags.Modified)
                                            {
                                                // XXX should read from the screenbuffer instead
                                                int FieldStart = b.Length;
                                                int NewMax = b.Length - 1; // set to existing max
                                                NewMax += 1; // SBA byte
                                                NewMax += 2; // Row & Column

                                                string s = emulator.Screen.Fields[i].Text;
                                                bool DoNegativeZoneConversion = false;
                                                if (emulator.Screen.Fields[i].Flags.Shift_Edit_Spec == IBM5250.Emulator.EmulatorScreen.StartOfField_Header.FFW_ShiftEditSpec.SignedNumeric)
                                                {
                                                    if ((s.Length > 0) && (s.Substring(s.Length - 1) == "-"))
                                                    {
                                                        DoNegativeZoneConversion = true;
                                                        s = s.Substring(0, s.Length - 1); // remove the "-"
                                                    }
                                                }

                                                NewMax += s.Length;
                                                var oldB = b;
                                                b = new byte[NewMax + 1];
                                                if (oldB != null)
                                                    Array.Copy(oldB, b, Math.Min(NewMax + 1, oldB.Length));
                                                b[FieldStart] = (byte)IBM5250.Emulator.EmulatorScreen.WTD_Order.Set_Buffer_Address;
                                                b[FieldStart + 1] = (byte)emulator.Screen.Fields[i].Location.Row;
                                                b[FieldStart + 2] = (byte)emulator.Screen.Fields[i].Location.Column;

                                                byte[] c = System.Text.Encoding.UTF8.GetBytes(s);
                                                c = emulator.UTF8_To_EBCDIC(c);

                                                if ((c.Length > 0) & DoNegativeZoneConversion)
                                                    c[c.Length - 1] = (byte)(c[c.Length - 1] - 0x20);// change from Fx to Dx to indicate negative number

                                                Array.Copy(c, 0, b, FieldStart + 3, c.Length);
                                            }
                                        }
                                        else
                                            break;
                                    }
                                }
                                Log.Debug("SendKey " + key);
                                SendBytes(ref b, IBM5250.TN5250.OpCodes.PutOrGet);
                                emulator.Screen.Read.Pending = false;
                            }
                        }
                        break;
                    }
            }
        }
        private void Emulator_DataReady(byte[] Bytes, IBM5250.TN5250.OpCodes OpCode)
        {
            SendBytes(ref Bytes, OpCode);
        }
        private void SendBytes(ref byte[] buf, IBM5250.TN5250.OpCodes OpCode)
        {
            SendBytes(ref buf, OpCode, 0);
        }
        private void SendBytes(ref byte[] buf, IBM5250.TN5250.OpCodes OpCode, ushort Flags)
        {
            var hdr = new IBM5250.TN5250.Header(buf.Length);
            hdr.OpCode = OpCode;
            hdr.Flags = Flags;
            var b = new byte[hdr.RecLen];
            Array.Copy(hdr.ToBytes(), 0, b, 0, hdr.Length());
            Array.Copy(buf, 0, b, hdr.Length(), buf.Length);

            try
            {
                telnet.Send(b);
                //if (OpCode == IBM5250.TN5250.OpCodes.PutOrGet)
                //	SetInputReady(false); // wait for a reply before we send anything else
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }
        //      public void SendKey(TnKey key)
        //{
        //	emu.SendKey(true, key, 2000);
        //	if (key != TnKey.Tab && key != TnKey.BackTab)
        //	{
        //		Refresh();
        //	} else
        //          {
        //		UpdateCaret();
        //	}
        //}
        public void Refresh()
        {
            Refresh(100);
        }
        public void Refresh(int screenCheckInterval)
        {
            //This line keeps checking to see when we've received a valid screen of data from the mainframe.
            //It loops until the TNEmulator.Refresh() method indicates that waiting for the screen did not time out.
            //This helps prevent blank screens, etc.
            // while (!emu.Refresh(true, screenCheckInterval)) { }
            Redraw();
        }
        public bool WaitForText(string Text, TimeSpan Timeout)
        {
            if (string.IsNullOrEmpty(Text)) return true;
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                do
                {
                    foreach (var field in emulator.Screen.Strings)
                    {
                        if (!string.IsNullOrEmpty(field.Text))
                        {
                            if (field.Text.Contains(Text)) return true;
                        }
                    }
                    foreach (var field in emulator.Screen.Fields)
                    {
                        if (!string.IsNullOrEmpty(field.Text))
                        {
                            if (field.Text.Contains(Text)) return true;
                        }
                    }
                    System.Threading.Thread.Sleep(250);
                    GenericTools.RunUI(Refresh);
                } while (sw.Elapsed < Timeout);
                return false;
            }
            finally
            {
                GenericTools.RunUI(Redraw);
            }
        }
        public bool WaitForKeyboardUnlocked(TimeSpan Timeout)
        {
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                if (!emulator.Keyboard.Locked) return true;
                System.Threading.Thread.Sleep(250);
                Refresh();
            } while (sw.Elapsed < Timeout);
            return false;
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
        // public Xceed.Wpf.Toolkit.RichTextBox rtb { get; set; }
        public System.Windows.Controls.RichTextBox rtb { get; set; }
        // public TextPointer Caret { get; set; }
        public void Redraw()
        {
            if (emulator == null) return;
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
                        var fields = new List<IBM5250.Emulator.EmulatorScreen.Field>();
                        fields.AddRange(emulator.Screen.Strings.Where(l => l.Location.Row == y && l.Allocated));
                        fields.AddRange(emulator.Screen.Fields.Where(l => l.Location.Row == y && l.Allocated));
                        fields = fields.OrderBy(x => x.Location.Column).ToList();
                        foreach (var field in fields)
                        {
                            if (field.Location.Column > (text.Length + 1))
                            {
                                var subtext = string.Format("{0," + (field.Location.Column - text.Length) + "}", "");
                                var subr = new Run(subtext);
                                // subr.FontStretch = System.Windows.FontStretch.FromOpenTypeStretch(9);
                                subr.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                                p.Inlines.Add(subr);
                                text += subtext;
                            }
                            if (field.Text == null) field.Text = "";
                            var newtext = field.Text.PadRight(field.Location.Length);
                            // var newtext = string.Format("{0," + field.Location.Length + "}", field.Text);
                            if (text.Length > field.Location.Column)
                            {
                                continue;
                            }
                            text += newtext;
                            var r = new Run(newtext);
                            r.FontFamily = new System.Windows.Media.FontFamily("Consolas");



                            Color clr = Color.FromName(field.Attribute.ForeColor);
                            if (field.Attribute.ForeColor == "Background") clr = Color.Black;
                            if (field.Attribute.ForeColor == "Green") clr = Color.LimeGreen;
                            if (field.Attribute.ForeColor == "Turquoise") clr = Color.Cyan;
                            if (field.Attribute.ForeColor == "Pink") clr = Color.Fuchsia;
                            if (field.Attribute.ForeColor == "Blue") clr = Color.CornflowerBlue;
                            r.Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(clr.R, clr.G, clr.B));

                            Color bck = Color.FromName(field.Attribute.BackColor);
                            if (field.Attribute.BackColor == "Background") bck = Color.Black;
                            if (field.Attribute.BackColor == "Green") bck = Color.LimeGreen;
                            if (field.Attribute.BackColor == "Turquoise") bck = Color.Cyan;
                            if (field.Attribute.BackColor == "Pink") bck = Color.Fuchsia;
                            if (field.Attribute.BackColor == "Blue") bck = Color.CornflowerBlue;

                            r.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(bck.R, bck.G, bck.B));

                            p.Inlines.Add(r);
                            if (field.Location.Row == HighlightCursorY)
                            {
                                if (HighlightCursorX >= field.Location.Column && HighlightCursorX <= (field.Location.Column + field.Location.Length) )
                                {
                                    if(!foundHighlight || (!HighlightEdit && field.IsInputField))
                                    {
                                        foundHighlight = true;
                                        HighlightEdit = field.IsInputField;
                                        bck = Color.Red;
                                        clr = Color.Yellow;
                                        if (field.IsInputField)
                                        {
                                            bck = Color.Green;
                                            clr = Color.Yellow;
                                        }
                                        r.Background = new System.Windows.Media.SolidColorBrush(
                                            System.Windows.Media.Color.FromRgb(bck.R, bck.G, bck.B));
                                        r.Foreground = new System.Windows.Media.SolidColorBrush(
                                            System.Windows.Media.Color.FromRgb(clr.R, clr.G, clr.B));

                                    }

                                    //var brush = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFrom("#ffaacc");
                                    //r.BorderBrush = brush;
                                    //r.BorderThickness = new System.Windows.Thickness(1, 1, 1, 3);
                                }
                            }
                            if(!IsConnected)
                            {
                                bck = Color.Black;
                                clr = Color.Gray;
                                r.Background = new System.Windows.Media.SolidColorBrush(
                                    System.Windows.Media.Color.FromRgb(bck.R, bck.G, bck.B));
                                r.Foreground = new System.Windows.Media.SolidColorBrush(
                                    System.Windows.Media.Color.FromRgb(clr.R, clr.G, clr.B));
                            }
                        }
                        //if (text.Length < 80)
                        //{
                        //    var endstr = string.Format("{0," + (80 - text.Length) + "}", "");
                        //    var r = new Run(endstr);
                        //    p.Inlines.Add(r);
                        //    text += endstr;
                        //}
                        try
                        {
                            rtb.Document.Blocks.Add(p);
                        }
                        catch (Exception)
                        {
                            return;
                        }

                    }
                    CursorPositionSet?.Invoke(this, new EventArgs());
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return;
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

            public Field(Emulator.EmulatorScreen.Field f)
            {
                Allocated = f.Allocated;
                IsInputField = f.IsInputField;
                Text = f.Text;
                Location.Column = f.Location.Column;
                Location.Row = f.Location.Row;
                Location.Length = f.Location.Length;
                Location.Position = f.Location.Position;
                ForeColor = f.Attribute?.ForeColor;
                BackColor = f.Attribute?.BackColor;
                UpperCase = f.Flags.UpperCase;
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
            var f = emulator.Screen.Fields[index];
            return new Field(f);
        }
        public IField GetString(int index)
        {
            var f = emulator.Screen.Strings[index];
            return new Field(f);
        }
        public int GetFieldByLocation(int Column, int Row)
        {
            for (var i = 0; i < emulator.Screen.Fields.Length; i++)
            {
                var field = emulator.Screen.Fields[i];
                var ColumnStart = field.Location.Column;
                var ColumnEnd = field.Location.Column + field.Location.Length;
                if (Column >= ColumnStart && Column <= ColumnEnd && field.Location.Row == Row)
                {
                    return i;
                }
            }
            //var fields = new List<IBM5250.Emulator.EmulatorScreen.Field>();
            //fields.AddRange(emulator.Screen.Strings.Where(l => l.Location.Row == Row));
            //fields.AddRange(emulator.Screen.Fields.Where(l => l.Location.Row == Row));
            //foreach (var field in fields)
            //{
            //    var ColumnStart = field.Location.Column;
            //    var ColumnEnd = field.Location.Column + field.Location.Length;
            //    if (Column >= ColumnStart && Column <= ColumnEnd)
            //    {
            //        var idx = emulator.Screen.Fields
            //    }
            //}
            return -1;
        }
        public int GetStringByLocation(int Column, int Row)
        {
            for (var i = 0; i < emulator.Screen.Strings.Length; i++)
            {
                var field = emulator.Screen.Strings[i];
                var ColumnStart = field.Location.Column;
                var ColumnEnd = field.Location.Column + field.Location.Length;
                if (Column >= ColumnStart && Column <= ColumnEnd && field.Location.Row == Row)
                {
                    return i;
                }
            }
            return -1;
        }
        public string GetTextAt(int Row, int Column, int length)
        {
            return emulator.Screen.GetText(Row, Column, length);
        }
    }
}
