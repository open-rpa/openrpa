//openrpalib = {};
//openrpalib.injectScript = function (func, parameters) {
//    var actualCode = "let data = null;" +
//        "try {  " +
//        "       data = { error : null, result : (" + func + ")(" + JSON.stringify(parameters) + ") };" +
//        "} catch (ex) { data = { error : ex.toString(), result: null }; }" +
//        "document.dispatchEvent(new CustomEvent('openrpainjectscript',              " +
//        "                       {detail: JSON.stringify(__data)}));                        ";
//    var doc = element.ownerDocument || document;
//    let result = { error: 'No result', result: null };
//    var eventListener = function (e) {
//        var data = JSON.parse(e.detail);
//        console.log('ExecuteFunctionInPageContext: received = ' + e.detail);
//        result = data;
//    };
//    doc.addEventListener('openrpainjectscript', eventListener);
//    var script = doc.createElement("script");
//    script.textContent = actualCode;
//    (doc.head || doc.documentElement).appendChild(script);
//    script.remove();
//    doc.removeEventListener('openrpainjectscript', eventListener);
//    return result;
//}

