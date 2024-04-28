using Newtonsoft.Json;
using OpenRPA.Interfaces;
using static OpenRPA.Interfaces.RegUtil;
using OpenRPA.NM.pipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace OpenRPA.NM
{
    public class NMHook
    {
        public static event Action<NativeMessagingMessage> onMessage;
        public static event Action<string> onDisconnected;
        public static event Action<string> Connected;
        private static NamedPipeClientAsync<NativeMessagingMessage> chromepipe = null;
        private static NamedPipeClientAsync<NativeMessagingMessage> ffpipe = null;
        private static NamedPipeClientAsync<NativeMessagingMessage> edgepipe = null;
        public const string PIPE_NAME = "openrpa_nativebridge";
        public static bool chromeconnected
        {
            get
            {
                if (chromepipe == null) return false;
                return chromepipe.isConnected;
            }
        }
        public static bool ffconnected
        {
            get
            {
                if (ffpipe == null) return false;
                return ffpipe.isConnected;
            }
        }
        public static bool edgeconnected
        {
            get
            {
                if (edgepipe == null) return false;
                return edgepipe.isConnected;
            }
        }
        public static bool connected
        {
            get
            {
                if (chromeconnected || ffconnected || edgeconnected) return true;
                return false;
            }
        }
        public static void checkForPipes(bool chrome, bool ff, bool edge)
        {
            //registreChromeNativeMessagingHost(false);
            //registreffNativeMessagingHost(false);
            if (chromepipe == null && chrome)
            {
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                chromepipe = new NamedPipeClientAsync<NativeMessagingMessage>(SessionId + "_" + PIPE_NAME + "_chrome");
                chromepipe.ServerMessage += Client_OnReceivedMessage;
                chromepipe.Disconnected += () => { onDisconnected?.Invoke("chrome"); };
                chromepipe.Connected += () => { Connected?.Invoke("chrome"); Task.Run(() => enumwindowandtabs()); };
                chromepipe.Error += (e) => { Log.Debug(e.ToString()); };
                chromepipe.Start();
            }
            if (ffpipe == null && ff)
            {
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                ffpipe = new NamedPipeClientAsync<NativeMessagingMessage>(SessionId + "_" + PIPE_NAME + "_ff");
                ffpipe.ServerMessage += Client_OnReceivedMessage;
                ffpipe.Disconnected += () => { onDisconnected?.Invoke("ff"); };
                ffpipe.Connected += () => { Connected?.Invoke("ff"); Task.Run(() => enumwindowandtabs()); };
                ffpipe.Error += (e) => { Log.Debug(e.ToString()); };
                ffpipe.Start();
            }
            if (edgepipe == null && chrome)
            {
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                edgepipe = new NamedPipeClientAsync<NativeMessagingMessage>(SessionId + "_" + PIPE_NAME + "_edge");
                edgepipe.ServerMessage += Client_OnReceivedMessage;
                edgepipe.Disconnected += () => { onDisconnected?.Invoke("edge"); };
                edgepipe.Connected += () => { Connected?.Invoke("edge"); Task.Run(() => enumwindowandtabs()); };
                edgepipe.Error += (e) => { Log.Debug(e.ToString()); };
                edgepipe.Start();
            }
        }
        public static object ExecuteScript(string browser, int frameid, int tabid, string script, TimeSpan timeout)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("executescript", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            NativeMessagingMessage result = null;
            NativeMessagingMessageTab tab = null;
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            if (tabid > -1) tab = FindTabById(browser, tabid);
            if (tab == null)
            {
                if (browser == "chrome") tab = CurrentChromeTab;
                if (browser == "ff") tab = CurrentFFTab;
                if (browser == "edge") tab = CurrentEdgeTab;
            }
            message.tab = tab;
            if (tab != null) { message.windowId = tab.windowId; message.tabid = tab.id; }
            message.browser = browser; message.frameId = frameid;
            message.script = script;
            result = sendMessageResult(message, timeout);
            if (result != null && result.error != null)
            {
                var error = result.error.ToString();
                if (!string.IsNullOrEmpty(error)) throw new ArgumentException(error);
            }
            if (result != null)
            {
                return result.result;
            }
            return null;
        }
        public static object GetTablev1(string browser, string xPath, string rowsxpath, string cellsxpath, string cellxpath, string headerrowsxpath, string headerrowxpath, int headerrowindex, bool skiptypecheck, TimeSpan timeout)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("gettablev1", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            message.xPath = xPath;
            NativeMessagingMessage result = null;
            NativeMessagingMessageTab tab = null;
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            if (tab == null)
            {
                if (browser == "chrome") tab = CurrentChromeTab;
                if (browser == "ff") tab = CurrentFFTab;
                if (browser == "edge") tab = CurrentEdgeTab;
            }
            message.tab = tab;
            if (tab != null) { message.windowId = tab.windowId; message.tabid = tab.id; }
            message.browser = browser;

            dynamic data = new System.Dynamic.ExpandoObject();
            data.rowsxpath = rowsxpath;
            data.cellsxpath = cellsxpath;
            data.cellxpath = cellxpath;
            data.headerrowsxpath = headerrowsxpath;
            data.headerrowxpath = headerrowxpath;
            data.headerrowindex = headerrowindex;
            data.skiptypecheck = skiptypecheck;
            message.data = JObject.FromObject(data).ToString();
            try
            {
                result = sendMessageResult(message, timeout);
            }
            catch (Exception)
            {
            }
            if(result ==  null || result.error != null && result.error.ToString() == "Unknown function gettablev1")
            {
                Log.Output("inject gettablev1 into page");
                var res = ExecuteScript(browser, 0, -1, "openrpautil['gettablev1'] = " + gettablev1, timeout);
                result = sendMessageResult(message, timeout);
            }
            if (result != null && result.error != null)
            {
                var error = result.error.ToString();
                if (!string.IsNullOrEmpty(error)) throw new ArgumentException(error);
            }
            if (result != null)
            {
                return result.result;
            }
            return null;
        }
        public static string gettablev1 = @"(message) => {
            var openrpadebug = false;

            let data = message.data;
            // if data is string, parse it to json
            if (typeof data === 'string') {
                data = JSON.parse(data);
            }

            if (openrpadebug) console.debug('gettablev1', data);
            const domTabe = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
            if (domTabe == null) {
                console.error('Failed locating table', message.xPath);
                const test = JSON.parse(JSON.stringify(message));
                return test;
            }
            const GetFirst = (element, xpath, prop) => {
                let value = null;
                const node = document.evaluate(xpath, element, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                if (node != null) {
                    value = node[prop];
                    if (value == null || value == '') value = '';
                    value = value.split('\r').join('').split('\t').join('').split('\n').join('').trim();
                }
                return value;
            }
            const GetFirstText = (element, xpath) => {
                return GetFirst(element, xpath, 'textContent');
            }

            let rowsxpath = data.rowsxpath && data.rowsxpath != '' ? data.rowsxpath : '';
            let cellsxpath = data.cellsxpath && data.cellsxpath != '' ? data.cellsxpath : '';
            let cellxpath = data.cellxpath && data.cellxpath != '' ? data.cellxpath : '';

            let headerrowsxpath = data.headerrowsxpath && data.headerrowsxpath != '' ? data.headerrowsxpath : '';
            let headerrowxpath = data.headerrowxpath && data.headerrowxpath != '' ? data.headerrowxpath : '';
            let headerrowindex = data.headerrowindex ? data.headerrowindex : 0;
            let skiptypecheck = data.skiptypecheck != null && data.skiptypecheck != '' ? data.skiptypecheck : false;
            let isGoogleSearch = false;

            if (domTabe.nodeName.toLowerCase() == 'table') {
                rowsxpath = rowsxpath != '' ? rowsxpath : '//tr';
                cellsxpath = cellsxpath != '' ? cellsxpath : `//*[local-name()='td' or local-name()='th']`;
                cellxpath = cellxpath != '' ? cellxpath : '';
                headerrowsxpath = headerrowsxpath != '' ? headerrowsxpath : cellsxpath;
                headerrowxpath = headerrowxpath != '' ? headerrowxpath : '';
            } else if (domTabe.nodeName.toLowerCase() == 'div') {
                // @ts-ignore
                if (domTabe.id == 'rso') {
                    isGoogleSearch = true;
                    headerrowindex = -1;
                    rowsxpath = `//div[contains(concat(' ', normalize-space(@class), ' '), ' g ')]`;
                    // rowsxpath = `/div`;
                } else {
                    if (rowsxpath == '' && GetFirstText(domTabe, `.//div[contains(@class, 'row')]`) != null) {
                        rowsxpath = `//div[contains(@class, 'row')]`;
                    } else if (rowsxpath == '' && GetFirstText(domTabe, `.//div[contains(@class, 'tableRow')]`) != null) {
                        rowsxpath = `//div[contains(@class, 'tableRow')]`;
                    } else if (rowsxpath == '' && GetFirstText(domTabe, `//div[contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'),'row')]`) != null) {
                        rowsxpath = `//div[contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'),'row')]`;
                    } else if (rowsxpath == '') {
                        console.error('Could not autodetect row class', domTabe.nodeName);
                        const test = JSON.parse(JSON.stringify(message));
                        return test;
                    }
                    console.log('rowsxpath', rowsxpath);
                    if (cellsxpath == '' && GetFirstText(domTabe, `.//div[contains(@class, 'col')]`) != null) {
                        cellsxpath = `//div[contains(@class, 'col')]`;
                    } else if (cellsxpath == '' && GetFirstText(domTabe, `.//div[contains(@class, 'cell')]`) != null) {
                        cellsxpath = `//div[contains(@class, 'tableCell')]`;
                    } else if (cellsxpath == '' && GetFirstText(domTabe, `.//div[contains(@class, 'tableCell')]`) != null) {
                        cellsxpath = `//div[contains(@class, 'tableCell')]`;
                    } else if (cellsxpath == '' && GetFirstText(domTabe, `.//*[contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'),'col')`) != null) {
                        cellsxpath = `//*[contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'),'col')`;
                    } else if (cellsxpath == '' && GetFirstText(domTabe, `.//*[contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'),'cell')`) != null) {
                        cellsxpath = `//*[contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'),'cell')`;
                    } else {
                        console.error('Could not autodetect column class', domTabe.nodeName);
                        const test = JSON.parse(JSON.stringify(message));
                        return test;
                    }
                    console.log('cellsxpath', cellsxpath);
                }
                cellxpath = cellxpath != '' ? cellxpath : '';
                headerrowsxpath = headerrowsxpath != '' ? headerrowsxpath : cellsxpath;
                headerrowxpath = headerrowxpath != '' ? headerrowxpath : '';
            } else {
                console.error('Table is of unknown type', domTabe.nodeName);
                const test = JSON.parse(JSON.stringify(message));
                return test;
            }

            if (openrpadebug) console.debug('skiptypecheck', skiptypecheck);
            const headers = [];
            const table = [];
            const isFloat = (val) => {
                const floatRegex = /^-?\d+(?:[.,]\d*?)?$/;
                if (!floatRegex.test(val))
                    return false;

                const newval = parseFloat(val);
                if (isNaN(newval))
                    return false;
                return true;
            }

            const isInt = (val) => {
                const intRegex = /^-?\d+$/;
                if (!intRegex.test(val))
                    return false;

                const intVal = parseInt(val, 10);
                return parseFloat(val) == intVal && !isNaN(intVal);
            }

            if (openrpadebug) console.debug('Working with table', domTabe);
            const query = document.evaluate('.' + rowsxpath, domTabe, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null)
            if (openrpadebug) console.debug('found ' + query.snapshotLength + ' rows using ' + rowsxpath);
            if (isGoogleSearch) {
                headers.push(['Title']);
                headers.push(['URL']);
                headers.push(['Description']);
            }
            for (let i = 0; i < query.snapshotLength; i++) {
                const row = query.snapshotItem(i)
                let subquery = null;
                if (i == headerrowindex && !isGoogleSearch) {
                    if (openrpadebug) console.debug('headers row', row);
                    if (!data.headerrowsxpath || data.headerrowsxpath == '') {
                        subquery = document.evaluate('.//th', row, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null)
                        if (subquery.snapshotLength == 0) {
                            subquery = document.evaluate('.//td', row, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null)
                        }
                    } else {
                        subquery = document.evaluate('.' + headerrowsxpath, row, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null)
                    }

                    if (openrpadebug) console.debug('headers row found ' + subquery.snapshotLength + ' cells using ' + headerrowsxpath);
                    for (let y = 0; y < subquery.snapshotLength; y++) {
                        const cel = subquery.snapshotItem(y)
                        let _name = cel.textContent;
                        if (headerrowxpath != '') {
                            _name = '';
                            let __name = GetFirstText(cel, '.' + headerrowxpath)
                            if (__name != null && __name != '') _name = __name;
                        } else {
                            let __name = GetFirstText(cel, './span')
                            if (__name == null) __name = GetFirstText(cel, './b')
                            if (__name == null) __name = GetFirstText(cel, './strong')
                            if (__name == null) __name = GetFirstText(cel, './em')
                            if (__name == null) __name = GetFirstText(cel, './/span')
                            if (__name == null) __name = GetFirstText(cel, './/b')
                            if (__name == null) __name = GetFirstText(cel, './/strong')
                            if (__name == null) __name = GetFirstText(cel, './/descendant::div[last()]')
                            if (__name == null) __name = GetFirstText(cel, './/em')
                            if (__name != null && __name != '') _name = __name;
                        }
                        if (_name == null || _name == '') _name = '';
                        _name = _name.split('\r').join('').split('\t').join('').split('\n').join('').trim()
                        if (!_name || _name == '') _name = 'cell' + (y + 1);
                        headers.push(_name);
                    }
                    if (openrpadebug) console.debug('headers', headers)
                }
                if (i <= headerrowindex) continue;
                subquery = document.evaluate('.' + cellsxpath, row, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null)
                if (openrpadebug) console.log('row', i, 'found ' + subquery.snapshotLength + ' cells using ' + cellsxpath);
                const obj = {};
                let hadvalue = false;

                if (isGoogleSearch) {
                    const title = GetFirstText(row, './/h3')
                    const url = GetFirst(row, './/a', 'href')
                    let description = GetFirstText(row, `.//span[contains(concat(' ', normalize-space(@class), ' '), ' st ')]`)
                    if (description == null) description = GetFirstText(row, `.//span[contains(concat(' ', normalize-space(@class), ' '), ' f ')]`)
                    if (description == null) description = GetFirstText(row, `.//div[@data-content-feature='1']`)
                    // if (description == null) description = GetFirstText(row, './/cite')
                    // //span[@class='f']/following-sibling::text()
                    obj['Title'] = title;
                    obj['URL'] = url;
                    obj['Description'] = description;
                    hadvalue = true;
                } else {
                    for (let y = 0; y < subquery.snapshotLength; y++) {
                        let cell = subquery.snapshotItem(y)
                        let val = cell.textContent;
                        if (cellxpath != '') {
                            val = '';
                            let __val = GetFirstText(cell, '.' + cellxpath)
                            if (__val != null && __val != '') val = __val;
                        }

                        if (!val || val == '') val = '';
                        while (val.endsWith('\n')) val = val.substring(0, val.length - 1);
                        while (val.startsWith('\n')) val = val.substring(1, val.length);
                        while (val.endsWith('\t')) val = val.substring(0, val.length - 1);
                        while (val.startsWith('\t')) val = val.substring(1, val.length);
                        val = val.trim();
                        if (!skiptypecheck) {
                            const input = document.evaluate(`.//input[@type='checkbox']`, cell, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                            if (input != null) {
                                // @ts-ignore
                                val = input.checked
                            }
                            if (isFloat(val)) {
                                val = parseFloat(val);
                            } else if (isInt(val)) {
                                val = Number.parseInt(val);
                                // is boolean 
                            } else if (val == true || val == false) {
                                val = val;
                            } else if (val && val.toLowerCase() == 'true') {
                                val = true;
                            } else if (val && val.toLowerCase() == 'false') {
                                val = false;
                            } else {
                                // xpath find input of type checkbox and then check if it is checked
                            }
                        }
                        let name = 'cell' + (y + 1);
                        if (headers.length > y) { name = headers[y]; }
                        obj[name] = val;
                        if (val != '') hadvalue = true;
                    }
                }
                if (hadvalue) table.push(obj);
            }
            console.log(table);
            message.result = table;
            const test = JSON.parse(JSON.stringify(message));
            return test;

        }";
        public static List<NativeMessagingMessageWindow> windows = new List<NativeMessagingMessageWindow>();
        public static List<NativeMessagingMessageTab> tabs = new List<NativeMessagingMessageTab>();
        public static NativeMessagingMessageWindow CurrentChromeWindow
        {
            get
            {
                var win = windows.Where(x => x != null && x.browser == "chrome" && x.focused).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x != null && x.browser == "chrome" && x.id == 1).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x != null && x.browser == "chrome").FirstOrDefault();
                return win;
            }
        }
        public static NativeMessagingMessageWindow CurrentFFWindow
        {
            get
            {
                var win = windows.Where(x => x.browser == "ff" && x.focused).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "ff" && x.id == 1).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "ff").FirstOrDefault();
                return win;
            }
        }
        public static NativeMessagingMessageWindow CurrentEdgeWindow
        {
            get
            {
                var win = windows.Where(x => x.browser == "edge" && x.focused).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "edge" && x.id == 1).FirstOrDefault();
                if (win != null) return win;
                win = windows.Where(x => x.browser == "edge").FirstOrDefault();
                return win;
            }
        }
        public static NativeMessagingMessageTab CurrentChromeTab
        {
            get
            {
                var win = CurrentChromeWindow;
                if (win == null) return null;
                return FindTabByWindowId("chrome", win.id, true);
            }
        }
        public static NativeMessagingMessageTab CurrentFFTab
        {
            get
            {
                var win = CurrentFFWindow;
                if (win == null) return null;
                return FindTabByWindowId("ff", win.id, true);
            }
        }
        public static NativeMessagingMessageTab CurrentEdgeTab
        {
            get
            {
                var win = CurrentEdgeWindow;
                if (win == null) return null;
                return FindTabByWindowId("edge", win.id, true);
            }
        }
        public static NativeMessagingMessageTab CurrentTab // this is stupid ... 
        {
            get
            {
                NativeMessagingMessageTab CurrentTab = CurrentChromeTab;
                if (CurrentTab != null) return CurrentTab;
                CurrentTab = CurrentEdgeTab;
                if (CurrentTab != null) return CurrentTab;
                CurrentTab = CurrentFFTab;
                return CurrentTab;
            }
        }
        public static NativeMessagingMessageTab GetCurrentTab(string browser)
        {
            NativeMessagingMessageTab CurrentTab = null;
            if (browser == "chrome") CurrentTab = CurrentChromeTab;
            if (browser == "edge") CurrentTab = CurrentEdgeTab;
            if (browser == "ff") CurrentTab = CurrentFFTab;
            return CurrentTab;
        }
        private static void windowcreated(NativeMessagingMessage msg)
        {
            windowremoved(msg);
            var win = new NativeMessagingMessageWindow(msg);
            windows.Add(win);
        }
        private static void windowremoved(NativeMessagingMessage msg)
        {
            var win = windows.Where(x => x.id == msg.windowId && x.browser == msg.browser).FirstOrDefault();
            if (win != null) windows.Remove(win);
        }
        private static void windowfocus(NativeMessagingMessage msg)
        {
            var win = windows.Where(x => x != null && x.id == msg.windowId && x.browser == msg.browser).FirstOrDefault();
            if (win != null)
            {
                windows.ForEach(x => x.focused = false && x.browser == msg.browser);
                Log.Debug("Selected " + msg.browser + " windows " + win.id);
                win.focused = true;
            }
        }
        private static void tabcreated(NativeMessagingMessage msg)
        {
            var tab = FindTabById(msg.browser, msg.tab.id);
            if (tab != null)
            {
                tabupdated(msg);
                return;
            }
            msg.tab.browser = msg.browser;
            if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    tabs.Add(msg.tab);
                    DetectorCheck(tab);
                }
                finally
                {
                    System.Threading.Monitor.Exit(tabs);
                }
            }
        }
        private static DateTime lastURLDetector = DateTime.Now;
        private static void DetectorCheck(NativeMessagingMessageTab tab)
        {
            try
            {
                TimeSpan ts = DateTime.Now.Subtract(lastURLDetector);
                if (ts.TotalSeconds < 2) return;
                if (tab == null || string.IsNullOrEmpty(tab.url)) return;
                URLDetectorPlugin plugin = null;
                foreach (var p in Plugins.detectorPlugins)
                {
                    if (p is URLDetectorPlugin _plugin)
                    {
                        plugin = _plugin;
                    }
                }
                if (plugin == null || string.IsNullOrEmpty(plugin.URL)) return;
                RegexOptions options = RegexOptions.None;
                if(plugin.IgnoreCase)
                {
                    options = RegexOptions.IgnoreCase;
                }
                var ma = Regex.Match(tab.url, plugin.URL, options);
                if (!ma.Success) return;
                lastURLDetector = DateTime.Now;
                var e = new URLDetectorEvent(tab.url);
                plugin.RaiseDetector(e);
                foreach (var wi in Plugin.client.WorkflowInstances.ToList())
                {
                    if (wi.isCompleted) continue;
                    if (wi.Bookmarks != null)
                    {
                        foreach (var b in wi.Bookmarks)
                        {
                            if (b.Key == "DownloadDetectorPlugin")
                            {
                                wi.ResumeBookmark(b.Key, e, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private static void tabupdated(NativeMessagingMessage msg)
        {
            var tab = FindTabById(msg.browser, msg.tab.id);
            if (tab == null)
            {
                tabcreated(msg);
                return;
            }
            tab.browser = msg.browser;
            tab.active = msg.tab.active;
            tab.audible = msg.tab.audible;
            tab.autoDiscardable = msg.tab.autoDiscardable;
            tab.discarded = msg.tab.discarded;
            tab.favIconUrl = msg.tab.favIconUrl;
            tab.height = msg.tab.height;
            tab.highlighted = msg.tab.highlighted;
            tab.incognito = msg.tab.incognito;
            tab.index = msg.tab.index;
            tab.pinned = msg.tab.pinned;
            tab.selected = msg.tab.selected;
            tab.status = msg.tab.status;
            tab.title = msg.tab.title;
            tab.url = msg.tab.url;
            tab.width = msg.tab.width;
            tab.windowId = msg.tab.windowId;
            DetectorCheck(tab);
        }
        private static void tabremoved(NativeMessagingMessage msg)
        {
            var tab = FindTabById(msg.browser, msg.tabid);
            if (tab != null)
            {
                if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
                {
                    try
                    {
                        tabs.Remove(tab);
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(tabs);
                    }
                }
            }
        }
        private static void tabactivated(NativeMessagingMessage msg)
        {
            if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    foreach (var tab in tabs.Where(x => x != null && x.browser == msg.browser && x.windowId == msg.windowId))
                    {
                        tab.highlighted = (tab.id == msg.tabid);
                        tab.selected = (tab.id == msg.tabid);
                        if (tab.highlighted)
                        {
                            Log.Debug("Selected " + msg.browser + " tab " + msg.tabid + " (" + tab.title + ")");
                        }
                    }
                }
                finally
                {
                    System.Threading.Monitor.Exit(tabs);
                }
            }
        }
        private static void downloadcomplete(NativeMessagingMessage msg)
        {
            var json = msg.data;
            var download = JsonConvert.DeserializeObject<Download>(json);
            foreach (var p in Plugins.detectorPlugins)
            {
                if (p is DownloadDetectorPlugin plugin)
                {
                    plugin.RaiseDetector(download);
                }
            }
            var e = new DetectorEvent(download);
            foreach (var wi in Plugin.client.WorkflowInstances.ToList())
            {
                if (wi.isCompleted) continue;
                if (wi.Bookmarks != null)
                {
                    foreach (var b in wi.Bookmarks)
                    {
                        if (b.Key == "DownloadDetectorPlugin")
                        {
                            wi.ResumeBookmark(b.Key, e, true);
                        }
                    }
                }
            }
        }
        private static void Client_OnReceivedMessage(NativeMessagingMessage message)
        {
            try
            {
                NativeMessagingMessage msg;
                try
                {
                    msg = message;
                    //msg = JsonConvert.DeserializeObject<NativeMessagingMessage>(e.Message);
                    if (string.IsNullOrEmpty(message.functionName) || message.functionName == "ping") return;
                }
                catch (Exception)
                {
                    return;
                }
                if (msg.functionName != "mousemove")
                {
                    Log.Verbose("[nmhook][resc][" + msg.browser + "]" + msg.functionName + " for tab " + msg.tabid + " - " + msg.messageid);
                }
                if (PluginConfig.compensate_for_old_addon)
                {
                    msg.uix -= 7;
                    msg.uiy += 7;
                }
                if (msg.functionName == "windowcreated") windowcreated(msg);
                if (msg.functionName == "windowremoved") windowremoved(msg);
                if (msg.functionName == "windowfocus") windowfocus(msg);
                if (msg.functionName == "tabcreated") tabcreated(msg);
                if (msg.functionName == "tabremoved") tabremoved(msg);
                if (msg.functionName == "tabupdated") tabupdated(msg);
                if (msg.functionName == "tabactivated") tabactivated(msg);
                if (msg.functionName == "downloadcomplete") downloadcomplete(msg);
                Task.Run(() => { onMessage?.Invoke(msg); });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        async public static Task<NativeMessagingMessage> sendMessageResultAsync(NativeMessagingMessage message, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (message.browser == "ff")
            {
                if (ffconnected)
                {
                    result = await ffpipe.MessageAsync(message, timeout);
                }
            }
            else
            {
                if (chromeconnected)
                {
                    result = await chromepipe.MessageAsync(message, timeout);
                }
            }
            return result;
        }
        public static NativeMessagingMessage sendMessageResult(NativeMessagingMessage message, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (message.browser == "ff")
            {
                if (ffconnected)
                {
                    // Log.Debug("Send and queue message " + message.functionName);
                    result = ffpipe.Message(message, timeout);
                }
            }
            else if (message.browser == "edge")
            {
                if (edgeconnected)
                {
                    // Log.Debug("Send and queue message " + message.functionName);
                    result = edgepipe.Message(message, timeout);
                }
            }
            else
            {
                if (chromeconnected)
                {
                    // Log.Debug("Send and queue message " + message.functionName);
                    result = chromepipe.Message(message, timeout);
                }
            }
            return result;
        }
        public static void enumwindows()
        {
            windows.Clear();
            NativeMessagingMessage message = new NativeMessagingMessage("enumwindows", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            if (chromeconnected)
            {
                var result = sendMessageChromeResult(message, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "windowcreated") windowcreated(msg);
                    }
            }
            if (ffconnected)
            {
                var result = sendMessageFFResult(message, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "windowcreated") windowcreated(msg);
                    }
            }
            if (edgeconnected)
            {
                var result = sendMessageEdgeResult(message, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "windowcreated") windowcreated(msg);
                    }
            }
        }
        public static void enumtabs()
        {
            if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    tabs.Clear();
                }
                finally
                {
                    System.Threading.Monitor.Exit(tabs);
                }
            }
            NativeMessagingMessage message = new NativeMessagingMessage("enumtabs", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);

            if (chromeconnected)
            {
                var result = sendMessageChromeResult(message, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "tabcreated") tabcreated(msg);
                    }
            }
            if (ffconnected)
            {
                var result = sendMessageFFResult(message, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "tabcreated") tabcreated(msg);
                    }
            }
            if (edgeconnected)
            {
                var result = sendMessageEdgeResult(message, TimeSpan.FromSeconds(3));
                if (result != null && result.results != null)
                    foreach (var msg in result.results)
                    {
                        if (msg.functionName == "tabcreated") tabcreated(msg);
                    }
            }
        }
        public static void enumwindowandtabs()
        {
            enumwindows();
            enumtabs();
        }
        public static void CloseTab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            NativeMessagingMessage result = null;
            message.browser = tab.browser; message.tabid = tab.id; message.tab = tab;
            message.windowId = tab.windowId;
            if (connected)
            {
                result = sendMessageResult(message, PluginConfig.protocol_timeout);
            }
        }
        //public static void HighlightTab(NativeMessagingMessageTab tab)
        //{
        //    if (!tab.highlighted)
        //    {
        //        tab.highlighted = true;
        //        UpdateTab(tab);
        //    }
        //}
        public static void openurl(string browser, string url, bool newtab, string profile, string profilepath)
        {
            if (browser == "chrome")
            {
                int tabcount = 0;
                if (chromeconnected)
                {
                    if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
                    {
                        try
                        {
                            tabcount = tabs.Where(x => x != null && x.browser == "chrome").Count();
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(tabs);
                        }
                    }                    
                }
                if (!chromeconnected || tabcount == 0)
                {
                    if (string.IsNullOrEmpty(profilepath))
                    {
                        System.Diagnostics.Process.Start("chrome.exe", "\"" + url + "\"");
                    }
                    else
                    {
                        System.Diagnostics.Process.Start("chrome.exe", "--user-data-dir=\"" + profilepath + "\"  \"" + url + "\"");
                    }

                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    do
                    {
                        System.Threading.Thread.Sleep(500);
                        Console.WriteLine("pending chrome addon to connect");
                    } while (sw.Elapsed < TimeSpan.FromSeconds(20) && !chromeconnected);
                }
                else
                {
                    chromeopenurl(url, newtab);
                }
            }
            else if (browser == "edge")
            {
                int tabcount = 0;
                if (edgeconnected)
                {
                    if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
                    {
                        try
                        {
                            tabcount = tabs.Where(x => x != null && x.browser == "edge").Count();
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(tabs);
                        }
                    }
                }                
                if (!edgeconnected || tabcount == 0)
                {
                    if (string.IsNullOrEmpty(profilepath))
                    {
                        // System.Diagnostics.Process.Start("microsoft-edge:" + url);
                        System.Diagnostics.Process.Start("msedge.exe", "\"" + url + "\"");
                    }
                    else
                    {
                        System.Diagnostics.Process.Start("msedge.exe", "--user-data-dir=\"" + profilepath + "\" \"" + url + "\"");
                    }

                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    do
                    {
                        System.Threading.Thread.Sleep(500);
                        Console.WriteLine("pending edge addon to connect");
                    } while (sw.Elapsed < TimeSpan.FromSeconds(20) && !edgeconnected);
                }
                else
                {
                    edgeopenurl(url, newtab);
                }
            }
            else
            {
                int tabcount = 0;
                if (ffconnected)
                {
                    if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
                    {
                        try
                        {
                            tabcount = tabs.Where(x => x != null && x.browser == "ff").Count();
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(tabs);
                        }
                    }
                }
                if (!ffconnected || tabcount == 0)
                {
                    if (string.IsNullOrEmpty(profilepath))
                    {
                        System.Diagnostics.Process.Start("firefox.exe", "\"" + url + "\"");
                    }
                    else
                    {
                        var p = System.Diagnostics.Process.Start("firefox.exe", "-no-remote -CreateProfile \"" + profile + "\" \"" + profilepath + "\" \"" + url + "\"");
                        p.WaitForExit(10000);
                        System.Diagnostics.Process.Start("firefox.exe", "-no-remote -P \"" + profile + "\" \"" + url + "\"");
                        // -no-remote -profile -p -CreateProfile
                    }

                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    do
                    {
                        System.Threading.Thread.Sleep(500);
                        Console.WriteLine("pending ff addon to connect");
                    } while (sw.Elapsed < TimeSpan.FromSeconds(20) && !ffconnected);

                }
                else
                {
                    ffopenurl(url, newtab);
                }

            }
        }
        internal static void ffopenurl(string url, bool forceNew)
        {
            if (ffconnected)
            {
                NativeMessagingMessage message = new NativeMessagingMessage("openurl", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { data = url };
                message.xPath = forceNew.ToString().ToLower();
                var result = ffpipe.Message(message, TimeSpan.FromSeconds(2));
                if (result != null && result.tab != null) WaitForTab(result.tab.id, result.browser, TimeSpan.FromSeconds(5));
            }
        }
        internal static void chromeopenurl(string url, bool forceNew)
        {
            if (chromeconnected)
            {
                NativeMessagingMessage message = new NativeMessagingMessage("openurl", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { data = url };
                message.xPath = forceNew.ToString().ToLower();
                // Log.Verbose("send openurl");
                var result = chromepipe.Message(message, TimeSpan.FromSeconds(2));
                // Log.Verbose("openurl reply, wait for tab #" + result.tab.id + " windowId " + result.tab.windowId);
                if (result != null && result.tab != null) WaitForTab(result.tab.id, result.browser, TimeSpan.FromSeconds(5));
            }
        }
        internal static void edgeopenurl(string url, bool forceNew)
        {
            if (edgeconnected)
            {
                NativeMessagingMessage message = new NativeMessagingMessage("openurl", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { data = url };
                message.xPath = forceNew.ToString().ToLower();
                var result = edgepipe.Message(message, TimeSpan.FromSeconds(2));
                if (result != null && result.tab != null) WaitForTab(result.tab.id, result.browser, TimeSpan.FromSeconds(5));
            }
        }
        public static NMElement[] getElement(int tabid, long frameId, string browser, string xPath, TimeSpan timeout)
        {
            var results = new List<NMElement>();
            var getelement = new NativeMessagingMessage("getelement", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);
            getelement.browser = browser;
            getelement.tabid = tabid;
            getelement.xPath = xPath;
            getelement.frameId = frameId;
            NativeMessagingMessage result = null;
            try
            {
                result = NMHook.sendMessageResult(getelement, timeout);
            }
            catch (Exception)
            {
            }
            if (result != null && result.result != null && result.results == null)
            {
                result.results = new NativeMessagingMessage[] { result };
            }
            if (result != null && result.results != null && result.results.Count() > 0)
            {
                foreach (var res in result.results)
                {
                    if (res.result != null)
                    {
                        //var html = new HtmlElement(getelement.xPath, getelement.cssPath, res.tabid, res.frameId, res.result);
                        res.tab = FindTabById(res.browser, res.tabid);
                        var html = new NMElement(res);
                        results.Add(html);
                    }
                }
                //result = result.results[0];
            }
            return results.ToArray();
        }
        public static void WaitForTab(int tabid, string browser, TimeSpan timeout, DateTime? lastready = null)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            enumtabs();
            var tab = FindTabById(browser, tabid);
            do
            {
                if (tab != null)
                {
                    // Log.Verbose("WaitForTab: " + tabid + " " + tab.status);
                }
                else
                {
                    // Log.Verbose("WaitForTab, failed locating tab: " + tabid);
                    enumtabs();
                }
                System.Threading.Thread.Sleep(PluginConfig.wait_for_tab_timeout);
                tab = FindTabById(browser, tabid);
            } while (tab != null && tab.status != "ready" && tab.status != "complete" && sw.Elapsed < timeout);
            return;
        }
        public static NativeMessagingMessage sendMessageChromeResult(NativeMessagingMessage message, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (chromeconnected)
            {
                result = chromepipe.Message(message, timeout);
            }
            return result;
        }
        public static NativeMessagingMessage sendMessageFFResult(NativeMessagingMessage message, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (ffconnected)
            {
                result = ffpipe.Message(message, timeout);
            }
            return result;
        }
        public static NativeMessagingMessage sendMessageEdgeResult(NativeMessagingMessage message, TimeSpan timeout)
        {
            NativeMessagingMessage result = null;
            if (edgeconnected)
            {
                result = edgepipe.Message(message, timeout);
            }
            return result;
        }
        public static void registreChromeNativeMessagingHost(bool localMachine)
        {
            try
            {
                if (localMachine)
                {
                    if (!hklmExists(@"SOFTWARE\Google")) return;
                    if (!hklmExists(@"SOFTWARE\Google\Chrome")) return;
                    if (!hklmExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                    if (!hklmExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.openrpa.msg")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.openrpa.msg");
                }
                else
                {
                    if (!hkcuExists(@"SOFTWARE\Google")) return;
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome")) return;
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.openrpa.msg")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.openrpa.msg");
                }
                var basepath = Interfaces.Extensions.PluginsDirectory;
                var filename = System.IO.Path.Combine(basepath, "chromemanifest.json");
                if (!System.IO.File.Exists(filename)) return;
                string json = System.IO.File.ReadAllText(filename);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);
                jsonObj["path"] = System.IO.Path.Combine(basepath, "OpenRPA.NativeMessagingHost.exe");
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                try
                {
                    System.IO.File.WriteAllText(filename, output);
                }
                catch (Exception)
                {
                }
                Microsoft.Win32.RegistryKey Chrome = null;
                if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.openrpa.msg", true);
                if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.openrpa.msg", true);
                Chrome.SetValue("", filename);


                //if (localMachine)
                //{
                //    if (!hklmExists(@"SOFTWARE\Policies")) return;
                //    if (!hklmExists(@"SOFTWARE\Policies\Google")) hklmCreate(@"SOFTWARE\Policies\Google");
                //    if (!hklmExists(@"SOFTWARE\Policies\Google\Chrome")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                //    if (!hklmExists(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist")) hklmCreate(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist");
                //    if (!hklmExists(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist")) hklmCreate(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist");

                //    Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist", true);
                //    var names = rk.GetSubKeyNames();
                //    string id = null;
                //    foreach (var name in names)
                //    {
                //        var value = rk.GetValue(name);
                //        if (value != null && value.ToString() == "hpnihnhlcnfejboocnckgchjdofeaphe") id = name;
                //    }
                //    if (string.IsNullOrEmpty(id))
                //    {
                //        rk.SetValue((names.Length + 1).ToString(), "hpnihnhlcnfejboocnckgchjdofeaphe");
                //    }
                //    rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", true);
                //    names = rk.GetSubKeyNames();
                //    id = null;
                //    foreach (var name in names)
                //    {
                //        var value = rk.GetValue(name);
                //        if (value != null && value.ToString() == "hpnihnhlcnfejboocnckgchjdofeaphe") id = name;
                //    }
                //    if (string.IsNullOrEmpty(id))
                //    {
                //        rk.SetValue((names.Length + 1).ToString(), "hpnihnhlcnfejboocnckgchjdofeaphe");
                //    }

                //}
                //else
                //{
                //    if (!hkcuExists(@"SOFTWARE\Policies")) return;
                //    if (!hkcuExists(@"SOFTWARE\Policies\Google")) hkcuCreate(@"SOFTWARE\Policies\Google");
                //    if (!hkcuExists(@"SOFTWARE\Policies\Google\Chrome")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                //    if (!hkcuExists(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist")) hkcuCreate(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist");
                //    if (!hkcuExists(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist")) hkcuCreate(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist");

                //    Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist", true);
                //    var names = rk.GetSubKeyNames();
                //    string id = null;
                //    foreach (var name in names)
                //    {
                //        var value = rk.GetValue(name);
                //        if (value != null && value.ToString() == "hpnihnhlcnfejboocnckgchjdofeaphe") id = name;
                //    }
                //    if (string.IsNullOrEmpty(id))
                //    {
                //        rk.SetValue((names.Length + 1).ToString(), "hpnihnhlcnfejboocnckgchjdofeaphe");
                //    }
                //    rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", true);
                //    names = rk.GetSubKeyNames();
                //    id = null;
                //    foreach (var name in names)
                //    {
                //        var value = rk.GetValue(name);
                //        if (value != null && value.ToString() == "hpnihnhlcnfejboocnckgchjdofeaphe") id = name;
                //    }
                //    if (string.IsNullOrEmpty(id))
                //    {
                //        rk.SetValue((names.Length + 1).ToString(), "hpnihnhlcnfejboocnckgchjdofeaphe");
                //    }

                //}

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static void registreffNativeMessagingHost(bool localMachine)
        {
            try
            {
                if (localMachine)
                {
                    if (!hklmExists(@"Software\Mozilla")) return;
                    if (!hklmExists(@"SOFTWARE\Mozilla\NativeMessagingHosts")) hklmCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts");
                    if (!hklmExists(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.openrpa.msg")) hklmCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.openrpa.msg");
                }
                else
                {
                    if (!hkcuExists(@"SOFTWARE\Mozilla")) return;
                    if (!hkcuExists(@"SOFTWARE\Mozilla\NativeMessagingHosts")) hkcuCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts");
                    if (!hkcuExists(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.openrpa.msg")) hkcuCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.openrpa.msg");
                }
                var basepath = Interfaces.Extensions.PluginsDirectory;
                var filename = System.IO.Path.Combine(basepath, "ffmanifest.json");
                if (!System.IO.File.Exists(filename)) return;
                string json = System.IO.File.ReadAllText(filename);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);
                jsonObj["path"] = System.IO.Path.Combine(basepath, "OpenRPA.NativeMessagingHost.exe");
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                try
                {
                    System.IO.File.WriteAllText(filename, output);
                }
                catch (Exception)
                {
                }
                Microsoft.Win32.RegistryKey Chrome = null;
                if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.openrpa.msg", true);
                if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.openrpa.msg", true);
                Chrome.SetValue("", filename);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static NativeMessagingMessageTab updatetab(string browser, NativeMessagingMessageTab tab)
        {
            if (browser == "chrome")
            {
                if (chromeconnected)
                {
                    return chromeupdatetab(tab);
                }
            }
            else if (browser == "edge")
            {
                if (edgeconnected)
                {
                    return edgeupdatetab(tab);
                }
            }
            else
            {
                if (ffconnected)
                {
                    return ffupdatetab(tab);
                }
            }
            return null;
        }
        internal static NativeMessagingMessageTab ffupdatetab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("updatetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tab.id, tab = tab };
            NativeMessagingMessage result = ffpipe.Message(message, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("ff", tab.id);
        }
        internal static NativeMessagingMessageTab chromeupdatetab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("updatetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tab.id, tab = tab };
            NativeMessagingMessage result = chromepipe.Message(message, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("chrome", tab.id);
        }
        internal static NativeMessagingMessageTab edgeupdatetab(NativeMessagingMessageTab tab)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("updatetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tab.id, tab = tab };
            NativeMessagingMessage result = edgepipe.Message(message, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("edge", tab.id);
        }
        public static NativeMessagingMessageTab selecttab(string browser, int tabid)
        {
            if (browser == "chrome")
            {
                if (chromeconnected)
                {
                    return chromeselecttab(tabid);
                }
            }
            else if (browser == "edge")
            {
                if (edgeconnected)
                {
                    return edgeselecttab(tabid);
                }
            }
            else
            {
                if (ffconnected)
                {
                    return ffselecttab(tabid);
                }
            }
            return null;
        }
        internal static NativeMessagingMessageTab ffselecttab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("selecttab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = ffpipe.Message(message, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("ff", tabid);
        }
        internal static NativeMessagingMessageTab chromeselecttab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("selecttab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = chromepipe.Message(message, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("chrome", tabid);
        }
        internal static NativeMessagingMessageTab edgeselecttab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("selecttab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = edgepipe.Message(message, TimeSpan.FromSeconds(2));
            WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
            return FindTabById("edge", tabid);
        }
        public static NativeMessagingMessageTab FindTabByURL(string browser, string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            NativeMessagingMessageTab CurrentTab = null;
            if (browser == "chrome") CurrentTab = CurrentChromeTab;
            if (browser == "edge") CurrentTab = CurrentEdgeTab;
            if (browser == "ff") CurrentTab = CurrentFFTab;
            if (CurrentTab != null && !string.IsNullOrEmpty(CurrentTab.url) && CurrentTab.url.ToLower().StartsWith(url.ToLower())) return CurrentTab;
            if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    CurrentTab = tabs.Where(x => x != null && x.browser == browser && !string.IsNullOrEmpty(x.url) && x.url.ToLower().StartsWith(url.ToLower())).FirstOrDefault();
                }
                finally
                {
                    System.Threading.Monitor.Exit(tabs);
                }
            }
            return CurrentTab;
        }
        public static NativeMessagingMessageTab FindTabById(string browser, int id)
        {
            NativeMessagingMessageTab CurrentTab = null;
            if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    CurrentTab = tabs.Where(x => x != null && x.browser == browser && x.id == id).FirstOrDefault();
                }
                finally
                {
                    System.Threading.Monitor.Exit(tabs);
                }
            }
            return CurrentTab;
        }
        public static NativeMessagingMessageTab FindTabByWindowId(string browser, int windowId, bool isSelectéd)
        {
            if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    if (isSelectéd)
                    {
                        // (x.selected || x.highlighted) ?
                        return tabs.Where(x => x != null && x.browser == browser && x.windowId == windowId && x.selected).FirstOrDefault();
                    }
                    else
                    {
                        return tabs.Where(x => x != null && x.browser == browser && x.windowId == windowId).FirstOrDefault();
                    }
                }
                finally
                {
                    System.Threading.Monitor.Exit(tabs);
                }
            }
            return null;
        }
        public static void CloseTab(string browser, int tabid)
        {
            if (browser == "chrome")
            {
                if (chromeconnected)
                {
                    chromeclosetab(tabid);
                }
            }
            else if (browser == "edge")
            {
                if (edgeconnected)
                {
                    edgeclosetab(tabid);
                }
            }
            else
            {
                if (ffconnected)
                {
                    ffclosetab(tabid);
                }
            }
        }
        public static void CloseAllTabs(string browser)
        {
            List<NativeMessagingMessageTab> _tabs = null;
            if (System.Threading.Monitor.TryEnter(tabs, Config.local.thread_lock_timeout_seconds * 1000))
            {
                try
                {
                    _tabs = tabs.Where(x => x != null && x.browser == browser).ToList();
                }
                finally
                {
                    System.Threading.Monitor.Exit(tabs);
                }
            }
            if(_tabs != null)
                foreach (var tab in _tabs)
                {
                    NMHook.CloseTab(browser, tab.id);
                }
        }
        internal static void ffclosetab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = ffpipe.Message(message, TimeSpan.FromSeconds(2));
            // if (result != null) WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
        }
        internal static void chromeclosetab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = chromepipe.Message(message, TimeSpan.FromSeconds(2));
            // if (result != null) WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(10));
        }
        internal static void edgeclosetab(int tabid)
        {
            NativeMessagingMessage message = new NativeMessagingMessage("closetab", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids) { tabid = tabid };
            NativeMessagingMessage result = edgepipe.Message(message, TimeSpan.FromSeconds(2));
            // if (result != null) WaitForTab(result.tabid, result.browser, TimeSpan.FromSeconds(5));
        }
    }
}
