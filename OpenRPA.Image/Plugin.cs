﻿using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.Image
{
    class Plugin : ObservableObject, IRecordPlugin
    {
        public string Name => "Image";
        public string Status => "";
        public int Priority { get => 9000; }
        private Views.RecordPluginView view;
        public System.Windows.Controls.UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.RecordPluginView();
                }
                return view;
            }
        }
        public event Action<IRecordPlugin, IRecordEvent> OnUserAction;
        public event Action<IRecordPlugin, IRecordEvent> OnMouseMove
        {
            add { }
            remove { }
        }
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
            if (timeout == TimeSpan.Zero) OnUserAction?.Invoke(null, null); // dummy use of OnUserAction to get rid of warning
            throw new NotImplementedException();
        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            throw new NotImplementedException();
        }
        public treeelement[] GetRootElements(Selector anchor)
        {
            throw new NotImplementedException();
        }
        public Selector GetSelector(Selector anchor, treeelement item)
        {
            throw new NotImplementedException();
        }
        public static IOpenRPAClient client { get; set; }
        public void Initialize(IOpenRPAClient client)
        {
            Plugin.client = client;
        }
        public IElement LaunchBySelector(Selector selector, bool CheckRunning, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        public bool Match(SelectorItem item, IElement m)
        {
            throw new NotImplementedException();
        }
        private static readonly object _lock = new object();
        private static bool _processing = false;
        private ImageElement lastelement = null;
        public bool ParseMouseMoveAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) { return false; }

            if (!e.Element.Equals(e.UIElement)) { return false; }
            if (e.Process == null) return false;
            if (e.Process != null)
            {
                if (e.Process.ProcessName == "iexplore" || e.Process.ProcessName == "iexplore.exe") { return false; }
                if (e.Process.ProcessName.ToLower() == "chrome" || e.Process.ProcessName.ToLower() == "firefox") { return false; }
                if (e.Process.ProcessName.ToLower() == "saplogon") return false;
            }
            if (e.UIElement.ControlType != "Pane") { return false; }
            e.Element = lastelement;
            if (System.Threading.Monitor.TryEnter(_lock, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    if (_processing) { return true; }
                    _processing = true;
                }
                finally
                {
                    System.Threading.Monitor.Exit(_lock);
                }
            }
            var image = getrectangle.GuessContour(e.UIElement.Rectangle, e.OffsetX, e.OffsetY, out int newOffsetX, out int newOffsetY, out System.Drawing.Rectangle resultrect);
            if (System.Threading.Monitor.TryEnter(_lock, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    _processing = false;
                }
                finally
                {
                    System.Threading.Monitor.Exit(_lock);
                }
            }
            if (image == null)
            {
                lastelement = null;
                // Log.Debug("Found null");
                e.Element = null;
                return true;
            }
            e.OffsetX = newOffsetX;
            e.OffsetY = newOffsetY;
            // Log.Debug(string.Format("Found element at ({0}, {1},{2},{3})", resultrect.X, resultrect.Y, resultrect.Width, resultrect.Height));
            lastelement = new ImageElement(resultrect, image);
            e.Element = lastelement;

            return true;
        }
        public bool ParseUserAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;

            if (!e.Element.Equals(e.UIElement)) { return false; }

            string Processname = "";
            if (e.UIElement.ProcessId > 0)
            {
                using (var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId))
                {
                    if (p.ProcessName == "iexplore" || p.ProcessName == "iexplore.exe") return false;
                    if (p.ProcessName.ToLower() == "chrome" || p.ProcessName.ToLower() == "firefox") return false;
                    if (p.ProcessName.ToLower() == "saplogon") return false;
                    Processname = p.ProcessName;
                }
            }
            if (e.UIElement.ControlType != "Pane") return false;

            Input.InputDriver.Instance.MouseMove(e.X - 80, e.Y - 80);
            System.Threading.Thread.Sleep(PluginConfig.recording_mouse_move_time);

            var a = new GetElement
            {
                Threshold = 0.9,
                // Processname = Processname
            };
            a.Variables.Add(new Variable<int>("Index", 0));
            a.Variables.Add(new Variable<int>("Total", 0));
            e.SupportInput = false;
            e.SupportSelect = false;
            var image = getrectangle.GuessContour(e.UIElement.Rectangle, e.OffsetX, e.OffsetY, out int newOffsetX, out int newOffsetY, out System.Drawing.Rectangle resultrect);
            if (image == null)
            {
                var tip = new Interfaces.Overlay.TooltipWindow("Failed Guessing Contour, please select manually");
                tip.SetTimeout(TimeSpan.FromSeconds(2));
                e.OffsetX = 10;
                e.OffsetY = 10;
                e.a = new GetElementResult(a);
                return true;

            }
            e.OffsetX = newOffsetX;
            e.OffsetY = newOffsetY;
            e.Element = new ImageElement(resultrect, image);

            a.Image = Interfaces.Image.Util.Bitmap2Base64(image);
            e.a = new GetElementResult(a);

            Input.InputDriver.Instance.MouseMove(e.X, e.Y);

            return true;
        }
        public void Start()
        {
        }
        public void Stop()
        {
        }
        void IRecordPlugin.StatusTextMouseUp()
        {
        }
    }
    public class GetElementResult : IBodyActivity
    {
        public GetElementResult(GetElement activity)
        {
            Activity = activity;
        }
        public System.Activities.Activity Activity { get; set; }
        public void AddActivity(System.Activities.Activity a, string Name)
        {
            var aa = new System.Activities.ActivityAction<ImageElement>();
            var da = new System.Activities.DelegateInArgument<ImageElement>
            {
                Name = Name
            };
            aa.Handler = a;
            ((GetElement)Activity).Body = aa;
            aa.Argument = da;
        }
        public void AddInput(string value, IElement element)
        {
            try
            {
                AddActivity(new System.Activities.Statements.Assign<string>
                {
                    To = new Microsoft.VisualBasic.Activities.VisualBasicReference<string>("item.value"),
                    Value = value
                }, "item");
                element.Value = value;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }
    }

}
