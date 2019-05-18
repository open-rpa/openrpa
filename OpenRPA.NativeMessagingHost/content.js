window.chrome = window.chrome || window.browser;

//console.log('*************************************');
//console.log(window.innerHeight);
//console.log(document.documentElement.clientHeight);
//console.log('*************************************');

// chrome.webstore.install("https://chrome.google.com/webstore/detail/faiaabbemgpndkgpjljhmjahkbpoopfp", successCallback, failureCallback);
if (window.zeniverse_contentlistner === null || window.zeniverse_contentlistner === undefined) {
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
        if (typeof document.zeniverse !== 'undefined') {
            console.log(sender.functionName);
            var func = zeniverse[sender.functionName];
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
            sender.error = "zeniverse not loaded";
            fnResponse(sender);
        }
    });
    window.zeniverse_contentlistner = true;
}

if (typeof document.zeniverse === 'undefined') {
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

