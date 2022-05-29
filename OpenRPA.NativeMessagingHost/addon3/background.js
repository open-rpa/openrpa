var port = null;
var portname = 'com.openrpa.msg';
var base_debug = false;
function BaseOnPortMessage(message) {
    if (port == null) {
        console.warn("BaseOnPortMessage: port is null!");
        console.log(message);
        return;
    }
    if (message === null || message === undefined) {
        console.warn("BaseOnPortMessage: Received null message!");
        return;
    }
    if (message.functionName === "ping") return;
    if (base_debug) console.log("[baseresc][" + message.messageid + "]" + message.functionName);
    if (message.functionName === "backgroundscript") {
        try {
            message.result = "ok";
        } catch (e) {
            console.error(e);
            message.result = e;
        }
        return;
    }
};
function BaseOnPortDisconnect(message) {
    if (base_debug) console.log("BaseOnPortDisconnect: " + message);
    port = null;
    if (chrome.runtime.lastError) {
        console.warn("BaseOnPortDisconnect: " + chrome.runtime.lastError.message);
        port = null;
        setTimeout(function () {
            Baseconnect();
        }, 1000);
        return;
    } else {
        if (base_debug) console.log("BaseOnPortDisconnect from native port");
    }
}
function Baseconnect() {
    if (base_debug) console.log("Baseconnect()");
    if (port !== null && port !== undefined) {
        try {
            if(port!=null) port.onMessage.removeListener(BaseOnPortMessage);
            if(port!=null) port.onDisconnect.removeListener(BaseOnPortDisconnect);
        } catch (e) {
            console.log(e);
        }
    }
    if (port === null || port === undefined) {
        try {
            console.log("Connecting to " + portname);
            port = chrome.runtime.connectNative(portname);
        } catch (e) {
            console.error(e);
            port = null;
            return;
        }
    }
    if(port!=null) port.onMessage.addListener(BaseOnPortMessage);
    if(port!=null) port.onDisconnect.addListener(BaseOnPortDisconnect);

    if (chrome.runtime.lastError) {
        console.warn("Whoops.. " + chrome.runtime.lastError.message);
        port = null;
        return;
    } else {
        if (base_debug) console.log("Connected to native port, request backgroundscript script");
    }

}
Baseconnect();


console.log('openrpa extension begin');
//var port;
var openrpautil_script = '';
var portname = 'com.openrpa.msg';
var lastwindowId = 1;
var openrpadebug = false;

// Opera 8.0+ (tested on Opera 42.0)

// Firefox 1.0+ (tested on Firefox 45 - 53)
var isFirefox = typeof InstallTrigger !== 'undefined';

// Internet Explorer 6-11
//   Untested on IE (of course). Here because it shows some logic for isEdge.

// Edge 20+ (tested on Edge 38.14393.0.0)
var isChromeEdge = navigator.appVersion.indexOf('Edge') > -1;
if (!isChromeEdge) isChromeEdge = navigator.appVersion.indexOf('Edg') > -1;

var isChrome = !isChromeEdge && !isFirefox;

// Chrome 1+ (tested on Chrome 55.0.2883.87)
// This does not work in an extension:
// The other browsers are trying to be more like Chrome, so picking
// capabilities which are in Chrome, but not in others is a moving
// target.  Just default to Chrome if none of the others is detected.

// Blink engine detection (tested on Chrome 55.0.2883.87 and Opera 42.0)

/* The above code is based on code from: https://stackoverflow.com/a/9851769/3773011 */

// var port = null;
var portname = 'com.openrpa.msg';

