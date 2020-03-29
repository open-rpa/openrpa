console.log('openrpa extension begin');
//var port;
var openrpautil_script = '';
var portname = 'com.openrpa.msg';
var lastwindowId = 1;

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
            console.debug("SendToTab: send message to tab id " + message.tabid + " windowId " + windowId);
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
        if (message.results && message.results.length > 0) {
            message.results.forEach((item) => {
                if (item.uix && item.uiy) {
                    item.uix += currentWindow.left;
                    item.uiy += currentWindow.top;
                }
                item.windowId = windowId;
                item.tabid = message.tabid;
            });
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

            var subtabsList = await tabsquery();
            for (var l in subtabsList) {
                try {
                    if (!allowExecuteScript(subtabsList[l])) continue;
                    await tabsexecuteScript(subtabsList[l].id, { code: openrpautil_script, allFrames: true });
                } catch (e) {
                    console.log(e);
                }
            }
            return;
        }
        if (message.functionName === "contentscript") {
            return;
        }
        if (message.functionName === "enumwindows") {
            await EnumWindows(message);
            console.log("[send][" + message.messageid + "]" + message.functionName + " results: " + message.results.length);
            port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        if (message.functionName === "enumtabs") {
            await EnumTabs(message);
            console.log("[send][" + message.messageid + "]" + message.functionName + " results: " + message.results.length);
            port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        if (message.functionName === "openurl") {
            if (message.xPath == "true") {
                var createProperties = { url: message.data };
                //if (message.windowId !== null && message.windowId !== undefined && message.windowId > 0) createProperties.windowId = message.windowId;
                var newtab = await tabscreate(createProperties);
                message.tab = newtab;
                message.tabid = newtab.id;
                console.log("[send][" + message.messageid + "]" + message.functionName);
                port.postMessage(JSON.parse(JSON.stringify(message)));
                return;
            }
            var tabsList = await tabsquery();
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
                message.error = "No tabs found!";
                port.postMessage(JSON.parse(JSON.stringify(message)));
                return;
            }
            var tab = tabsList[0];
            if (tab.url != message.data) {
                var updateoptions = { active: true, highlighted: true };
                updateoptions.url = message.data;
                tab = await tabsupdate(tab.id, updateoptions);
            }
            message.tab = tab;
            message.tabid = tab.id;
            console.log("[send][" + message.messageid + "]" + message.functionName);
            port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        var windowId = 1;
        var tabsList = await tabsquery();
        if (message.windowId !== null && message.windowId !== undefined && message.windowId !== '' && message.windowId > 0) {
            windowId = message.windowId;
            tabsList = tabsList.filter(x => x.windowId == message.windowId);
        } else {
            windowId = lastwindowId;
            tabsList = tabsList.filter(x => x.windowId == lastwindowId);
        }
        if (message.tabid !== null && message.tabid !== undefined && message.tabid !== '' && message.tabid > 0) {
            tabsList = tabsList.filter(x => x.id == message.tabid);
        } else {
            tabsList = tabsList.filter(x => x.active == true);
        }
        if (tabsList.length == 0) {
            message.error = "No tabs found!";
            console.log("[send][" + message.messageid + "]" + message.functionName + " No tabs found");
            port.postMessage(JSON.parse(JSON.stringify(message)));
            return;
        }
        var tab = tabsList[0];
        message.windowId = windowId;
        message.tabid = tab.id;
        message = await SendToTab(windowId, message);

    } catch (e) {
        console.log(e);
        message.error = JSON.stringify(e);
    }
    console.log("[send][" + message.messageid + "]" + message.functionName);
    port.postMessage(JSON.parse(JSON.stringify(message)));
    console.debug(message);
};
function OnPortDisconnect(message) {
    console.log("OnPortDisconnect: " + message);
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
            port.onMessage.removeListener(OnPortMessage);
            port.onDisconnect.removeListener(OnPortDisconnect);
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
    if (window) window.removeEventListener("load", OnPageLoad, false);
    chrome.windows.onCreated.addListener((window) => {
        if (window.type === "normal" || window.type === "popup") { // panel
            if (window.id > 0) lastwindowId = window.id;
            var message = { functionName: "windowcreated", windowId: window.id, result: JSON.stringify(window) };
            if (isChrome) message.browser = "chrome";
            if (isFirefox) message.browser = "ff";
            if (isChromeEdge) message.browser = "edge";
            console.debug("[send]" + message.functionName + " " + window.id);
            port.postMessage(JSON.parse(JSON.stringify(message)));
        }
    });
    chrome.windows.onRemoved.addListener((windowId) => {
        var message = { functionName: "windowremoved", windowId: windowId };
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        if (isChromeEdge) message.browser = "edge";
        console.debug("[send]" + message.functionName + " " + windowId);
        port.postMessage(JSON.parse(JSON.stringify(message)));
    });
    chrome.windows.onFocusChanged.addListener((windowId) => {
        var message = { functionName: "windowfocus", windowId: windowId };
        if (windowId > 0) lastwindowId = windowId;
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        if (isChromeEdge) message.browser = "edge";
        console.debug("[send]" + message.functionName + " " + windowId);
        port.postMessage(JSON.parse(JSON.stringify(message)));
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
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
function tabsOnRemoved(tabId) {
    if (port == null) return;
    var message = { functionName: "tabremoved", tabid: tabId };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.debug("[send]" + message.functionName);
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
async function tabsOnUpdated(tabId, changeInfo, tab) {
    if (!allowExecuteScript(tab)) return;
    try {
        await tabsexecuteScript(tab.id, { code: openrpautil_script, allFrames: true });
    } catch (e) {
        console.log(e);
        console.log(tab);
    }
    var message = { functionName: "tabupdated", tabid: tabId, tab: tab };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.debug("[send]" + message.functionName);
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
function tabsOnActivated(activeInfo) {
    if (port == null) return;
    var message = { functionName: "tabactivated", tabid: activeInfo.tabId, windowId: activeInfo.windowId };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (isChromeEdge) message.browser = "edge";
    console.debug("[send]" + message.functionName);
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
//window.addEventListener("load", OnPageLoad, false);

var allowExecuteScript = function (tab){
    if (port == null) return;
    if (isFirefox) {
        if (tab.url.startsWith("about:")) return;
        if (tab.url.startsWith("https://support.mozilla.org")) return;
    }
    // ff uses chrome:// when debugging ?
    if (tab.url.startsWith("chrome://")) return;
    if (isChrome) {
        if (tab.url.startsWith("https://chrome.google.com")) return;
    }
    return true;
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
                try {
                    if (message.frameId > -1) result = await TabsSendMessage(tabid, message, { frameId: message.frameId });
                    if (message.frameId == -1) result = await TabsSendMessage(tabid, message);
                    
                    // result = await TabsSendMessage(tabid, message);

                    if (result == null) {
                        var tabsList = await tabsquery();
                    }

                } catch (e) {
                    lasterror = e;
                    console.debug(e);
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
        if (msg.functionName !== "mousemove" && msg.functionName !== "mousedown" && msg.functionName !== "click") console.log("[send]" + msg.functionName);
        port.postMessage(JSON.parse(JSON.stringify(msg)));
    }
    else {
        if (msg.functionName !== "keydown" && msg.functionName !== "keyup") console.log("[send]" + msg.functionName);        
        port.postMessage(JSON.parse(JSON.stringify(msg)));
    }
}

if (port != null) {
    port.onMessage.addListener(OnPortMessage);
    port.onDisconnect.addListener(OnPortDisconnect);
}
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
setInterval(function () {
    try {
        if (port != null) {
            var message = { functionName: "ping" };
            if (isChrome) message.browser = "chrome";
            if (isFirefox) message.browser = "ff";
            if (isChromeEdge) message.browser = "edge";
            // console.debug("[send]" + message.functionName);
            port.postMessage(JSON.parse(JSON.stringify(message)));
        } else {
            console.log("no port, cannot ping");
        }
    } catch (e) {
        console.error(e);
    }
}, 1000);