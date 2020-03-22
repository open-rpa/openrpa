var port = null;
var portname = 'com.openrpa.msg';
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
    console.log("[baseresc][" + message.messageid + "]" + message.functionName);
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
};
function BaseOnPortDisconnect(message) {
    console.log("BaseOnPortDisconnect: " + message);
    port = null;
    if (chrome.runtime.lastError) {
        console.warn("BaseOnPortDisconnect: " + chrome.runtime.lastError.message);
        port = null;
        if (portname == 'com.openrpa.msg') {
            // Try with the old name
            portname = 'com.zenamic.msg';
            setTimeout(function () {
                Baseconnect();
            }, 1000);
        } else {
            // Wait a few seconds and reretry
            portname = 'com.openrpa.msg';
            setTimeout(function () {
                Baseconnect();
            }, 5000);
        }
        return;
    } else {
        console.log("BaseOnPortDisconnect from native port");
    }
}
function Baseconnect() {
    console.log("Baseconnect()");
    if (port !== null && port !== undefined) {
        try {
            port.onMessage.removeListener(BaseOnPortMessage);
            port.onDisconnect.removeListener(BaseOnPortDisconnect);
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
    port.onMessage.addListener(BaseOnPortMessage);
    port.onDisconnect.addListener(BaseOnPortDisconnect);

    if (chrome.runtime.lastError) {
        console.warn("Whoops.. " + chrome.runtime.lastError.message);
        port = null;
        return;
    } else {
        console.log("Connected to native port, request backgroundscript script");
    }

    var message = { functionName: "backgroundscript" };
    try {
        port.postMessage(JSON.parse(JSON.stringify(message)));
    } catch (e) {
        console.error(e);
        port = null;
    }
}
Baseconnect();
