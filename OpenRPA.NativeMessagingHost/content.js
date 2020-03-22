window.chrome = window.chrome || window.browser;
// chrome.webstore.install("https://chrome.google.com/webstore/detail/faiaabbemgpndkgpjljhmjahkbpoopfp", successCallback, failureCallback);
if (window.openrpautil_contentlistner === null || window.openrpautil_contentlistner === undefined) {
    chrome.runtime.onMessage.addListener(function (sender, message, fnResponse) {
        if (sender === "loadscript") {
            try {
                eval.call(window, message);
            } catch (ex) {
                alert(ex);
                console.error(ex);
            }
            if (fnResponse) fnResponse();
            return;
        }
        if (sender.functionName == "getmanifest") {
            //console.log("getmanifest");
            var manifestData = chrome.runtime.getManifest();
            //console.log(manifestData);
            sender.result = manifestData;
            var test = JSON.parse(JSON.stringify(sender));
            fnResponse(test);
        } else if (typeof document.openrpautil !== 'undefined' && openrpautil !== undefined) {
            // console.log(sender.functionName);
            var func = openrpautil[sender.functionName];
            if (func) {
                sender.result = func(sender);
                fnResponse(sender);
            }
            else {
                sender.error = "Unknown function " + sender.functionName;
                fnResponse(sender);
            }
        }
        else {
            sender.error = "openrpautil not loaded";
            fnResponse(sender);
        }
    });
    window.openrpautil_contentlistner = true;
}

if (typeof document.openrpautil === 'undefined') {
    chrome.runtime.sendMessage("loadscript", function (message) {
        if (message) {
            try {
                eval.call(window, message);
            } catch (ex) {
                alert(ex);
                console.error(ex);
            }
        }
    });
}

