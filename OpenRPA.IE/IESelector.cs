using FlaUI.Core;
using FlaUI.Core.AutomationElements.Infrastructure;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    class IESelector : Selector
    {
        IEElement element { get; set; }
        public IESelector(string json) : base(json) { }
        public IESelector(mshtml.IHTMLElement element, IESelector anchor, mshtml.HTMLDocument Document, bool doEnum)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Debug(string.Format("IEselector::AutomationElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            Log.Debug(string.Format("IEselector::GetControlViewWalker::end {0:mm\\:ss\\.fff}", sw.Elapsed));



            //mshtml.IHTMLElement root = null;
            //mshtml.IHTMLElement baseElement = null;
            var pathToRoot = new List<mshtml.IHTMLElement>();
            while (element != null)
            {
                // Break on circular relationship (should not happen?)
                //if (pathToRoot.Contains(element) || element.Equals(_rootElement)) { break; }
                if (pathToRoot.Contains(element)) { break; }
                try
                {
                    pathToRoot.Add(element);
                }
                catch (Exception)
                {
                }
                try
                {
                    //element = _treeWalker.GetParent(element);
                    element = element.parentElement;
                    if (element != null)
                    {
                        Log.Information(element.tagName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return;
                }
            }
            Log.Debug(string.Format("IEselector::create pathToRoot::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            pathToRoot.Reverse();
            if (anchor != null)
            {
                var anchorlist = anchor.Where(x => x.Enabled && x.Selector == null).ToList();
                for (var i = 0; i < anchorlist.Count(); i++)
                {
                    if (((IESelectorItem)anchorlist[i]).match(pathToRoot[0]))
                    {
                        pathToRoot.Remove(pathToRoot[0]);
                    }
                    else
                    {
                        Log.Debug("Element does not match the anchor path");
                        return;
                    }
                }
            }

            if (pathToRoot.Count == 0)
            {
                Log.Error("Element is same as annchor");
                return;
            }
            element = pathToRoot.Last();
            Clear();
            Log.Debug(string.Format("IEselector::remove anchor if needed::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            IESelectorItem item;
            if (anchor == null)
            {
                item = new IESelectorItem(Document);
                item.Enabled = true;
                //item.canDisable = false;
                Items.Add(item);
                item.PropertyChanged += SelectorChanged;
            }
            //var test = new List<IEElement>();
            //foreach(var e in pathToRoot) { test.Add(new IEElement(e)); }
            for (var i = 0; i < pathToRoot.Count(); i++)
            {
                var o = pathToRoot[i];
                item = new IESelectorItem(o);
                if (i == 0 || i == (pathToRoot.Count() - 1)) item.canDisable = false;
                foreach (var p in item.Properties) // TODO: Ugly, ugly inzuBiz hack !!!!
                {
                    int idx = p.Value.IndexOf(".");
                    if (p.Name == "ClassName" && idx > -1)
                    {
                        int idx2 = p.Value.IndexOf(".", idx + 1);
                        if (idx2 > idx) p.Value = p.Value.Substring(0, idx2 + 1) + "*";
                    }
                    //if (p.Name == "ClassName" && p.Value.StartsWith("IEForms10")) p.Value = "IEForms10*";
                }
                if (doEnum) item.EnumNeededProperties(o, o.parentElement);

                Items.Add(item);
                item.PropertyChanged += SelectorChanged;
            }
            pathToRoot.Reverse();
            Log.Debug(string.Format("IEselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        public static IEElement[] GetElementsWithuiSelector(IESelector selector, IElement fromElement = null)
        {
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
            SHDocVw.WebBrowser Browser = null;
            mshtml.HTMLDocument Document = null;
            foreach (SHDocVw.InternetExplorer _ie in shellWindows)
            {
                var filename = System.IO.Path.GetFileNameWithoutExtension(_ie.FullName).ToLower();

                if (filename.Equals("iexplore"))
                {
                    //Debug.WriteLine("Web Site   : {0}", _ie.LocationURL);
                    try
                    {
                        Browser = _ie as SHDocVw.WebBrowser;
                        Document = (Browser.Document as mshtml.HTMLDocument);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "");
                    }

                }
            }
            if (Document == null)
            {
                Log.Warning("Failed locating an Internet Explore instance");
                return new IEElement[] { };
            }

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            IEElement _fromElement = fromElement as IEElement;
            var selectors = selector.Where(x => x.Enabled == true && x.Selector == null).ToList();

            var current = new List<IEElement>();

            IEElement[] result = null;

            mshtml.IHTMLElement startfrom = null;
            if (_fromElement != null) startfrom = _fromElement.rawElement;
            if (startfrom == null) startfrom = Document.documentElement;
            current.Add(new IEElement(startfrom));
            for (var i = 2; i < selectors.Count; i++)
            {
                var s = new IESelectorItem(selectors[i]);
                var elements = new List<IEElement>();
                elements.AddRange(current);
                current.Clear();
                int failcounter = 0;
                do
                {
                    foreach (var _element in elements)
                    {
                        var matches = ((IESelectorItem)s).matches(_element.rawElement);
                        var uimatches = new List<IEElement>();
                        foreach (var m in matches)
                        {
                            var ui = new IEElement(m);
                            var list = selectors.Take(i).ToList();
                            list.Add(new IESelectorItem(m));
                            uimatches.Add(ui);
                        }

                        //result = uimatches.ToArray();
                        current.AddRange(uimatches.ToArray());
                        Log.Debug("add " + uimatches.Count + " matches to current");
                    }
                    if (current.Count == 0)
                    {
                        ++failcounter;
                        Log.Debug(string.Format("Failer # " + failcounter + " finding any hits for selector # " + i + " {0:mm\\:ss\\.fff}", sw.Elapsed));
                        foreach (var element in elements)
                        {
                            mshtml.IHTMLElementCollection children = element.rawElement.children;
                            foreach (mshtml.IHTMLElement elementNode in children) { }
                        }
                    }
                    else
                    {
                        Log.Debug(string.Format("Found " + current.Count + " hits for selector # " + i + " {0:mm\\:ss\\.fff}", sw.Elapsed));
                    }
                } while (failcounter < 2 && current.Count == 0);

                if (i == (selectors.Count - 1)) result = current.ToArray();
                if (current.Count == 0)
                {
                    var message = "needed to find " + Environment.NewLine + selectors[i].ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                    foreach (var element in elements)
                    {
                        mshtml.IHTMLElementCollection children = element.rawElement.children;
                        foreach (mshtml.IHTMLElement c in children)
                        {
                            try
                            {
                                // message += automationutil.getSelector(c, (i == selectors.Count - 1)) + Environment.NewLine;
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    Log.Debug(message);
                    return new IEElement[] { };
                }
            }
            if (result == null) return new IEElement[] { };
            Log.Debug(string.Format("GetElementsWithuiSelector::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            return result;
        }


    }
}
