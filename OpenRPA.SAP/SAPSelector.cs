using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAP
{
    class SAPSelector : Selector
    {
        SAPElement element { get; set; }
        public SAPSelector(string json) : base(json) { }
        public SAPSelector(SAPElement element, SAPSelector anchor, bool doEnum)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Selector(string.Format("SAPselector::AutomationElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            Log.Selector(string.Format("SAPselector::GetControlVSAPwWalker::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            Clear();
            SAPSelectorItem item;
            if (anchor == null)
            {

                item = new SAPSelectorItem(element, true);
                item.Enabled = true;
                item.canDisable = false;
                Items.Add(item);
            }
            item = new SAPSelectorItem(element, false);
            item.Enabled = true; item.canDisable = false;
            var idfield = element.id;
            if (idfield.Contains("/")) idfield = idfield.Substring(idfield.LastIndexOf("/") + 1);
            item.Properties.Add(new SelectorItemProperty("idfield", idfield));
            Items.Add(item);
            Log.Selector(string.Format("SAPselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public override IElement[] GetElements(IElement fromElement = null, int maxresults = 1)
        {
            return SAPSelector.GetElementsWithuiSelector(this, fromElement, 0, maxresults, false);
        }
        private static SAPElement[] GetElementsWithuiSelector(SAPSession session, SAPSelector selector, IElement fromElement, int skip, int maxresults, bool FlatternGuiTree)
        {
            var result = new List<SAPElement>();
            SAPElement _fromElement = fromElement as SAPElement;

            var root = new SAPSelectorItem(selector[0]);
            var sel = new SAPSelectorItem(selector[1]);
            var SystemName = root.SystemName;
            var id = sel.id;
            var path = sel.path;
            var cell = sel.cell;

            var msg = new SAPEvent("getitem");
            msg.Set(new SAPEventElement() { Id = id, SystemName = SystemName, GetAllProperties = true, Path = path, Cell = cell, Skip = skip, MaxItem = maxresults, Flat = FlatternGuiTree });
            msg = SAPhook.Instance.SendMessage(msg, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            if (msg != null)
            {
                var ele = msg.Get<SAPEventElement>();
                if (!string.IsNullOrEmpty(ele.Id))
                {
                    var _element = new SAPElement(null, ele);
                    result.Add(_element);
                }
            }
            return result.ToArray();
        }
        public static SAPElement[] GetElementsWithuiSelector(SAPSelector selector, IElement fromElement, int skip, int maxresults, bool FlatternGuiTree)
        {
            var result = new List<SAPElement>();
            var root = new SAPSelectorItem(selector[0]);
            var SystemName = root.SystemName;
            if (SAPhook.Instance.Sessions == null || SAPhook.Instance.Sessions.Length == 0)
            {
                SAPhook.Instance.RefreshConnections();
            }
            if (SAPhook.Instance.Sessions != null)
                foreach (var session in SAPhook.Instance.Sessions)
                {
                    if (string.IsNullOrEmpty(SystemName) || (SystemName == session.Info.SystemName))
                    {
                        result.AddRange(GetElementsWithuiSelector(session, selector, fromElement, skip, maxresults, FlatternGuiTree));
                        if (result.Count > maxresults) return result.ToArray();
                    }
                }
            return result.ToArray();
        }
    }
}
