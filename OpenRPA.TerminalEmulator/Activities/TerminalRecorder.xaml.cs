using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenRPA.TerminalEmulator
{

    /// <summary>
    /// Interaction logic for TerminalRecorder.xaml
    /// </summary>
    public partial class TerminalRecorder : Window, Interfaces.VT.ITerminalSession
    {
        public TerminalRecorder()
        {
            ConnectCommand = new CommandHandler(() => Connect(), () => CanConnect());
            DisconnectCommand = new CommandHandler(() => Disconnect(), () => CanDisconnect());
            RefreshCommand = new CommandHandler(() => Refresh(), () => CanRefresh());
            SetTextCommand = new CommandHandler(() => SetText(), () => CanSetText());
            SendTextCommand = new CommandHandler(() => SendText(), () => CanSendText());
            GetTextCommand = new CommandHandler(() => GetText(), () => CanGetText());
            InitializeComponent();
            rtbConsole.Focus();
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (Terminal == null) return;
                if (!Terminal.IsConnected) return;
                var _f = Terminal.GetField(0);
                if (_f == null) Terminal.Refresh();
                if (_f != null && _f.BackColor == null) Terminal.Refresh();
            }
            catch (Exception)
            {
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        System.Timers.Timer timer = null;
        private void Terminal_CursorPositionSet(object sender, EventArgs e)
        {
            if (Terminal == null) return;
                GenericTools.RunUI(() =>
            {
                try
                {
                    int ColumnNumber, LineNumber;
                    GetCaretPosition(out ColumnNumber, out LineNumber);
                    if (ColumnNumber == Terminal.CursorX && LineNumber == Terminal.CursorY)
                    {
                        Log.Verbose("Caret already where it should be");
                        return;
                    }
                    else
                    {
                        Log.Debug("Caret at " + ColumnNumber + "," + LineNumber + " but shold be at " + Terminal.CursorX + "," + Terminal.CursorY);
                    }
                    if (rtbConsole.Document.Blocks.Count < Terminal.CursorY)
                    {
                        Log.Output("Cannot move caret, UI not complete");
                        return;
                    }
                    //int diff = 0;
                    //while(ColumnNumber != Terminal.CursorX || LineNumber != Terminal.CursorY)
                    //{
                    //    if (Terminal.CursorX <= 0 && Terminal.CursorY <= 0) return;
                    //    diff++;
                    //    var y = Terminal.CursorY - 1;
                    //    if (y < 0) y = 0;
                    //    var x = Terminal.CursorX + diff;
                    //    if (diff > 40) return;
                    //    // if (x > 80) x = 80;
                    //    var _p = rtbConsole.Document.Blocks.ElementAt(y);
                    //    if (_p == null) return;
                    //    TextPointer myTextPointer2 = _p.ContentStart.GetPositionAtOffset(x);
                    //    if (myTextPointer2 == null) return;
                    //    rtbConsole.CaretPosition = myTextPointer2;
                    //    GetCaretPosition(out ColumnNumber, out LineNumber);
                    //    if (ColumnNumber != Terminal.CursorX || LineNumber != Terminal.CursorY)
                    //    {
                    //        Console. Write Line("Caret at " + ColumnNumber + "," + LineNumber + " but shold be at " + Terminal.CursorX + "," + Terminal.CursorY);
                    //    }                            
                    //}
                    var y = Terminal.CursorY - 1;
                    if (y < 0) y = 0;
                    var x = Terminal.CursorX + 4;
                    var _p = rtbConsole.Document.Blocks.ElementAt(y);
                    if (_p == null) return;
                    TextPointer myTextPointer2 = _p.ContentStart.GetPositionAtOffset(x);
                    if (myTextPointer2 == null) return;
                    rtbConsole.CaretPosition = myTextPointer2;

                    Log.Debug("Set Carrat at " + Terminal.CursorX + "," + Terminal.CursorY);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return;
                }
            });
        }
        public Interfaces.VT.ITerminal Terminal { get; set; }
        public Interfaces.VT.ITerminalConfig Config { get; set; }
        public string WorkflowInstanceId { get; set; }
        public int lastField = -1;
        public string lastText = "";
        private bool Working = false;
        private bool HandlingInput = false;
        private bool SkipNextKey = false;
        public bool HighlightOnMove { get; set; }
        public bool CatchKeys { get; set; }        
        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (Terminal == null) return;
            if (Terminal.IsConnected)
            {
                if (Terminal is termOpen3270)
                {
                    var index = Terminal.GetFieldByLocation(Terminal.CursorX, Terminal.CursorY);
                    if (index == -1)
                    {
                        Log.Output("No field found at " + Terminal.CursorX + "," + Terminal.CursorY);
                        Terminal.Refresh();
                        return;
                    }
                    var _f = Terminal.GetField(index);
                    if (_f == null) return;
                    string text = _f.Text + e.Text;
                    if (lastField != index && _f.Text.Length == _f.Location.Length) text = e.Text;
                    if (lastField == index && text.StartsWith(lastText))
                    {
                        text = lastText + e.Text;
                    }
                    if (_f.UpperCase) text = text.ToUpper();
                    lastField = index;
                    lastText = text;
                    e.Handled = true;
                    var ColumnStart = _f.Location.Column;
                    var ColumnEnd = _f.Location.Column + _f.Location.Length;
                    Log.Debug("field #" + index + " " + _f.Location.Column + "," + _f.Location.Row + " " + _f.Location.Length);
                    Log.Debug("Assign field #" + index + " found at " + Terminal.CursorX + "," + Terminal.CursorY + " the value of '" + text + "'");
                    Terminal.SendText(index, text);
                    // Terminal.SendText(e.Text);
                }
                else
                {
                    var index = Terminal.GetFieldByLocation(Terminal.CursorX, Terminal.CursorY);
                    if (index == -1)
                    {
                        Log.Output("No field found at " + Terminal.CursorX + "," + Terminal.CursorY);
                        return;
                    }
                    var _f = Terminal.GetField(index);
                    if (_f == null) return;
                    string text = _f.Text + e.Text;
                    if (_f.UpperCase) text = text.ToUpper();
                    e.Handled = true;
                    var ColumnStart = _f.Location.Column;
                    var ColumnEnd = _f.Location.Column + _f.Location.Length;
                    Log.Debug("field #" + index + " " + _f.Location.Column + "," + _f.Location.Row + " " + _f.Location.Length);
                    Log.Debug("Assign field #" + index + " found at " + Terminal.CursorX + "," + Terminal.CursorY + " the value of '" + text + "'");
                    Terminal.SendText(index, text);
                }

            }
        }
        public ICommand ConnectCommand { get; set; }
        public bool CanConnect()
        {
            if (Terminal == null) { return true; }
            return !Terminal.IsConnected && !Terminal.IsConnecting;
        }
        public void Connect()
        {
            if (Terminal != null)
            {
                Terminal.Close();
                Terminal = null;
                Terminal.CursorPositionSet -= Terminal_CursorPositionSet;
            }
            if (Config.TermType == "IBM-3179-2")
            {
                Terminal = new termOpen3270();
                (Terminal as termOpen3270).rtb = rtbConsole;
            }
            else
            {
                Terminal = new termVB5250();
                (Terminal as termVB5250).rtb = rtbConsole;
            }
            Terminal.CursorPositionSet += Terminal_CursorPositionSet;
            NotifyPropertyChanged("Terminal");
            Terminal.Connect(Config);
            rtbConsole.Focus();
        }
        public ICommand DisconnectCommand { get; set; }
        public bool CanDisconnect()
        {
            if(Terminal==null) { return false; }
            return Terminal.IsConnected || Terminal.IsConnecting;
        }
        public void Disconnect()
        {
            if (Terminal != null)
            {
                Terminal.Close();
                Terminal.CursorPositionSet -= Terminal_CursorPositionSet;
                Terminal.Redraw();
                Terminal = null;
            }
        }
        public ICommand RefreshCommand { get; set; }
        public bool CanRefresh()
        {
            if (Terminal == null) { return false; }
            return Terminal.IsConnected;
        }
        public void Refresh()
        {
            if (Terminal != null) Terminal.Refresh();
            rtbConsole.Focus();
        }
        public ICommand SetTextCommand { get; set; }
        public bool CanSetText()
        {
            if (Terminal == null) { return false; }
            return Terminal.IsConnected;
        }
        public void SetText()
        {
            GenericTools.RunUI(() =>
            {
                if (Terminal != null && Terminal.IsConnected)
                {
                    HandlingInput = true;
                    try
                    {
                        var index = Terminal.GetFieldByLocation(Terminal.HighlightCursorX, Terminal.HighlightCursorY);
                        if (index == -1) index = Terminal.GetFieldByLocation(Terminal.CursorX, Terminal.CursorY);
                        if (index == -1)
                        {
                            Log.Output("No field found, please click a field or use <tab> to select on");
                            Terminal.Refresh();
                            return;
                        }
                        var _f = Terminal.GetField(index);
                        if (_f == null) return;
                        string text = _f.Text;
                        if (!string.IsNullOrEmpty(text)) text = text.TrimEnd(); // 
                        if (_f.UpperCase) text = text.ToUpper();
                        var win = new InsertText
                        {
                            Topmost = true,
                            Text = text
                        };
                        win.Owner = this;
                        if (win.ShowDialog() == true)
                        {
                            SkipNextKey = true;
                            text = win.Text;
                            if (_f.UpperCase) text = text.ToUpper();
                            global.OpenRPAClient.CurrentDesigner.AddActivity(new SetText() { DisplayName = "SetText #" + index, Text = text, Field = index });
                            Focus();
                            Terminal.SendText(index, text);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        HandlingInput = false;
                    }
                }
            });
        }
        public ICommand SendTextCommand { get; set; }
        public bool CanSendText()
        {
            if (Terminal == null) { return false; }
            return Terminal.IsConnected;
        }
        public void SendText()
        {
            if (Terminal != null && Terminal.IsConnected)
            {
                HandlingInput = true;
                try
                {
                    var win = new InsertText
                    {
                        Topmost = true,
                        Text = ""
                    };
                    win.Owner = this;
                    if (win.ShowDialog() == true)
                    {
                        SkipNextKey = true;
                        var text = win.Text;
                        this.Focus();
                        Terminal.SendText(text);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    HandlingInput = false;
                }
            }
        }
        public ICommand GetTextCommand { get; set; }
        public bool CanGetText()
        {
            if (Terminal == null) { return false; }
            return Terminal.IsConnected;
        }
        public void GetText()
        {
            if (Terminal != null && Terminal.IsConnected)
            {
                HandlingInput = true;
                try
                {
                    var index = Terminal.GetFieldByLocation(Terminal.HighlightCursorX, Terminal.HighlightCursorY);
                    if (index == -1)
                    {
                        index = Terminal.GetStringByLocation(Terminal.HighlightCursorX, Terminal.HighlightCursorY);
                        if (index > -1)
                        {
                            global.OpenRPAClient.CurrentDesigner.AddActivity(new GetText() { DisplayName = "GetText string #" + index, String = index });
                            Focus();
                        }
                        return;
                    }
                    global.OpenRPAClient.CurrentDesigner.AddActivity(new GetText() { DisplayName = "GetText field #" + index, Field = index });
                    Focus();
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    HandlingInput = false;
                }
            }
        }
        private void GetCaretPosition(out int ColumnNumber, out int LineNumber)
        {
            TextPointer current = rtbConsole.CaretPosition;
            var start = current.GetEdgeTextPointer(LogicalDirection.Backward); // Word position before caret
            var end = current.GetEdgeTextPointer(LogicalDirection.Forward); // Word position after caret

            current.GetCharacterRect(LogicalDirection.Forward);
            current.GetLineStartPosition(-int.MaxValue, out LineNumber);
            if (LineNumber < 0) LineNumber *= -1;
            LineNumber++;

            //var p = rtbConsole.Document.Blocks.ElementAt(lineNumber - 1) as Paragraph;
            ColumnNumber = start.GetLineStartPosition(0).GetOffsetToPosition(current) - 3;
        }
        public void GetCurrentWord()
        {
            if (Working) return;
            Working = true;
            try
            {
                int ColumnNumber, LineNumber;
                GetCaretPosition(out ColumnNumber, out LineNumber);
                if (ColumnNumber > 0 && LineNumber > 0)
                {
                    Terminal.HighlightCursorY = LineNumber;
                    Terminal.HighlightCursorX = ColumnNumber;

                    var text = "";
                    var findex = Terminal.GetFieldByLocation(ColumnNumber, LineNumber);
                    var sindex = Terminal.GetStringByLocation(ColumnNumber, LineNumber);

                    if (findex > -1) text = Terminal.GetField(findex).Text;
                    if (sindex > -1 && string.IsNullOrEmpty(text)) text = Terminal.GetString(sindex).Text;
                    // Console.Write Line("Line " + LineNumber + " columnNumber " + ColumnNumber + " findex " + findex + " sindex " + sindex + " = " + text);
                    // Terminal.Refresh();
                }
                // rtbConsole.Focus();
            }
            catch (Exception)
            {
            }
            Working = false;
        }
        Point lastMousePos;
        private void rtbConsole_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (Terminal == null) return;
                if (!HighlightOnMove) return;
                if (HandlingInput) return;
                var pos = e.GetPosition(rtbConsole);
                if (pos == lastMousePos) return;
                lastMousePos = pos;
                TextPointer t = rtbConsole.GetPositionFromPoint(pos, true);
                rtbConsole.CaretPosition = t;
                GetCurrentWord();
                Terminal.Redraw();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void rtbConsole_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Terminal == null) return;
                if (HandlingInput) return;
                var pos = e.GetPosition(rtbConsole);
                TextPointer t = rtbConsole.GetPositionFromPoint(pos, true);
                rtbConsole.CaretPosition = t;
                GetCurrentWord();
                Terminal.Redraw();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Terminal == null) return;
            if (HandlingInput) return;
            if(SkipNextKey)
            {
                SkipNextKey = false;
                return;
            }
            if (Terminal.IsConnected)
            {
                char parsedCharacter = ' ';
                if (char.TryParse(e.Key.ToString(), out parsedCharacter))
                {
                }
                else
                {
                    if (e.Key != Key.Space)
                    {
                        if(CatchKeys)
                        {
                            global.OpenRPAClient.CurrentDesigner.AddActivity(new SendKey() { DisplayName = "Send Key " + e.Key.ToString(), Key = e.Key.ToString() });
                            Focus();

                        }
                        Terminal.SendKey(e.Key);
                    }
                }
                if (e.Key == Key.Tab) e.Handled = true;
                if (e.Key == Key.Back) e.Handled = true;
                e.Handled = true;
            }
        }
        private void rtbConsole_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (HandlingInput) return;
            if (e.Key == Key.Tab) e.Handled = true;
            if (e.Key == Key.Back) e.Handled = true;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Terminal != null) Terminal.Close();
            Close();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Terminal != null) { Terminal.Close(); Terminal = null; }
            if (timer != null) { timer.Stop(); timer.Dispose(); timer = null; }
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
