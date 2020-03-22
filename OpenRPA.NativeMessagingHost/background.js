console.log('n/a');
//var port;
var content_script = '';
var openrpautil_script = '';
var portname = 'com.openrpa.msg';

// Opera 8.0+ (tested on Opera 42.0)
var isOpera = !!window.opr && !!opr.addons || !!window.opera || navigator.userAgent.indexOf(' OPR/') >= 0;

// Firefox 1.0+ (tested on Firefox 45 - 53)
var isFirefox = typeof InstallTrigger !== 'undefined';

// Internet Explorer 6-11
//   Untested on IE (of course). Here because it shows some logic for isEdge.
var isIE = /*@cc_on!@*/false || !!document.documentMode;

// Edge 20+ (tested on Edge 38.14393.0.0)
var isEdge = !isIE && !!window.StyleMedia;
var isChromeEdge = navigator.appVersion.indexOf('Edge') > -1;

// Chrome 1+ (tested on Chrome 55.0.2883.87)
// This does not work in an extension:
//var isChrome = !!window.chrome && !!window.chrome.webstore;
// The other browsers are trying to be more like Chrome, so picking
// capabilities which are in Chrome, but not in others is a moving
// target.  Just default to Chrome if none of the others is detected.
var isChrome = !isOpera && !isFirefox && !isIE && !isEdge;

// Blink engine detection (tested on Chrome 55.0.2883.87 and Opera 42.0)
var isBlink = (isChrome || isOpera) && !!window.CSS;

/* The above code is based on code from: https://stackoverflow.com/a/9851769/3773011 */
//Verification:
var log = console.log;
if (isEdge) log = alert; //Edge console.log() does not work, but alert() does.
log('isChrome: ' + isChrome);
log('isEdge: ' + isEdge);
log('isChromeEdge: ' + isChromeEdge);
log('isFirefox: ' + isFirefox);
log('isIE: ' + isIE);
log('isOpera: ' + isOpera);
log('isBlink: ' + isBlink);


// var port = null;
var portname = 'com.openrpa.msg';

