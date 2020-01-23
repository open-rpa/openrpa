console.log('n/a');
if (backgroundscript === undefined) backgroundscript = null;
if (port === undefined) port = null;
var content_script = '';
var zeniverse_script = '';

// Opera 8.0+ (tested on Opera 42.0)
var isOpera = !!window.opr && !!opr.addons || !!window.opera || navigator.userAgent.indexOf(' OPR/') >= 0;

// Firefox 1.0+ (tested on Firefox 45 - 53)
var isFirefox = typeof InstallTrigger !== 'undefined';

// Internet Explorer 6-11
//   Untested on IE (of course). Here because it shows some logic for isEdge.
var isIE = /*@cc_on!@*/false || !!document.documentMode;

// Edge 20+ (tested on Edge 38.14393.0.0)
var isEdge = !isIE && !!window.StyleMedia;

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
log('isFirefox: ' + isFirefox);
log('isIE: ' + isIE);
log('isOpera: ' + isOpera);
log('isBlink: ' + isBlink);

chrome.runtime.onInstalled.addListener(async (details) => {
    try {
        var tabsList = await tabsquery();
        for (var i in tabsList) {
            //if (tabsList[i].url.indexOf("chrome://") !== 0) {
            chrome.tabs.reload(tabsList[i].id, {});
            //}
        }
    } catch (e) {
        console.log(e);
        return;
    }
});
chrome.runtime.onMessage.addListener((sender, msg, fnResponse) => {
    if (sender === "loadscript") {
        if (zeniverse_script !== null && zeniverse_script !== undefined && zeniverse_script !== '') {
            console.log("send zeniverse to tab");
            fnResponse(zeniverse_script);
        } else {
            console.log("tab requested script, but zeniverse has not been loaded");
            fnResponse(null);
        }
    }
    else {
        runtimeOnMessage(sender, msg, fnResponse);
    }
});
async function runtimeOnMessage(sender, msg, fnResponse) {
    if (isChrome) sender.browser = "chrome";
    if (isFirefox) sender.browser = "ff";
    sender.tabid = msg.tab.id;
    sender.windowId = msg.tab.windowId;
    if (sender.uix && sender.uiy) {
        var currentWindow = await windowsget(sender.windowId);
        console.log(sender.uix + "," + sender.uiy);
        if (!('id' in currentWindow)) return;

        sender.uix += currentWindow.left;
        sender.uiy += currentWindow.top;
        console.log("after: " + sender.uix + "," + sender.uiy);
        sender.uix += 7;
        sender.uiy -= 7;

        // https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/how-to-size-a-windows-forms-label-control-to-fit-its-contents
        var message = sender;
        //console.log('[' + currentWindow.left.toString().padEnd(3, ' ') + ',' + currentWindow.top.toString().padEnd(3, ' ') + ']');
        //console.log('[' + message.x.toString().padEnd(3, ' ') + ',' + message.y.toString().padEnd(3, ' ') + ',' + message.width.toString().padEnd(4, ' ') + ',' + message.height.toString().padEnd(4, ' ') + '] ' +
        //    '[' + message.uix.toString().padEnd(3, ' ') + ',' + message.uiy.toString().padEnd(3, ' ') + ',' + message.uiwidth.toString().padEnd(4, ' ') + ',' + message.uiheight.toString().padEnd(4, ' ') + ']');
        //sender.screenInfo = screenInfo;
        //var screenInfo = sender.screenInfo;
        console.log("Send message " + sender.functionName + " to port");
        port.postMessage(JSON.parse(JSON.stringify(sender)));
    }
    else {
        console.log("Send message " + sender.functionName + " to port");
        port.postMessage(JSON.parse(JSON.stringify(sender)));
    }
}
async function portOnMessage(message) {
    if (message.functionName === "zeniversescript") {
        console.log("received zeniverse script from host");
        zeniverse_script = message.script;
        delete message.script;

        message = { functionName: "contentscript" };
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        port.postMessage(JSON.parse(JSON.stringify(message)));
        return;
    }
    if (message.functionName === "contentscript") {
        console.log("received content script from host");
        content_script = message.script;
        delete message.script;

        var subtabsList = await tabsquery();
        for (var l in subtabsList) {
            try {
                //await tabsexecuteScript(subtabsList[l].id, { file: 'content.js', allFrames: true });
                await tabsexecuteScript(subtabsList[l].id, { code: content_script, allFrames: true });
            } catch (e) {
                console.log(e);
            }
        }

        return;
    }
    if (message.functionName === "ping") {
        message.result = "pong";
        port.postMessage(JSON.parse(JSON.stringify(message)));
        return;
    }
    if (message.functionName === "updatetab") {
        try {
            var updateoptions = { active: message.tab.active, highlighted: message.tab.highlighted };
            var tab = await tabsupdate(message.tab.id, updateoptions);
            if (message.data !== message.tab.url) updateoptions.url = message.data;
            tab = await tabsupdate(message.tab.id, updateoptions);
            //if (message.data !== message.tab.url) {
            //    updateoptions = { url: message.tab.url };
            //    tab = await tabsupdate(message.tab.id, updateoptions);
            //}

            message.tab = tab;
        } catch (e) {
            message.error = e;
            console.log(e);
        }
        port.postMessage(JSON.parse(JSON.stringify(message)));
        return;
    }
    if (message.functionName === "closetab") {
        chrome.tabs.remove(message.tab.id, function () {
            port.postMessage(JSON.parse(JSON.stringify(message)));
        });
        return;
    }
    if (message.functionName === "refreshtabs") {
        try {
            var subtabsList2 = await tabsquery();
            for (var p in subtabsList2) {
                //if (subtabsList2[p].url.indexOf("chrome://") !== 0) {
                var submessage = { functionName: "tabupdated", tabid: subtabsList2[p].id, tab: subtabsList2[p] };
                if (isChrome) submessage.browser = "chrome";
                if (isFirefox) submessage.browser = "ff";
                port.postMessage(JSON.parse(JSON.stringify(submessage)));
                //}
            }
        } catch (e) {
            message.error = e;
            console.log(e);
        }
        port.postMessage(JSON.parse(JSON.stringify(message)));
        return;
    }
    if (message.functionName === "openurl") {
        try {
            var url = message.data;
            var createProperties = { url: url };
            if (message.windowId !== null && message.windowId !== undefined && message.windowId > 0) createProperties.windowId = message.windowId;
            var newtab = await tabscreate(createProperties);
            message.tab = newtab;
        } catch (e) {
            message.error = e;
            console.log(e);
        }
        port.postMessage(JSON.parse(JSON.stringify(message)));
        return;
    }

    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    if (message.tabid !== undefined && message.tabid !== null && message.tabid > -1) {
        try {
            console.log('sendMessage ' + message.functionName + ' for tab ' + message.tabid + ' - ' + message.messageid);
            var options = null;
            if (message.frameId !== null && message.frameId !== undefined && message.frameId > -1) options = { frameId: message.frameId };
            try {
                var singleresult = await tabssendMessage(message.tabid, message, options);
                if (singleresult === null || singleresult === undefined) {
                    console.log('sendMessage null reply ' + message.functionName + ' for tab ' + message.tabid + ' - ' + message.messageid);
                    console.log(message);
                    port.postMessage(JSON.parse(JSON.stringify(message)));
                    return;
                }
                var result = singleresult.result;
                var currentWindow = await windowsgetCurrent();
                if (result.uix !== undefined && result.uiy !== undefined) {
                    if (!('id' in currentWindow)) return;
                    result.uix += currentWindow.left;
                    result.uiy += currentWindow.top;
                    //result.uix += 7;
                    //result.uiy -= 7;

                    console.log('sendMessage reply with uix and uiy ' + result.functionName + ' for tab ' + result.tabid + ' - ' + result.messageid);
                    console.log(result);
                    port.postMessage(JSON.parse(JSON.stringify(result)));
                }
                else {
                    console.log('sendMessage reply no cords ' + result.functionName + ' for tab ' + result.tabid + ' - ' + result.messageid);
                    console.log(result);
                    port.postMessage(JSON.parse(JSON.stringify(result)));
                }
                return;
            } catch (e) {
                message.error = e;
                port.postMessage(JSON.parse(JSON.stringify(message)));
            }
        } catch (e) {
            console.log("Error while sending message to Tab" + message.tabid + " " + e);
        }
    }

    var tabsList = await tabsquery();
    var resultarray = [];
    var tabCount = 0;
    var frameCount = 0;
    var messageSent = function (result, noCount) {
        if (result !== null) resultarray.push(result);
        if (noCount !== true)--frameCount;
        console.log('handleFrameResponse.messageSent: ' + frameCount);
        if (frameCount <= 0) {
            message.results = resultarray;
            console.log('sendMessage replys (' + resultarray.length + ') ' + message.functionName + ' - ' + message.messageid);
            console.log(message);
            port.postMessage(JSON.parse(JSON.stringify(message)));
        }
    };
    for (var y in tabsList) {
        ++tabCount;
    }
    if (tabCount === 0) {
        console.log('sendMessage ' + message.functionName + ' - ' + message.messageid + ' is empty, no tabs found');
        message.results = resultarray;
        console.log(message);
        port.postMessage(JSON.parse(JSON.stringify(message)));
    }
    else {
        var handleFrameResponse = async function (tabresult) {
            try {
                console.log('handleFrameResponse.frameCount: ' + frameCount);
                var result = tabresult;
                if (tabresult !== null && tabresult !== undefined) {
                    result = tabresult.result;
                }
                if (result === null || result === undefined) {
                    tabresult = { functionName: message.functionName, messageid: message.messageid, tabid: message.tabid, windowId: message.windowId };
                    console.log('sendMessage log null reply ' + message.functionName + ' from Tab: ' + message.tabid + ' Frame: ' + message.frameId + ' messageid: ' + message.messageid);
                    messageSent(tabresult);
                }
                else {
                    var currentWindow = await windowsget(message.windowId);
                    if (result.result !== null && result.result !== undefined) {
                        try {
                            var arr = JSON.parse(result.result);
                            if (Array.isArray(arr)) {
                                for (var i = 0; i < arr.length; i++) {
                                    arr[i].uix += currentWindow.left;
                                    arr[i].uiy += currentWindow.top;
                                    arr[i].uix += 7;
                                    arr[i].uiy -= 7;

                                }
                            }
                            result.result = JSON.stringify(arr);
                        } catch (e) {
                            console.log(e);
                        }
                    }
                    console.log(result.result);
                    if (result.result !== undefined && result.result !== null && ('id' in currentWindow)) {

                    }
                    if (result.uix && result.uiy && ('id' in currentWindow)) {
                        result.uix += currentWindow.left;
                        result.uiy += currentWindow.top;
                        result.uix += 7;
                        result.uiy -= 7;

                        console.log('sendMessage log reply ' + result.functionName + ' from Tab: ' + result.tabid + ' Frame: ' + result.frameId + ' messageid: ' + result.messageid);
                        messageSent(result);
                    }
                    else {
                        console.log('sendMessage log reply ' + result.functionName + ' from Tab: ' + result.tabid + ' Frame: ' + result.frameId + ' messageid: ' + result.messageid);
                        messageSent(result);
                    }
                }

            } catch (e) {
                console.log(e);
            }
        };
        for (var i in tabsList) {
            try {
                var _tabid = tabsList[i].id;
                details = await getAllFrames(_tabid);
                let subframeCount = 0;
                details.forEach(() => {
                    ++subframeCount;
                    ++frameCount;
                });
                if (subframeCount === 0)++frameCount;
            } catch (e) {
                console.log('Error getting all frames from Tab ' + _tabid + ' ' + e);
            }
        }
        console.log('frameCount: ' + frameCount);
        for (var z in tabsList) {
            var tabid = tabsList[z].id;
            message.tabid = tabid;
            message.windowId = tabsList[z].windowId;
            //message.tab = tabsList[z];
            try {
                details = await getAllFrames(tabid);
                let subframeCount = 0;
                details.forEach((frame) => {
                    ++subframeCount;
                });
                if (subframeCount > 0) {
                    details.forEach(async (frame) => {
                        try {
                            var frameId = frame.frameId;
                            message.frameId = frameId;
                            message.tabid = tabid;
                            console.log('sendMessage ' + message.functionName + ' to Tab: ' + tabid + ' Frame: ' + frameId + ' messageid: ' + message.messageid);
                            var sendresult = await tabssendMessage(message.tabid, message, { frameId: frameId });
                            handleFrameResponse(sendresult);
                        } catch (e) {
                            console.log('Error while sending message to Tab: ' + tabid + ' Frame: ' + frameId + ' ' + e);
                            --frameCount;
                            messageSent(null, true);
                        }
                    });
                } else {
                    var frameId = -1;
                    message.frameId = frameId;
                    message.tabid = tabid;
                    console.log('sendMessage ' + message.functionName + ' to Tab: ' + tabid + ' Frame: ' + frameId + ' messageid: ' + message.messageid);
                    var sendresult = await tabssendMessage(message.tabid, message, null);
                    handleFrameResponse(sendresult);
                }
            } catch (e) {
                console.log('Error getting all frames from Tab ' + tabid + ' ' + e);
                messageSent(null, true);
            }
        }
    }
}
function portOnDisconnect(message) {
    console.log("onDisconnect from native port");
    port = null;
    //setTimeout(function () { connect(); }, 3000);
}
function connect() {
    if (port !== null) {
        try {
            if (port.onConnect) { port.onConnect.removeListener(portOnConnect); }
            port.onMessage.removeListener(portOnMessage);
            port.onDisconnect.removeListener(portOnDisconnect);
        } catch (e) {
            console.log(e);
        }
    }
    if (port === null) {
        console.log("Connecting to com.zenamic.msg");
        port = chrome.runtime.connectNative("com.zenamic.msg");
    }
    port.onMessage.addListener(portOnMessage);
    port.onDisconnect.addListener(portOnDisconnect);

    console.log("Connected to native port, request zeniverse script");
    var message = { functionName: "zeniversescript" };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    port.postMessage(JSON.parse(JSON.stringify(message)));

}

