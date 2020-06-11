using Newtonsoft.Json;
using OpenRPA.NamedPipeWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRPA.SAPBridge
{
    [Serializable]
    public class SAPEvent : PipeMessage
    {
        public string action { get; set; }
        public SAPEvent() : base()
        {
        }
        public SAPEvent(string action) : base()
        {
            this.action = action;
        }
        public string data { get; set; }
        public T Get<T>()
        {
            if (string.IsNullOrEmpty(data)) return default(T);
            return JsonConvert.DeserializeObject<T>(data);
        }
        public void Set(object o)
        {
            if (o == null) data = null; else data = JsonConvert.SerializeObject(o);
        }
    }
    [Serializable]
    public class SAPToogleRecordingEvent
    {
        public bool overlay { get; set; }
        public bool mousemove { get; set; }
    }
    [Serializable]
    public class SAPInvokeMethod
    {
        public SAPInvokeMethod() { }
        public SAPInvokeMethod(string Id, string ActionName)
        {
            this.Id = Id;
            this.ActionName = ActionName;
        }
        public SAPInvokeMethod(string SystemName, string Id, string ActionName, object[] Parameters)
        {
            this.SystemName = SystemName;
            this.Id = Id;
            this.ActionName = ActionName;
            //var _params = new List<SAPEventParameter>();
            //foreach(var p in Parameters)
            //{
            //    if(p!=null)
            //    {
            //        _params.Add(new SAPEventParameter() { Value = p, ValueType = p.GetType().FullName });
            //    } else
            //    {
            //        _params.Add(new SAPEventParameter() { Value = p, ValueType = typeof(object).FullName });
            //    }
            //}
            //this.Parameters = _params.ToArray();
            this.Parameters = JsonConvert.SerializeObject(Parameters);
        }
        // public SAPEventParameter[] Parameters { get; set; }
        public string Parameters { get; set; }
        public string SystemName { get; set; }
        public string ActionName { get; set; }
        public string Id { get; set; }
        public object Result { get; set; }
    }
    //[Serializable]
    //public class SAPEventParameter
    //{
    //    public string ValueType { get; set; }
    //    public object Value { get; set; }
    //}
    [Serializable]
    public class SAPRecordingEvent
    {
        public SAPRecordingEvent() : base()
        {
        }
        //public SAPEventParameter[] Parameters { get; set; }
        public string Parameters { get; set; }
        public string Action { get; set; }
        public string ActionName { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string SystemName { get; set; }
        public int TypeAsNumber { get; set; }
        public bool ContainerType { get; set; }
        public string Id { get; set; }
    }
    [Serializable]
    public class SAPLoginEvent
    {
        public SAPLoginEvent(string Host, string Username, string Password, string Client, string Language, string SystemName) : base()
        {
            this.Host = Host;
            this.Username = Username;
            this.Password = Password;
            this.Client = Client;
            this.Language = Language;
            this.SystemName = SystemName;
        }
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Client { get; set; }
        public string Language { get; set; }
        public string SystemName { get; set; }
    }
    [Serializable]
    public class SAPGetSessions
    {
        public SAPConnection[] Connections;
    }
    [Serializable]
    public class SAPSessionInfo
    {
        public string ApplicationServer { get; set; }
        public string Client { get; set; }
        public int Codepage { get; set; }
        public int Flushes { get; set; }
        public string Group { get; set; }
        public int GuiCodepage { get; set; }
        public bool I18NMode { get; set; }
        public int InterpretationTime { get; set; }
        public bool IsLowSpeedConnection { get; set; }
        public string Language { get; set; }
        public string MessageServer { get; set; }
        public string Program { get; set; }
        public int ResponseTime { get; set; }
        public int RoundTrips { get; set; }
        public int ScreenNumber { get; set; }
        public bool ScriptingModeReadOnly { get; set; }
        public bool ScriptingModeRecordingDisabled { get; set; }
        public int SessionNumber { get; set; }
        public string SystemName { get; set; }
        public int SystemNumber { get; set; }
        public string SystemSessionId { get; set; }
        public string Transaction { get; set; }
        public string User { get; set; }
    }
    [Serializable]
    public class SAPSession
    {
        public bool Busy { get; set; }
        public string Id { get; set; }
        public SAPSessionInfo Info { get; set; }
        public bool IsActive { get; set; }
        public bool IsListBoxActive { get; set; }
        public string Name { get; set; }
        public int ProgressPercent { get; set; }
        public string ProgressText { get; set; }
        public bool Record { get; set; }
        public string RecordFile { get; set; }
        public bool SaveAsUnicode { get; set; }
        public bool ShowDropdownKeys { get; set; }
        public bool SuppressBackendPopups { get; set; }
        public int TestToolMode { get; set; }
    }
    [Serializable]
    public class SAPConnection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string Description { get; set; }
        public bool DisabledByServer { get; set; }
        public SAPSession[] sessions { get; set; }
    }
    [Serializable]
    public class SAPElementProperty
    {
        public SAPElementProperty() { }
        public SAPElementProperty(string Name, string Value, bool IsReadOnly) { this.Name = Name; this.Value = Value; this.IsReadOnly = IsReadOnly; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsReadOnly { get; set; }
    }
    [Serializable]
    public partial class SAPEventElement
    {
        public bool GetAllProperties { get; set; }
        public SAPEventElement() { }
        public int MaxItem { get; set; }
        public string Id { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Parent { get; set; }
        public string SystemName { get; set; }
        public bool ContainerType { get; set; }
        public string type { get; set; }
        public SAPEventElement[] Children { get; set; }
        public SAPEventElement[] Items { get; set; }
        public SAPElementProperty[] Properties { get; set; }
    }

}