async function OnPortMessage(message) {
    if (port == null) {
        console.warn("OnPortMessage: port is null!");
        console.log(message);
        return;
    }
    if (message === null || message === undefined) {
        console.warn("OnPortMessage: Received null message!");
        return;
    }
    if (message.functionName === "ping") {
        return;
    }
    console.log("[resc]" + message.functionName);
    if (message.functionName === "backgroundscript") {
        try {
            eval.call(window, message.script);
            message.result = "ok";
        } catch (e) {
            console.error(e);
            message.result = e;
        }
        return;
    }
    if (message.functionName === "openrpautilscript") {
        openrpautil_script = message.script;
        return;
    }
    if (message.functionName === "contentscript") {
        content_script = message.script;
        var subtabsList = await tabsquery();
        for (var l in subtabsList) {
            try {
                await tabsexecuteScript(subtabsList[l].id, { code: content_script, allFrames: true });
            } catch (e) {
                console.log(e);
            }
        }
        return;
    }
    // port.postMessage(JSON.parse(JSON.stringify(message)));

};
function OnPortDisconnect(message) {
    console.log("OnPortDisconnect: " + message);
    port = null;
    if (chrome.runtime.lastError) {
        console.warn("onDisconnect: " + chrome.runtime.lastError.message);
        port = null;
        if (portname == 'com.openrpa.msg') {
            // Try with the old name
            portname = 'com.zenamic.msg';
            setTimeout(function () {
                connect();
            }, 1000);
        } else {
            // Wait a few seconds and reretry
            portname = 'com.openrpa.msg';
            setTimeout(function () {
                connect();
            }, 5000);
        }
        return;
    } else {
        console.log("onDisconnect from native port");
    }
}
function connect() {
    console.log("connect()");
    if (port !== null && port !== undefined) {
        try {
            port.onMessage.removeListener(OnPortMessage);
            port.onDisconnect.removeListener(OnPortDisconnect);
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
    port.onMessage.addListener(OnPortMessage);
    port.onDisconnect.addListener(OnPortDisconnect);

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
        message.results.push({ functionName: "tabcreated", tabid: tab.id, tab: tab });
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
    if (window) window.removeEventListener("load", OnPageLoad, false);
    chrome.windows.onCreated.addListener((window) => {
        if (window.type === "normal" || window.type === "popup") { // panel
            var message = { functionName: "windowcreated", windowId: window.id, result: JSON.stringify(window) };
            if (isChrome) message.browser = "chrome";
            if (isFirefox) message.browser = "ff";
            if (isChromeEdge) message.browser = "edge";
            console.log("[send]" + message.functionName + " " + window.id);
            port.postMessage(JSON.parse(JSON.stringify(message)));
        }
    });
    chrome.windows.onRemoved.addListener((windowId) => {
        var message = { functionName: "windowremoved", windowId: windowId };
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        if (isChromeEdge) message.browser = "edge";
        console.log("[send]" + message.functionName + " " + windowId);
        port.postMessage(JSON.parse(JSON.stringify(message)));
    });
    chrome.windows.onFocusChanged.addListener((windowId) => {
        var message = { functionName: "windowfocus", windowId: windowId };
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        if (isChromeEdge) message.browser = "edge";
        console.log("[send]" + message.functionName + " " + windowId);
        port.postMessage(JSON.parse(JSON.stringify(message)));
    });

    if (port == null) return;

    var message = { functionName: "enumwindows" };
    await EnumWindows(message);
    console.log("[send]" + message.functionName + " results: " + message.results.length);
    port.postMessage(JSON.parse(JSON.stringify(message)));

    message = { functionName: "enumtabs" };
    await EnumTabs(message);
    console.log("[send]" + message.functionName + " results: " + message.results.length);
    port.postMessage(JSON.parse(JSON.stringify(message)));

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
                resolve(tabsList);
            });
        } catch (e) {
            reject(e);
        }
    });
};
async function tabsOnCreated(tab) {
    if (port == null) return;
    var message = { functionName: "tabcreated", tab: tab, tabid: tab.id };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.log("[send]" + message.functionName);
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
function tabsOnRemoved(tabId) {
    if (port == null) return;
    var message = { functionName: "tabremoved", tabid: tabId };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.log("[send]" + message.functionName);
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
async function tabsOnUpdated(tabId, changeInfo, tab) {
    if (port == null) return;
    try {
        await tabsexecuteScript(tab.id, { code: content_script, allFrames: true });
    } catch (e) {
        console.log(e);
    }
    var message = { functionName: "tabupdated", tabid: tabId, tab: tab };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.log("[send]" + message.functionName);
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
function tabsOnActivated(activeInfo) {
    if (port == null) return;
    var message = { functionName: "tabactivated", tabid: activeInfo.tabId, windowId: activeInfo.windowId };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.log("[send]" + message.functionName);
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
//window.addEventListener("load", OnPageLoad, false);
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
            chrome.tabs.executeScript(tabid, options, function (results) {
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

OnPageLoad();
chrome.tabs.onCreated.addListener(tabsOnCreated);
chrome.tabs.onRemoved.addListener(tabsOnRemoved);
chrome.tabs.onUpdated.addListener(tabsOnUpdated);
chrome.tabs.onActivated.addListener(tabsOnActivated);
if (openrpautil_script === null || openrpautil_script === undefined || openrpautil_script === '') {
    if (port != null) {
        var message = { functionName: "openrpautilscript" };
        try {
            console.log("[send]" + message.functionName);
            port.postMessage(JSON.parse(JSON.stringify(message)));
        } catch (e) {
            console.error(e);
            port = null;
        }
    }
}
if (content_script === null || content_script === undefined || content_script === '') {
    if (port != null) {
        var message = { functionName: "contentscript" };
        try {
            console.log("[send]" + message.functionName);
            port.postMessage(JSON.parse(JSON.stringify(message)));
        } catch (e) {
            console.error(e);
            port = null;
        }
    }
}
