using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenRPA.Interfaces.VT
{
    public interface ITerminalConfig
    {
        string Hostname { get; set; }
        int Port { get; set; }
        string TermType { get; set; }
        bool UseSSL { get; set; }
    }
    public interface IField
    {
        bool Allocated { get; set; }
        ILocation Location { get; set; }
        bool IsInputField { get; set; }
        string Text { get; set; }
        string ForeColor { get; set; }
        string BackColor { get; set; }
        bool UpperCase { get; set; }
    }
    public interface ILocation
    {
        int Position { get; set; }
        int Column { get; set; }
        int Row { get; set; }
        int Length { get; set; }
    }
    public interface ITerminalSession : INotifyPropertyChanged
    {
        ITerminal Terminal { get; set; }
        ITerminalConfig Config { get; set; }
        void Connect();
        void Disconnect();
        void Refresh();
        void Show();
        void Close();
        string WorkflowInstanceId { get; set; }
    }
    public interface ITerminal : INotifyPropertyChanged
    {
        event EventHandler CursorPositionSet;
        void Connect(ITerminalConfig config);
        bool IsConnected { get; set; }
        bool IsConnecting { get; set; }
        int CursorY { get; set; }
        int CursorX { get; set; }
        int HighlightCursorY { get; set; }
        int HighlightCursorX { get; set; }

        void SendText(int field, string text);
        void SendText(string text);
        void Refresh();
        void Redraw();
        int GetFieldByLocation(int column, int row);
        int GetStringByLocation(int column, int row);
        IField GetField(int field);
        IField GetString(int field);
        void SendKey(Key key);
        void Close();
        bool WaitForText(string Text, TimeSpan Timeout);
        bool WaitForKeyboardUnlocked(TimeSpan Timeout);
    }
}
