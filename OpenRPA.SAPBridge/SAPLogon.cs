/* 
MIT License

Copyright (c) [2016] [Zhou Yang]

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
*/
using SAPFEWSELib;
using SapROTWr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OpenRPA.SAPBridge
{
    public delegate void LoginHandler(GuiSession sender, EventArgs e);
    public class SAPLogon
    {
        public event LoginHandler FailLogin;
        public event LoginHandler BeforeLogin;
        public event LoginHandler AfterLogin;
        public GuiSession Session { get; private set; }
        public GuiConnection Connection { get; private set; }
        // public GuiApplication Application { get; private set; }
        public SAPLogon() { }
        public void StartProcess(string processPath = "saplogon.exe")
        {
            System.Diagnostics.Process.Start(processPath);
        }
        private static object _lockObj = new object();
        public void OpenConnection(string server, int secondsOfTimeout = 10)
        {
            if (System.Threading.Monitor.TryEnter(_lockObj, 1000))
            {
                try
                {
                    var Application = SAPHook.Instance.app;
                    try
                    {
                        Application.OpenConnectionByConnectionString(server);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Open Connection Failed, is ip/fqdn correct? is server alive and running? " + ex.Message);
                    }
                    var index = Application.Connections.Count - 1;
                    this.Connection = Application.Children.ElementAt(index) as GuiConnection;
                    index = Connection.Sessions.Count - 1;
                    if (Connection.Sessions.Count == 0) throw new Exception("New session not found, did you forget to enable scripting on the server side ?");
                    this.Session = Connection.Children.Item(index) as GuiSession;
                }
                finally
                {
                    System.Threading.Monitor.Exit(_lockObj);
                }
            }
        }
        public bool Login(string UserName, string Password, string Client, string Language)
        {
            BeforeLogin?.Invoke(Session, new EventArgs());
            Session.FindById<GuiTextField>("wnd[0]/usr/txtRSYST-BNAME").Text = UserName;
            Session.FindById<GuiTextField>("wnd[0]/usr/pwdRSYST-BCODE").Text = Password;
            Session.FindById<GuiTextField>("wnd[0]/usr/txtRSYST-MANDT").Text = Client;
            Session.FindById<GuiTextField>("wnd[0]/usr/txtRSYST-LANGU").Text = Language;
            var window = Session.FindById<GuiFrameWindow>("wnd[0]");
            window.SendVKey(0);
            GuiStatusbar status = Session.FindById<GuiStatusbar>("wnd[0]/sbar");
            if (status != null && status.MessageType.ToLower() == "e")
            {
                Connection.CloseSession(Session.Id);
                FailLogin?.Invoke(Session, new EventArgs());
                return false;
            }
            AfterLogin?.Invoke(Session, new EventArgs());
            GuiRadioButton rb_Button = Session.FindById<GuiRadioButton>("wnd[1]/usr/radMULTI_LOGON_OPT2");
            if (rb_Button != null)
            {
                rb_Button.Select();
                window.SendVKey(0);
            }
            return true;
        }
        public GuiMainWindow MainWindow
        {
            get
            {
                return FindElementById<GuiMainWindow>("wnd[0]");
            }
        }
        public GuiFrameWindow PopupWindow
        {
            get
            {
                if (!(Session.ActiveWindow is GuiMainWindow))
                    return Session.ActiveWindow;
                else
                    return null;
            }
        }
        public T FindElementById<T>(string id) where T : class
        {
            var component = FindElementById(id);
            T element = component as T;
            return element;
        }
        public GuiComponent FindElementById(string id)
        {
            GuiComponent component = Session.FindById(id);
            return component;
        }
        public static GuiApplication GetSAPGuiApp(int secondsOfTimeout = 10)
        {
            SapROTWr.CSapROTWrapper sapROTWrapper = new SapROTWr.CSapROTWrapper();
            return getSAPGuiApp(sapROTWrapper, secondsOfTimeout);
        }
        private static GuiApplication getSAPGuiApp(CSapROTWrapper sapROTWrapper, int secondsOfTimeout)
        {
            object SapGuilRot = sapROTWrapper.GetROTEntry("SAPGUI");
            if (secondsOfTimeout < 0)
            {
                throw new TimeoutException(string.Format("Can get sap script engine in {0} seconds", secondsOfTimeout));
            }
            else
            {
                if (SapGuilRot == null)
                {
                    System.Threading.Thread.Sleep(1000);
                    return getSAPGuiApp(sapROTWrapper, secondsOfTimeout - 1);
                }
                else
                {
                    object engine = SapGuilRot.GetType().InvokeMember("GetSCriptingEngine", System.Reflection.BindingFlags.InvokeMethod, null, SapGuilRot, null);
                    if (engine == null)
                        throw new NullReferenceException("No SAP GUI application found");
                    return engine as GuiApplication;
                }
            }
        }
        public void CloseSession()
        {
            Connection.CloseSession(Session.Id);
        }
    }
}
