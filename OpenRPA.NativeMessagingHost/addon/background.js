var port = null;
var portname = 'com.openrpa.msg';
function OnPortMessage(message) {
    if (port == null) {
        console.warn("OnPortMessage: port is null!");
        console.log(message);
        return;
    }
    if (message === null || message === undefined) {
        console.warn("OnPortMessage: Received null message!");
        return;
    }
    if (message.functionName === "backgroundscript") {
        console.log("[resc]" + message.functionName);
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
connect();