async function SendToTab(windowId, message) {
    try {
        var retry = false;
        try {
            console.debug("SendToTab: send message to tab id " + message.tabid + " windowId " + windowId);
            message = await tabssendMessage(message.tabid, message);
        } catch (e) {
            console.debug('tabssendMessage failed once');
            retry = true;
            console.warn(e);
        }
        if (retry) {
            await new Promise(r => setTimeout(r, 2000));
            console.debug("retry: SendToTab: send message to tab id " + message.tabid + " windowId " + windowId);
            message = await tabssendMessage(message.tabid, message);
        }

        var allWindows = await windowsgetAll();
        var win = allWindows.filter(x => x.id == windowId);
        var currentWindow = allWindows[0];
        if (win.length > 0) currentWindow = win[0];
        if (message.uix && message.uiy) {
            message.uix += currentWindow.left;
            message.uiy += currentWindow.top;
        }
        if (message.results) {
            if (Array.isArray(message.results)) {
                message.results.forEach((item) => {
                    if (item.uix && item.uiy) {
                        item.uix += currentWindow.left;
                        item.uiy += currentWindow.top;
                    }
                    item.windowId = windowId;
                    item.tabid = message.tabid;
                });
            } else {
                delete message.results;
            }
        }

    } catch (e) {
        console.error(e);
        message.error = JSON.stringify(e);
    }
    return message;
}
async function OnPortMessage(message) {
    try {
        if (port == null) {
            console.warn("OnPortMessage: port is null!", message);
            return;
        }
        if (message === null || message === undefined) {
            console.warn("OnPortMessage: Received null message!");
            return;
        }
        if (message.functionName === "ping") {
            return;
        }
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        if (isChromeEdge) message.browser = "edge";
        console.log("[resc][" + message.messageid + "]" + message.functionName);
        if (message.functionName === "openrpautilscript") {
            return;
        }
        if (message.functionName === "contentscript") {
            return;
        }

        if (message.functionName === "enumwindows") {
            await EnumWindows(message);
            console.log("[send][" + message.messageid + "]" + message.functionName + " results: " + message.results.length);
            if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        if (message.functionName === "enumtabs") {
            await EnumTabs(message);
            var _result = "";
            message.results = message.results.filter(x => x.type != "popup" && x.state != "minimized");
            for (var i = 0; i < message.results.length; i++) {
                var _tab = message.results[i].tab;
                _result += "(tabid " + _tab.id + "/index:" + _tab.index + ") ";
            }
            console.log("[send][" + message.messageid + "]" + message.functionName + " results: " + message.results.length + " " + _result);
            if (port != null) port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        if (message.functionName === "selecttab") {
            var tab = null;
            var tabsList = await tabsquery();
            if (tabsList.length == 0) {
                message.error = "selecttab, No tabs found!";
                if (port != null) port.postMessage(JSON.parse(JSON.stringify(message)));
                return;
            }
            for (var i = 0; i < tabsList.length; i++) {
                if (tabsList[i].id == message.tabid) {
                    tab = tabsList[i];
                }
            }
            if (tab == null) {
                message.error = "Tab " + message.tabid + " not found!";
                if (port != null) port.postMessage(JSON.parse(JSON.stringify(message)));
                return;
            }
            await tabshighlight(tab.index);
            message.tab = tab;
            // message.tab = await tabsupdate(message.tabid, { highlighted: true });
            console.log("[send][" + message.messageid + "]" + message.functionName + " " + message.tab.url);
            if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        if (message.functionName === "updatetab") {
            var tabsList = await tabsquery();
            if (tabsList.length == 0) {
                message.error = "updatetab, No tabs found!";
                if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
                return;
            }
            for (var i = 0; i < tabsList.length; i++) {
                if (tabsList[i].id == message.tabid) {
                    if (tabsList[i].url == message.tab.url) {
                        delete message.tab.url;
                    }
                }
            }

            delete message.tab.browser;
            delete message.tab.audible;
            delete message.tab.discarded;
            delete message.tab.favIconUrl;
            delete message.tab.height;
            delete message.tab.id;
            delete message.tab.incognito;
            delete message.tab.status;
            delete message.tab.title;
            delete message.tab.width;
            delete message.tab.index;
            delete message.tab.windowId;
            message.tab = await tabsupdate(message.tabid, message.tab);
            console.log("[send][" + message.messageid + "]" + message.functionName + " " + message.tab.url);
            if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        if (message.functionName === "closetab") {
            chrome.tabs.remove(message.tabid);
            if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        if (message.functionName === "openurl") {
            var windows = await windowsgetAll();
            console.log("openurl windowid" + message.windowId + " tabid: " + message.tabid, message);
            console.log("windows", windows);
            if (message.xPath == "true") {
                var createProperties = { url: message.data };
                var w = await getCurrentWindow();
                console.log("WINDOW", w);
                createProperties.windowId = w.id;
                console.log("openurl, open NEW in window " + w.id);
                //if (message.windowId !== null && message.windowId !== undefined && message.windowId > 0) createProperties.windowId = message.windowId;
                var newtab = await tabscreate(createProperties);
                message.tab = newtab;
                message.tabid = newtab.id;
                console.log("[send][" + message.messageid + "]" + message.functionName);
                if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
                return;
            }
            if (message.windowId == -1) {
                var w = await getCurrentWindow();
                message.windowId = w.id;
            } else {
                windows = windows.filter(x => x.type != "popup" && x.state != "minimized");
                var win = windows.filter(x => x.id == message.windowId);
                if (win.length == 0) {
                    var w = await getCurrentWindow();
                    message.windowId = w.id;
                }
            }
            var tabsList = await tabsquery({ windowId: message.windowId });
            if (message.windowId !== null && message.windowId !== undefined && message.windowId !== '' && message.windowId > 0) {
                tabsList = tabsList.filter(x => x.windowId == message.windowId);
            } else {
                tabsList = tabsList.filter(x => x.windowId == lastwindowId);
            }
            if (message.tabid !== null && message.tabid !== undefined && message.tabid !== '' && message.tabid > 0) {
                tabsList = tabsList.filter(x => x.id == message.tabid);
            } else {
                tabsList = tabsList.filter(x => x.active == true);
            }
            if (tabsList.length == 0) {
                var w = await getCurrentWindow();
                // tabsList = await tabsquery({ windowId: w.id });
                var createProperties = { url: message.data };
                createProperties.windowId = w.id;
                console.log("openurl, open NEW 2 in window " + w.id);
                var newtab = await tabscreate(createProperties);
                console.log(newtab);
                message.tab = newtab;
                message.tabid = newtab.id;
                console.log("[send][" + message.messageid + "]" + message.functionName);
                if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
                return;
                // message.error = "openurl, No tabs found!";
                // if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
                // return;
            }
            var tab = tabsList[0];
            if (tab.url != message.data) {
                var updateoptions = { active: true, highlighted: true };
                updateoptions.url = message.data;
                console.log("openurl, update tab #" + tab.id, tab);
                lastwindowId = tab.windowId;
                tab = await tabsupdate(tab.id, updateoptions);
            } else {
                var updateoptions = { active: true, highlighted: true };
                updateoptions.url = message.data;
                console.log("openurl, tab #" + tab.id + " windowId " + tab.windowId + " is already on the right url", tab);
                lastwindowId = tab.windowId;
                tab = await tabsupdate(tab.id, updateoptions);
            }
            message.tab = tab;
            message.tabid = tab.id;
            console.log("[send][" + message.messageid + "]" + message.functionName);
            if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        console.log("SendToTab before tab #" + message.tabid + " windowId " + message.windowId, tab);
        var windowId = lastwindowId;
        var tabsList = await tabsquery();
        if (message.windowId !== null && message.windowId !== undefined && message.windowId !== '' && message.windowId > 0) {
            windowId = message.windowId;
            tabsList = tabsList.filter(x => x.windowId == message.windowId);
        } else {
            var templist = tabsList.filter(x => x.windowId == lastwindowId);
            if (templist.length > 0) {
                windowId = lastwindowId;
                tabsList = tabsList.filter(x => x.windowId == lastwindowId);
            }
        }
        if (message.tabid !== null && message.tabid !== undefined && message.tabid !== '' && message.tabid > 0) {
            tabsList = tabsList.filter(x => x.id == message.tabid);
        } else {
            tabsList = tabsList.filter(x => x.active == true);
        }
        if (tabsList.length == 0) {
            var w = await getCurrentWindow();
            tabsList = await tabsquery({ windowId: w.id });
            if (tabsList.length == 0) {
                message.error = "SendToTab, No tabs found! windowId " + message.windowId + " lastwindowId: " + lastwindowId;
                console.log("[send][" + message.messageid + "]" + message.functionName + " No tabs found");
                if (port != null) port.postMessage(JSON.parse(JSON.stringify(message)));
                return;
            }
            windowId = w.id;
        }
        var tab = tabsList[0];
        console.log("SendToTab tab #" + tab.id + " windowid " + tab.windowId, tab);
        message.windowId = windowId;
        message.tabid = tab.id;
        openrpadebug = message.debug;

        if (message.functionName === "executescript") {
            console.log("executescript tab #" + message.tabid + " frameId: " + message.frameId, message);
            // var script = "(" + message.script + ")()";
            var script = message.script;

            var detatch = false;
            try {
                console.log("attach to " + message.tabid);
                await debuggerattach(message.tabid);
                detatch = true;
                console.log("eval", message.script);
                message.result = await debuggerEvaluate(message.tabid, script);
                if (message.result && message.result.result && message.result.result.value) {
                    message.result = [message.result.result.value];
                } else if (!Array.isArray(message.result)) {
                    message.result = [message.result];
                }

                console.log("result", message.result);
                // message.results = [message.result];
            } catch (e) {
                console.error(e);
                message.error = e.message;
            }
            if (detatch) {
                try {
                    console.log("detach to " + message.tabid);
                    await debuggerdetach(message.tabid);
                } catch (e) {
                    console.error(e);
                }
            }

            console.log("[send][" + message.messageid + "]" + message.functionName);
            port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }

        //if (message.functionName === "executescript") {
        //    console.log("executescript tab #" + message.tabid + " frameId: " + message.frameId, message);

        //    var detatch = false;
        //    try {
        //        console.log("attach to " + message.tabid);
        //        await debuggerattach(message.tabid);
        //        detatch = true;
        //        console.log("eval", message.script);
        //        message.result = await debuggerEvaluate(message.tabid, message.script);
        //        if (message.result && message.result.result && message.result.result.value) {
        //            message.result = message.result.result.value;
        //        }

        //        console.log("result", message.result);
        //        message.results = [message.result];
        //    } catch (e) {
        //        console.error(e);
        //        message.error = e.message;
        //    }
        //    if (detatch) {
        //        try {
        //            console.log("detach to " + message.tabid);
        //            await debuggerdetach(message.tabid);
        //        } catch (e) {
        //            console.error(e);
        //        }
        //    }
        //    console.log("[send][" + message.messageid + "]" + message.functionName);
        //    if (port != null) port.postMessage(JSON.parse(JSON.stringify(message)));
        //    return;
        //}

        message = await SendToTab(windowId, message);

    } catch (e) {
        console.error(e);
        message.error = JSON.stringify(e);
    }
    console.log("[send][" + message.messageid + "]" + message.functionName);
    if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
    console.debug(message);
};
function OnPortDisconnect(message) {
    console.log("OnPortDisconnect", message);
    port = null;
    if (chrome.runtime.lastError) {
        console.warn("onDisconnect: " + chrome.runtime.lastError.message);
        port = null;
        setTimeout(function () {
            connect();
        }, 1000);
        return;
    } else {
        console.log("onDisconnect from native port");
    }
}
function connect() {
    console.log("connect()");
    if (port !== null && port !== undefined) {
        try {
            if(port!=null) port.onMessage.removeListener(OnPortMessage);
            if(port!=null) port.onDisconnect.removeListener(OnPortDisconnect);
        } catch (e) {
            console.error(e);
        }
    }
    if (port === null || port === undefined) {
        try {
            console.log("Connecting to " + portname);
            port = chrome.runtime.connectNative(portname);
        } catch (e) {
            console.error(e);
            port = null;
            return;
        }
    }
    if(port!=null) port.onMessage.addListener(OnPortMessage);
    if(port!=null) port.onDisconnect.addListener(OnPortDisconnect);

    if (chrome.runtime.lastError) {
        console.warn("Whoops.. " + chrome.runtime.lastError.message);
        port = null;
        return;
    } else {
        console.log("Connected to native port");
    }
}
async function EnumTabs(message) {
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    message.results = [];
    var tabsList = await tabsquery();
    tabsList.forEach((tab) => {
        var _message = { functionName: "tabcreated", tabid: tab.id, tab: tab };
        if (isChrome) _message.browser = "chrome";
        if (isFirefox) _message.browser = "ff";
        if (isChromeEdge) _message.browser = "edge";
        message.results.push(_message);
    });
}
async function EnumWindows(message) {
    var allWindows = await windowsgetAll();
    message.results = [];
    for (var i in allWindows) {
        var window = allWindows[i];
        var _message = { functionName: "windowcreated", windowId: window.id, result: JSON.stringify(window) };
        if (isChrome) _message.browser = "chrome";
        if (isFirefox) _message.browser = "ff";
        if (isChromeEdge) _message.browser = "edge";
        message.results.push(_message);
    }
}
async function OnPageLoad(event) {
    // if (window) window.removeEventListener("load", OnPageLoad, false);
    chrome.windows.onCreated.addListener((window) => {
        if (window.type === "normal" || window.type === "popup") { // panel
            if (window.type === "popup" && window.state == "minimized") return;
            if (window.id > 0) lastwindowId = window.id;
            var message = { functionName: "windowcreated", windowId: window.id, result: JSON.stringify(window) };
            if (isChrome) message.browser = "chrome";
            if (isFirefox) message.browser = "ff";
            if (isChromeEdge) message.browser = "edge";
            console.debug("[send]" + message.functionName + " " + window.id);
            if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
        }
    });
    chrome.windows.onRemoved.addListener((windowId) => {
        var message = { functionName: "windowremoved", windowId: windowId };
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        if (isChromeEdge) message.browser = "edge";
        console.debug("[send]" + message.functionName + " " + windowId);
        if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
    });
    chrome.windows.onFocusChanged.addListener((windowId) => {
        var message = { functionName: "windowfocus", windowId: windowId };
        if (windowId > 0) lastwindowId = windowId;
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        if (isChromeEdge) message.browser = "edge";
        console.debug("[send]" + message.functionName + " " + windowId);
        if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
    });

    if (port == null) return;
}
async function tabsOnCreated(tab) {
    if (port == null) return;
    var message = { functionName: "tabcreated", tab: tab, tabid: tab.id };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.debug("[send]" + message.functionName);
    if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
}
function tabsOnRemoved(tabId) {
    if (port == null) return;
    var message = { functionName: "tabremoved", tabid: tabId };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.debug("[send]" + message.functionName);
    if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
}
async function tabsOnUpdated(tabId, changeInfo, tab) {
    if (!allowExecuteScript(tab)) {
        console.debug('tabsOnUpdated, skipped, not allowExecuteScript');
        return;
    }
    if (openrpadebug) console.log(changeInfo);
    try {
        console.debug("tabsOnUpdated: " + changeInfo.status)
        if (openrpautil_script != null) {
            // tabsexecuteScript(tab.id, { code: openrpautil_script, allFrames: true });
        }
    } catch (e) {
    //    console.log(e);
    //    console.log(tab);
        return;
    }
    var message = { functionName: "tabupdated", tabid: tabId, tab: tab };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.debug("[send]" + message.functionName);
    if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
}
function tabsOnActivated(activeInfo) {
    if (port == null) return;
    var message = { functionName: "tabactivated", tabid: activeInfo.tabId, windowId: activeInfo.windowId };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.debug("[send]" + message.functionName);
    if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
}

var allowExecuteScript = function (tab) {
    if (port == null) return false;
    if (isFirefox) {
        if (tab.url.startsWith("about:")) return false;
        if (tab.url.startsWith("https://support.mozilla.org")) return false;
    }
    // ff uses chrome:// when debugging ?
    if (tab.url.startsWith("chrome://")) return false;
    if (isChrome) {
        if (tab.url.startsWith("https://chrome.google.com")) return false;
    }
    if (tab.url.startsWith("https://docs.google.com/spreadsheets/d")) {
        console.log("skip google docs");
        return false;
    }
    return true;
}
var getCurrentWindow = function () {
    return new Promise(function (resolve, reject) {
        chrome.windows.getCurrent(async curWindow => {
            if (chrome.runtime.lastError) {
                reject(chrome.runtime.lastError.message);
                return;
            }
            if (curWindow.type == "popup" && curWindow.state == "minimized") {
                console.log("Current window #" + curWindow.id + " looks like a background page, look for another window")
                var windows = await windowsgetAll();
                windows = windows.filter(x => x.type != "popup" && x.state != "minimized");
                if (windows.length > 0) return resolve(windows[0]);
            }
            resolve(curWindow);
        });
    });
}
var tabsquery = function (options) {
    return new Promise(function (resolve, reject) {
        try {
            if (options === null || options === undefined) options = {};
            chrome.tabs.query(options, async (tabsList) => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                return resolve(tabsList);
                // fix: https://stackoverflow.com/questions/59974414/chrome-tabs-query-returning-empty-tab-array
            });
        } catch (e) {
            reject(e);
        }
    });
};
var windowsgetAll = function () {
    return new Promise(function (resolve, reject) {
        try {
            chrome.windows.getAll({ populate: false }, (allWindows) => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(allWindows);
            });
        } catch (e) {
            reject(e);
        }
    });
};
var tabsexecuteScript = function (tabid, options) {
    return new Promise(function (resolve, reject) {
        try {
            // https://blog.bitsrc.io/what-is-chrome-scripting-api-f8dbdb6e3987
            var opt = Object.assign(options, { target: { tabId: tabid, allFrames: true } });
            if (opt.code) {
                opt.func = eval(opt.code);
                delete opt.code;
            }
            chrome.scripting.executeScript(
                opt, function (results) {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(results);
            });
        } catch (e) {
            reject(e);
        }
    });
};
var getCurrentTab = function () {
    return new Promise(function (resolve, reject) {
        try {
            chrome.tabs.getCurrent((tab, p1, p2) => {
                console.log(tab);
                console.log(p1);
                console.log(p2);
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(tab);
            });
        } catch (e) {
            reject(e);
        }
    });
};
var tabsupdate = function (tabid, updateoptions) {
    return new Promise(function (resolve, reject) {
        try {
            chrome.tabs.update(tabid, updateoptions, (tab) => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(tab);
            });
        } catch (e) {
            reject(e);
        }
    });
};
var tabshighlight = function (index) {
    return new Promise(function (resolve, reject) {
        try {
            chrome.tabs.highlight({ 'tabs': index, windowId: lastwindowId }, () => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve();
            });
        } catch (e) {
            reject(e);
        }
    });
};

var tabssendMessage = function (tabid, message) {
    return new Promise(async function (resolve, reject) {
        try {
            var details = [];
            if (message.frameId == -1) {
                details = await getAllFrames(tabid);
                console.debug("tabssendMessage, found " + details.length + " frames in tab " + tabid);
            } else {
                console.debug("tabssendMessage, sending to tab " + tabid + " frameid " + message.frameId);
            }
            var result = null;
            var lasterror = "tabssendMessage: No error";
            if (details.length > 1) {
                for (var i = 0; i < details.length; i++) {
                    try {
                        var frame = details[i];
                        if (!allowExecuteScript(frame)) continue;
                        var tmp = await TabsSendMessage(tabid, message, { frameId: frame.frameId });
                        if (result == null) {
                            result = tmp;
                            result.frameId = frame.frameId;
                            if (result.result != null) {
                                result.frameId = frame.frameId;
                            }
                            if (result.results != null && result.results.length > 0) {
                                for (var z = 0; z < result.results.length; z++) {
                                    result.results[z].frameId = frame.frameId;
                                }
                            }
                        } else {
                            if (result.result != null || result.result != undefined) {
                                //if (typeof result.result == "string") {
                                //    result.results = [JSON.parse(result.result)];
                                //} else {
                                //    result.results = [result.result];
                                //}                            
                                //delete result.result;
                            }
                            if (tmp.result != null) {
                                tmp.result.frameId = frame.frameId;
                                if (result.results == null) result.results = [];
                                result.results.push(tmp.result);
                            }
                            if (tmp.results != null && tmp.results.length > 0) {
                                for (var z = 0; z < tmp.results.length; z++) {
                                    tmp.results[z].frameId = frame.frameId;
                                }
                                result.results = result.results.concat(tmp.results);
                            }
                        }
                    } catch (e) {
                        lasterror = e;
                        console.debug(e);
                    }
                }
            }
            if (details.length == 0 || details.length == 1) {
                lasterror = null;
                try {
                    if (message.frameId > -1) result = await TabsSendMessage(tabid, message, { frameId: message.frameId });
                    if (message.frameId == -1) result = await TabsSendMessage(tabid, message);
                } catch (e) {
                    lasterror = e;
                }
                if (result == null) {
                    try {
                        console.log("result == null, so send to tab id #" + tabid);
                        result = await TabsSendMessage(tabid, message);
                        if(result != null) lasterror = null;
                    } catch (e) {
                        if (lasterror == null) lasterror = e;
                    }
                }
                if (result == null) {
                    var w = await getCurrentWindow();
                    tabsList = await tabsquery({ windowId: w.id });
                    if (tabsList.length > 0) {
                        var tab = tabsList[0];
                        console.log("result == null, still null default window is #" + w.id + " and first tab is #" + tab.id + " so send to that!");
                        console.log("window", w);
                        console.log("tabsList", tabsList);
                        result = await TabsSendMessage(tab.id, message);
                    }
                }
            }
            if (result == null || result == undefined) {
                // this will fail with "Could not establish connection. Receiving end does not exist." when page is loading, so just send empty result to robot, it will try again 
                //console.debug("tabssendMessage has no result, return original message");
                //message.error = lasterror;
                result = message;
                console.debug(lasterror);
            }
            console.debug(result);
            resolve(result);
        } catch (e) {
            reject(e);
        }
    });
};
var TabsSendMessage = function (tabid, message, options) {
    return new Promise(function (resolve, reject) {
        try {
            chrome.tabs.sendMessage(tabid, message, options, (result, r2, r3) => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(result);
            });
        } catch (e) {
            reject(e);
        }
    });
};
var getAllFrames = function (tabid) {
    return new Promise(function (resolve, reject) {
        try {
            chrome.webNavigation.getAllFrames({ tabId: tabid }, (details) => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(details);
            });
        } catch (e) {
            reject(e);
        }
    });
};
var tabscreate = function (createProperties) {
    return new Promise(function (resolve, reject) {
        try {
            chrome.tabs.create(createProperties, (tab) => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(tab);
            });
        } catch (e) {
            reject(e);
        }
    });
};
OnPageLoad();
chrome.tabs.onCreated.addListener(tabsOnCreated);
chrome.tabs.onRemoved.addListener(tabsOnRemoved);
chrome.tabs.onUpdated.addListener(tabsOnUpdated);
chrome.tabs.onActivated.addListener(tabsOnActivated);
chrome.downloads.onChanged.addListener(downloadsOnChanged);
chrome.runtime.onMessage.addListener((msg, sender, fnResponse) => {
    if (msg === "loadscript") {
        if (openrpautil_script !== null && openrpautil_script !== undefined && openrpautil_script !== '') {
            console.debug("send openrpautil to tab");
            fnResponse(openrpautil_script);
        } else {
            console.warn("tab requested script, but openrpautil has not been loaded");
            fnResponse(null);
        }
    }
    else {
        runtimeOnMessage(msg, sender, fnResponse);
    }
});
async function runtimeOnMessage(msg, sender, fnResponse) {
    if (port == null) return;
    if (isChrome) msg.browser = "chrome";
    if (isFirefox) msg.browser = "ff";
    if (isChromeEdge) msg.browser = "edge";
    msg.tabid = sender.tab.id;
    msg.windowId = sender.tab.windowId;
    msg.frameId = sender.frameId;
    if (msg.uix && msg.uiy) {
        //var currentWindow = await windowsget(sender.windowId);
        //if (!('id' in currentWindow)) return;
        var allWindows = await windowsgetAll();
        var win = allWindows.filter(x => x.id == sender.tab.windowId);
        var currentWindow = allWindows[0];
        if (win.length > 0) currentWindow = win[0];
        msg.uix += currentWindow.left;
        msg.uiy += currentWindow.top;

        // https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/how-to-size-a-windows-forms-label-control-to-fit-its-contents
        // if (msg.functionName !== "mousemove" && msg.functionName !== "mousedown" && msg.functionName !== "click") console.log("[send]" + msg.functionName);
        if (msg.functionName !== "mousemove") console.log("[send]" + msg.functionName + " (" + msg.uix + "," + msg.uiy + ")");
        if(port!=null) port.postMessage(JSON.parse(JSON.stringify(msg)));
    }
    else {
        if (msg.functionName !== "keydown" && msg.functionName !== "keyup") console.log("[send]" + msg.functionName);
        if(port!=null) port.postMessage(JSON.parse(JSON.stringify(msg)));
    }
}

