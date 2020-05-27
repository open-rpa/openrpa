using SAPFEWSELib;
using SapROTWr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAP
{
    public class SAPhook
    {
        private static SAPhook _instance = null;
        public Action<SAPElement> OnRecordEvent;
        public static SAPhook Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SAPhook();
                    _instance.init();
                }
                return _instance;
            }
        }
        public GuiApplication app { get; set; } = null;
        public void init()
        {
            app = GetSAPGuiApp();
        }
        public GuiSession[] Sessions { get; private set; }
        public void RefreshSessions()
        {
            var result = new List<GuiSession>();
            var application = app;
            if (app.Connections.Count == 0) return;
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = application.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;

                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    result.Add(session);
                }
            }
            Sessions = result.ToArray();
        }
        public bool Recording { get; private set; } = false;
        public void BeginRecord()
        {
            var application = app;
            if (app.Connections.Count == 0) return;
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = application.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;

                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    session.Change += Session_Change;
                    session.AbapScriptingEvent += Session_AbapScriptingEvent;
                    Recording = true;
                }
            }
        }

        private void Session_AbapScriptingEvent(string param)
        {
            Console.WriteLine(param);
        }

        private void Session_Change(GuiSession Session, GuiComponent Component, object CommandArray)
        {
            var Element = new SAPElement(Component);

            object[] objs = CommandArray as object[];

            objs = objs[0] as object[];
            System.Reflection.BindingFlags Action;
            switch (objs[0].ToString().ToLower())
            {
                case "m":
                    Action = System.Reflection.BindingFlags.InvokeMethod;
                    break;
                case "sp":
                    Action = System.Reflection.BindingFlags.SetProperty;
                    break;
            }

            var ActionName = objs[1].ToString();
            upperFirstChar(ref ActionName);


            OnRecordEvent?.Invoke(Element);


        }
        private static void upperFirstChar(ref string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = char.ToUpper(s[0]) + s.Substring(1);
            }
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

    }
}