async function OnPageLoad(event) {
    if (window) window.removeEventListener("load", OnPageLoad, false);
    var allWindows = await windowsgetAll();
    for (var i in allWindows) {
        var window = allWindows[i];
        var message = { functionName: "windowcreated", windowId: window.id };
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        port.postMessage(JSON.parse(JSON.stringify(message)));
    }
    chrome.windows.onCreated.addListener((window) => {
        if (window.type === "normal" || window.type === "popup") { // panel
            var message = { functionName: "windowcreated", windowId: windowId };
            if (isChrome) message.browser = "chrome";
            if (isFirefox) message.browser = "ff";
            port.postMessage(JSON.parse(JSON.stringify(message)));
        }
    });
    chrome.windows.onRemoved.addListener((windowId) => {
        var message = { functionName: "windowremoved", windowId: windowId };
        if (isChrome) message.browser = "chrome";
        if (isFirefox) message.browser = "ff";
        port.postMessage(JSON.parse(JSON.stringify(message)));
    });
}

async function tabsOnCreated(tab) {
    var message = { functionName: "tabcreated", tab: tab };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
function tabsOnRemoved(tabId) {
    var message = { functionName: "tabremoved", tabid: tabId };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
async function tabsOnUpdated(tabId, changeInfo, tab) {
    try {
        await tabsexecuteScript(tab.id, { code: content_script, allFrames: true });
    } catch (e) {
        console.log(e);
    }
    var message = { functionName: "tabupdated", tabid: tabId, tab: tab };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    port.postMessage(JSON.parse(JSON.stringify(message)));
}
function tabsOnActivated(activeInfo) {
    var message = { functionName: "tabactivated", tabid: activeInfo.tabId, windowId: activeInfo.windowId };
    if (isChrome) message.browser = "chrome";
    if (isFirefox) message.browser = "ff";
    port.postMessage(JSON.parse(JSON.stringify(message)));
    //await tabsexecuteScript(tab.id, { code: content_script, allFrames: true });
}
window.addEventListener("load", OnPageLoad, false);
chrome.tabs.onCreated.addListener(tabsOnCreated);
chrome.tabs.onRemoved.addListener(tabsOnRemoved);
chrome.tabs.onUpdated.addListener(tabsOnUpdated);
chrome.tabs.onActivated.addListener(tabsOnActivated);

connect();

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

var windowsget = function (windowId) {
    return new Promise(function (resolve, reject) {
        try {
            chrome.windows.get(windowId, null, (currentWindow) => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(currentWindow);
            });
        } catch (e) {
            reject(e);
        }
    });
};

var tabssendMessage = function (tabid, message, options) {
    return new Promise(function (resolve, reject) {
        try {
            chrome.tabs.sendMessage(tabid, message, options, (result) => {
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

var windowsgetCurrent = function () {
    return new Promise(function (resolve, reject) {
        try {
            chrome.windows.getCurrent((currentWindow) => {
                if (chrome.runtime.lastError) {
                    reject(chrome.runtime.lastError.message);
                    return;
                }
                resolve(currentWindow);
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



// https://stackoverflow.com/questions/18309471/how-to-capture-a-screenshot-of-a-single-html-element-in-chrome-extension
// here we create a new image
function createImage(dataURL, x, y, width, height, callback) {
    // create a canvas
    var canvas = createCanvas(width, height);
    // get the context of your canvas
    var context = canvas.getContext('2d');
    // create a new image object
    var croppedImage = new Image();
    // https://stackoverflow.com/questions/30466490/create-canvas-element-with-image-and-append-to-parent
    croppedImage.onload = function () {
        // this is where you manipulate the screenshot (cropping)
        // parameter 1: source image (screenshot)
        // parameter 2: source image x coordinate
        // parameter 3: source image y coordinate
        // parameter 4: source image width
        // parameter 5: source image height
        // parameter 6: destination x coordinate
        // parameter 7: destination y coordinate
        // parameter 8: destination width
        // parameter 9: destination height
        context.drawImage(croppedImage, x, y, width, height, 0, 0, width, height);

        // canvas.toDataURL() contains your cropped image
        //console.log(canvas.toDataURL());
        callback(canvas.toDataURL());
    };
    croppedImage.src = dataURL; // screenshot (full image)
}

// creates a canvas element
function createCanvas(canvasWidth, canvasHeight) {
    var canvas = document.createElement("canvas");

    // size of canvas in pixels
    canvas.width = canvasWidth;
    canvas.height = canvasHeight;
    return canvas;
}

// calling the captureVisibleTab method takes a screenhot
function createScreenshot(callback) {
    // you can have two image formats (jpeg and png)
    // for jpeg use { format: "jpeg", quality: 100 } (you can adjust the jpeg image quality from 0-100) 
    // for png use { format: "png" }
    chrome.tabs.captureVisibleTab(null, { format: "png" }, callback);
}



//createScreenshot(function (dataURL) {
//    if (dataURL === null || dataURL === undefined) {
//        console.log("send message to host, without image, dataURL is null");
//        port.postMessage(sender);
//    }
//    else
//    {
//        createImage(dataURL, sender.x, sender.y, sender.width, sender.height, function (dataURL) {
//            console.log(dataURL);
//            sender.image = dataURL;
//            console.log("send message to host, WITH image");
//            //console.log(msg);
//            port.postMessage(sender);
//        });
//    }
//});