if (port != null) {
    if(port!=null) port.onMessage.addListener(OnPortMessage);
    if(port!=null) port.onDisconnect.addListener(OnPortDisconnect);
}
if (openrpautil_script === null || openrpautil_script === undefined || openrpautil_script === '') {
    if (port != null) {
        var message = { functionName: "openrpautilscript" };
        try {
            console.log("[send]" + message.functionName);
            if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
        } catch (e) {
            console.error(e);
            port = null;
        }
    }
}
setInterval(function () {
    try {
        if (port != null) {
            var message = { functionName: "ping" };
            if (isChrome) message.browser = "chrome";
            if (isFirefox) message.browser = "ff";
            if (isChromeEdge) message.browser = "edge";
            // console.debug("[send]" + message.functionName);
            if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));
        } else {
            console.log("no port, cannot ping");
        }
    } catch (e) {
        console.error(e);
    }
}, 1000);


var downloadsSearch = function (id) {
    return new Promise(function (resolve, reject) {
        try {
            chrome.downloads.search({ id: id }, function (data) {
                if (data != null && data.length > 0) {
                    return resolve(data[0]);
                }
                resolve(null);
            });
        } catch (e) {
            reject(e);
        }
    });
};
async function downloadsOnChanged(delta) {
    if (!delta.state ||
        (delta.state.current != 'complete')) {
        return;
    }
    const download = await downloadsSearch(delta.id);
    console.log(download);

    if (port == null) return;
    var message = { functionName: "downloadcomplete", data: JSON.stringify(download) };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.debug("[send]" + message.functionName);
    if(port!=null) port.postMessage(JSON.parse(JSON.stringify(message)));

}



var debuggerattach = function (tabId) {
    if (port == null) return false;
    return new Promise(function (resolve, reject) {
        chrome.debugger.attach({ tabId }, "1.0", () => {
            resolve();
        });
    });
}
var debuggerdetach = function (tabId) {
    if (port == null) return false;
    return new Promise(function (resolve, reject) {
        chrome.debugger.detach({ tabId }, () => {
            resolve();
        });
    });
}
var debuggerEvaluate = function (tabId, code) {
    if (port == null) return false;
    return new Promise(function (resolve, reject) {
        chrome.debugger.sendCommand({ tabId }, "Runtime.evaluate", { expression: code }, (result) => {
            resolve(result);
        });
    });
}
