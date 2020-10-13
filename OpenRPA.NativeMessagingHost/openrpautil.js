document.openrpadebug = false;
document.openrpauniquexpathids = ['ng-model', 'ng-reflect-name']; // aria-label
function inIframe() {
    var result = true;
    try {
        if (window.self === window.top) return false;
        if (parent) {
        }

    } catch (e) {
    }
    return result;
}
if (true == false) {
    console.debug('skip declaring openrpautil class');
    document.openrpautil = {};
} else {
    if (window.openrpautil_contentlistner === null || window.openrpautil_contentlistner === undefined) {
        function remotePushEvent(evt) {
            if (evt.data != null && evt.data.functionName == "mousemove") {
                openrpautil.parent = evt.data;
                try {
                    notifyFrames();
                } catch (e) {
                }
            }
        }
        if (window.addEventListener) {
            window.addEventListener("message", remotePushEvent, false);
        } else {
            window.attachEvent("onmessage", remotePushEvent);
        }
        var notifyFrames = (event) => {
            for (let targetElement of document.getElementsByTagName('iframe')) {
                var message = { functionName: 'mousemove', parents: 0, xpaths: [] };
                try {
                    openrpautil.applyPhysicalCords(message, targetElement);
                } catch (e) {
                    console.error(e);
                }
                if (openrpautil.parent != null) {
                    message.parents = openrpautil.parent.parents + 1;
                    message.uix += openrpautil.parent.uix;
                    message.uiy += openrpautil.parent.uiy;
                }
                var width = getComputedStyle(targetElement, null).getPropertyValue('border-width');
                width = parseInt(width.replace('px', '')) * 0.85;
                message.uix += (width | 0);
                var height = getComputedStyle(targetElement, null).getPropertyValue('border-height');
                height = parseInt(height.replace('px', '')) * 0.85;
                message.uiy += (height | 0);

                message.cssPath = UTILS.cssPath(targetElement, false);
                message.xPath = UTILS.xPath(targetElement, true);
                //console.log('postMessage to', targetElement, { uix: message.uix, uiy: message.uiy });
                targetElement.contentWindow.postMessage(message, '*');
            }
            var doFrames = () => {
                try {
                    for (let targetElement of document.getElementsByTagName('frame')) {
                        var message = { functionName: 'mousemove', parents: 0, xpaths: [] };
                        try {
                            openrpautil.applyPhysicalCords(message, targetElement);
                        } catch (e) {
                            console.error(e);
                        }
                        if (openrpautil.parent != null) {
                            message.parents = openrpautil.parent.parents + 1;
                            message.uix += openrpautil.parent.uix;
                            message.uiy += openrpautil.parent.uiy;
                        }
                        var width = getComputedStyle(targetElement, null).getPropertyValue('border-width');
                        width = parseInt(width.replace('px', '')) * 0.85;
                        message.uix += width;
                        var height = getComputedStyle(targetElement, null).getPropertyValue('border-height');
                        height = parseInt(height.replace('px', '')) * 0.85;
                        message.uiy += (height | 0);

                        message.cssPath = UTILS.cssPath(targetElement, false);
                        message.xPath = UTILS.xPath(targetElement, true);
                        targetElement.contentDocument.openrpautil.parent = message;
                    }
                } catch (e) {
                    setTimeout(doFrames, 500);
                }
            };
            doFrames();
        }
        if (!document.URL.startsWith("https://docs.google.com/spreadsheets/d")) {
            window.addEventListener('load', notifyFrames);
        } else {
            console.log("skip google docs");
        }


        var runtimeOnMessage = function (sender, message, fnResponse) {
            try {
                if (openrpautil == undefined) return;
                var func = openrpautil[sender.functionName];
                if (func) {
                    var result = func(sender);
                    if (result == null) {
                        console.warn(sender.functionName + " gave no result.");
                        fnResponse(sender);
                    } else {
                        fnResponse(result);
                    }
                }
                else {
                    sender.error = "Unknown function " + sender.functionName;
                    fnResponse(sender);
                }
            } catch (e) {
                console.error('chrome.runtime.onMessage: error ');
                console.error(e);
                sender.error = e;
                fnResponse(sender);
            }
        }
        chrome.runtime.onMessage.addListener(runtimeOnMessage);
        window.openrpautil_contentlistner = true;
        if (typeof document.openrpautil === 'undefined') {
            console.debug('declaring openrpautil class 1');
            document.openrpautil = {};
            var last_mousemove = null;
            var cache = {};
            var cachecount = 0;
            var openrpautil = {
                parent: null,
                ping: function () {
                    return "pong";
                },
                init: function () {
                    if (document.URL.startsWith("https://docs.google.com/spreadsheets/d")) {
                        console.log("skip google docs");
                        return;
                    }
                    document.addEventListener('mousemove', function (e) { openrpautil.pushEvent('mousemove', e); }, true);
                    if (inIframe()) return;
                    document.addEventListener('click', function (e) { openrpautil.pushEvent('click', e); }, true);
                    document.addEventListener('keydown', function (e) { openrpautil.pushEvent('keydown', e); }, true);
                    document.addEventListener('keypress', function (e) { openrpautil.pushEvent('keyup', e); }, true);
                    document.addEventListener('mousedown', function (e) { openrpautil.pushEvent('mousedown', e); }, true);
                },
                findform: function (element) {
                    try {
                        var form = null;
                        var ele = element;
                        while (ele && !form) {
                            var name = ele.localName;
                            if (!name) {
                                ele = ele.parentNode;
                                continue;
                            }
                            name = name.toLowerCase();
                            if (name === "form") form = ele;
                            ele = ele.parentNode;
                        }
                        return form;
                    } catch (e) {
                        console.error(e);
                        return null;
                    }
                },
                clickelement: function (message) {
                    document.openrpadebug = message.debug;
                    if (message.uniquexpathids) document.openrpauniquexpathids = message.uniquexpathids;
                    var ele = null;
                    if (ele === null && message.zn_id !== null && message.zn_id !== undefined && message.zn_id > -1) {
                        message.xPath = '//*[@zn_id="' + message.zn_id + '"]';
                    }
                    if (message.xPath) {
                        var xpathEle = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (xpathEle === null) message.xPath = 'false';
                        if (xpathEle !== null) message.xPath = 'true';
                        ele = xpathEle;
                    }
                    if (message.cssPath && ele === null) {
                        var cssEle = document.querySelector(message.cssPath);
                        if (cssEle === null) message.cssPath = 'false';
                        if (cssEle !== null) message.cssPath = 'true';
                        ele = cssEle;
                    }
                    try {
                        if (ele !== null && ele !== undefined) {
                            var tagname = ele.tagName;
                            var tagtype = ele.getAttribute("type");
                            if (tagname) tagname = tagname.toLowerCase();
                            if (tagtype) tagtype = tagtype.toLowerCase();
                            if (tagname == "input" || tagtype == "type") {
                                var events = ["mousedown", "mouseup", "click", "submit"];
                                for (var i = 0; i < events.length; ++i) {
                                    simulate(ele, events[i]);
                                }
                            } else {
                                var events = ["mousedown", "mouseup", "click"];
                                for (var i = 0; i < events.length; ++i) {
                                    simulate(ele, events[i]);
                                }
                            }
                        }
                    } catch (e) {
                        console.error(e);
                        message.error = e;
                    }
                    var test = JSON.parse(JSON.stringify(message));
                    if (document.openrpadebug) console.log(test);
                    return test;
                },
                focuselement: function (message) {
                    document.openrpadebug = message.debug;
                    if (message.uniquexpathids) document.openrpauniquexpathids = message.uniquexpathids;
                    var ele = null;
                    if (ele === null && message.zn_id !== null && message.zn_id !== undefined && message.zn_id > -1) {
                        message.xPath = '//*[@zn_id="' + message.zn_id + '"]';
                    }
                    if (message.xPath) {
                        var xpathEle = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (xpathEle === null) message.xPath = 'false';
                        if (xpathEle !== null) message.xPath = 'true';
                        ele = xpathEle;
                    }
                    if (message.cssPath && ele === null) {
                        var cssEle = document.querySelector(message.cssPath);
                        if (cssEle === null) message.cssPath = 'false';
                        if (cssEle !== null) message.cssPath = 'true';
                        ele = cssEle;
                    }
                    try {
                        if (ele !== null && ele !== undefined) {
                            ele.scrollIntoView({ block: "center", behaviour: "smooth" });
                            var eventType = "onfocusin" in ele ? "focusin" : "focus",
                                bubbles = "onfocusin" in ele,
                                event;

                            if ("createEvent" in document) {
                                event = document.createEvent("Event");
                                event.initEvent(eventType, bubbles, true);
                            }
                            else if ("Event" in window) {
                                event = new Event(eventType, { bubbles: bubbles, cancelable: true });
                            }

                            ele.focus();
                            ele.dispatchEvent(event);
                            // getelement(message);
                        }
                    } catch (e) {
                        console.error(e);
                        message.error = e;
                    }
                    var test = JSON.parse(JSON.stringify(message));
                    if (document.openrpadebug) console.log(test);
                    return test;
                },
                getelements: function (message) {
                    try {
                        document.openrpadebug = message.debug;
                        if (message.uniquexpathids) document.openrpauniquexpathids = message.uniquexpathids;
                        var fromele = null;
                        if (message.fromxPath != null && message.fromxPath != "") {
                            if (message.fromxPath != null && message.fromxPath != "") {
                                if (document.openrpadebug) console.log("fromele = document.evaluate('" + message.fromxPath + "', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;");
                                fromele = document.evaluate(message.fromxPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                            } else if (message.fromcssPath != null && message.fromcssPath != "") {
                                if (document.openrpadebug) console.log("fromele = document.querySelector('" + message.fromcssPath + "');");
                                fromele = document.querySelector(message.fromcssPath);
                            }
                            if (fromele == null) {
                                var test = JSON.parse(JSON.stringify(message));
                                if (document.openrpadebug) console.log("null hits when searching for anchor (from element!)");
                                return test;
                            }
                            if (document.openrpadebug) console.log(fromele);
                        }
                        var ele = [];
                        if (ele === null && message.zn_id !== null && message.zn_id !== undefined && message.zn_id > -1) {
                            message.xPath = '//*[@zn_id="' + message.zn_id + '"]';
                        }
                        if (ele.length === 0 && message.xPath) {
                            //var iterator = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
                            var iterator;
                            var xpath = message.xPath;
                            var searchfrom = document;
                            if (fromele) {
                                var prexpath = UTILS.xPath(fromele, true);
                                // xpath = prexpath + message.xPath.substr(1, message.xPath.length - 1);
                                xpath = prexpath + message.xPath;
                                searchfrom = document;
                            }
                            iterator = document.evaluate(xpath, searchfrom, null, XPathResult.ANY_TYPE, null);

                            if (document.openrpadebug) console.log("document.evaluate('" + xpath + "', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);");
                            try {
                                var thisNode = iterator.iterateNext();
                                if (thisNode && document.openrpadebug) console.log(thisNode);

                                while (thisNode) {
                                    ele.push(thisNode);
                                    thisNode = iterator.iterateNext();
                                }
                            }
                            catch (e) {
                                console.error('Error: Document tree modified during iteration ' + e);
                            }
                            if (ele.length === 0) message.xPath = 'false';
                            if (ele.length > 0) message.xPath = 'true';
                        }
                        if (ele.length === 0 && message.cssPath) {
                            if (fromele == null) {
                                ele = document.querySelectorAll(message.cssPath);
                                if (document.openrpadebug) console.log("document.querySelector('" + message.cssPath + "');");
                            } else {
                                ele = fromele.querySelectorAll(message.cssPath);
                                if (document.openrpadebug) console.log("fromele.querySelector('" + message.cssPath + "');");
                            }

                            if (ele.length === 0) message.cssPath = 'false';
                            if (ele.length > 0) message.cssPath = 'true';
                        }
                        var base = Object.assign({}, message);
                        message.results = [];
                        notifyFrames();
                        if (ele.length > 0) {
                            try {
                                for (var i = 0; i < ele.length; i++) {
                                    var result = Object.assign({}, base);
                                    if (message.data === 'getdom') {
                                        console.log('getdom');
                                        result.result = openrpautil.mapDOM(ele[i], false, true);
                                    }
                                    else {
                                        result.result = openrpautil.mapDOM(ele[i], false);
                                    }
                                    try {
                                        openrpautil.applyPhysicalCords(result, ele[i]);
                                    } catch (e) {
                                        console.error(e);
                                    }
                                    result.zn_id = openrpautil.getuniqueid(ele[i]);

                                    if (openrpautil.parent != null) {
                                        result.parents = openrpautil.parent.parents + 1;
                                        result.uix += openrpautil.parent.uix;
                                        result.uiy += openrpautil.parent.uiy;
                                        result.xpaths = openrpautil.parent.xpaths.slice(0);
                                    } else if (inIframe()) {
                                        // TODO: exit?
                                        //return;
                                        var currentFramePosition = openrpautil.currentFrameAbsolutePosition();
                                        // console.log({ uix: result.uix, uiy: result.uiy, parent: result.parents }, currentFramePosition);
                                        result.uix += currentFramePosition.x;
                                        result.uiy += currentFramePosition.y;
                                    }

                                    message.results.push(result);
                                }
                            } catch (e) {
                                console.error(e);
                            }
                        } else {
                        }
                    } catch (e) {
                        console.error('error in getelements');
                        message.error = e;
                        console.error(e);
                    }
                    var test = JSON.parse(JSON.stringify(message));
                    if (document.openrpadebug) console.log(test);
                    return test;
                },
                getelement: function (message) {
                    document.openrpadebug = message.debug;
                    if (message.uniquexpathids) document.openrpauniquexpathids = message.uniquexpathids;
                    try {
                        var ele = null;
                        if (ele === null && message.zn_id !== null && message.zn_id !== undefined && message.zn_id > -1) {
                            message.xPath = '//*[@zn_id="' + message.zn_id + '"]';
                        }
                        if (ele === null && message.xPath) {
                            var xpathEle = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                            if (document.openrpadebug) console.log("document.evaluate('" + message.xPath + "', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;");
                            if (xpathEle === null) message.xPath = 'false';
                            if (xpathEle !== null) message.xPath = 'true';
                            ele = xpathEle;
                        }
                        if (ele === null && message.cssPath) {
                            var cssEle = document.querySelector(message.cssPath);
                            if (document.openrpadebug) console.log("document.querySelector('" + message.cssPath + "');");
                            if (cssEle === null) message.cssPath = 'false';
                            if (cssEle !== null) message.cssPath = 'true';
                            ele = cssEle;
                        }
                        if (ele !== null && ele !== undefined) {
                            try {
                                try {
                                    openrpautil.applyPhysicalCords(message, ele);
                                } catch (e) {
                                    console.error(e);
                                }
                            } catch (e) {
                                console.error(e);
                            }
                        }
                        if (ele !== null) {
                            if (message.data === 'getdom') {
                                message.result = openrpautil.mapDOM(ele, true, true, false);
                            }
                            else if (message.data === 'innerhtml')
                            {
                                message.result = openrpautil.mapDOM(ele, true, true, true);
                            }
                            else {
                                message.result = openrpautil.mapDOM(ele, true);
                            }
                            message.zn_id = openrpautil.getuniqueid(ele);



                            if (openrpautil.parent != null) {
                                message.parents = openrpautil.parent.parents + 1;
                                message.uix += openrpautil.parent.uix;
                                message.uiy += openrpautil.parent.uiy;
                                message.xpaths = openrpautil.parent.xpaths.slice(0);
                            } else if (inIframe()) {
                                // TODO: exit?
                                //return;
                                var currentFramePosition = openrpautil.currentFrameAbsolutePosition();
                                message.uix += currentFramePosition.x;
                                message.uiy += currentFramePosition.y;
                            }

                        }

                    } catch (e) {
                        console.error('error in getelement');
                        message.error = e;
                        console.error(e);
                    }
                    var test = JSON.parse(JSON.stringify(message));
                    if (document.openrpadebug) console.log(test);
                    return test;
                },
                updateelementvalue: function (message) {
                    var ele = null;
                    if (ele === null && message.zn_id !== null && message.zn_id !== undefined && message.zn_id > -1) {
                        message.xPath = '//*[@zn_id="' + message.zn_id + '"]';
                        var znEle = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (znEle === null) message.xPath = 'false';
                        if (znEle !== null) message.xPath = 'true';
                        ele = znEle;
                    }
                    if (ele === null && message.xPath) {
                        var xpathEle = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (xpathEle === null) message.xPath = 'false';
                        if (xpathEle !== null) message.xPath = 'true';
                        ele = xpathEle;
                    }
                    if (ele === null && message.cssPath) {
                        var cssEle = document.querySelector(message.cssPath);
                        if (cssEle === null) message.cssPath = 'false';
                        if (cssEle !== null) message.cssPath = 'true';
                        ele = cssEle;
                    }
                    if (ele) {
                        var data = message.data;
                        try {
                            data = Base64.decode(data);
                        } catch (e) {
                            console.error(e);
                            console.log(data);
                        }
                        if (document.openrpadebug) console.log('focus', ele);
                        ele.focus();
                        if (ele.tagName == "INPUT" && ele.getAttribute("type") == "checkbox") {
                            if (data === true || data === "true" || data === "True") {
                                if (document.openrpadebug) console.log('set checked = true');
                                ele.checked = true;
                            } else {
                                if (document.openrpadebug) console.log('set checked = false');
                                ele.checked = false;
                            }
                        } else if (message.result == "innerhtml") {
                            if (document.openrpadebug) console.log('set value', data);
                            ele.innerHTML = data;
                        } else if (ele.tagName == "DIV") {
                            if (document.openrpadebug) console.log('set value', data);
                            ele.innerText = data;
                        } else {
                            if (document.openrpadebug) console.log('set value', data);
                            ele.value = data;
                        }
                        try {
                            var evt = document.createEvent("HTMLEvents");
                            evt.initEvent("change", true, true);
                            ele.dispatchEvent(evt);
                        } catch (e) {
                            console.error(e);
                        }
                        try {
                            var evt = document.createEvent("HTMLEvents");
                            evt.initEvent("input", true, true);
                            ele.dispatchEvent(evt);
                        } catch (e) {
                            console.error(e);
                        }
                        //ele.blur();
                        //var events = ["keydown", "keyup", "keypress"];
                        //for (var i = 0; i < events.length; ++i) {
                        //    simulate(ele, events[i]);
                        //}
                    }
                    var test = JSON.parse(JSON.stringify(message));
                    return test;
                },
                updateelementvalues: function (message) {
                    var ele = null;
                    if (ele === null && message.zn_id !== null && message.zn_id !== undefined && message.zn_id > -1) {
                        message.xPath = '//*[@zn_id="' + message.zn_id + '"]';
                        var znEle = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (znEle === null) message.xPath = 'false';
                        if (znEle !== null) message.xPath = 'true';
                        ele = znEle;
                    }
                    if (ele === null && message.xPath) {
                        var xpathEle = document.evaluate(message.xPath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (xpathEle === null) message.xPath = 'false';
                        if (xpathEle !== null) message.xPath = 'true';
                        ele = xpathEle;
                    }
                    if (ele === null && message.cssPath) {
                        var cssEle = document.querySelector(message.cssPath);
                        if (cssEle === null) message.cssPath = 'false';
                        if (cssEle !== null) message.cssPath = 'true';
                        ele = cssEle;
                    }
                    if (ele) {
                        var data = message.data;
                        try {
                            data = Base64.decode(data);
                        } catch (e) {
                            console.error(e);
                            console.log(data);
                        }
                        ele.focus();
                        var values = JSON.parse(data);
                        if (ele.tagName && ele.tagName.toLowerCase() == "select") {
                            for (i = 0; i < ele.options.length; i++) {
                                if (values.indexOf(ele.options[i].value) > -1) {
                                    ele.options[i].selected = true;
                                } else { ele.options[i].selected = false; }
                            }
                        }

                        try {
                            var evt = document.createEvent("HTMLEvents");
                            evt.initEvent("change", true, true);
                            ele.dispatchEvent(evt);
                        } catch (e) {
                            console.error(e);
                        }
                        try {
                            var evt = document.createEvent("HTMLEvents");
                            evt.initEvent("input", true, true);
                            ele.dispatchEvent(evt);
                        } catch (e) {
                            console.error(e);
                        }
                        //ele.blur();
                        //var events = ["keydown", "keyup", "keypress"];
                        //for (var i = 0; i < events.length; ++i) {
                        //    simulate(ele, events[i]);
                        //}
                    }
                    var test = JSON.parse(JSON.stringify(message));
                    return test;
                },
                applyPhysicalCords: function (message, ele) {
                    var ClientRect = ele.getBoundingClientRect();
                    var devicePixelRatio = window.devicePixelRatio || 1;
                    var scrollLeft = (((t = document.documentElement) || (t = document.body.parentNode)) && typeof t.scrollLeft === 'number' ? t : document.body).scrollLeft;
                    message.x = Math.floor(ClientRect.left);
                    message.y = Math.floor(ClientRect.top);
                    message.width = Math.floor(ele.offsetWidth);
                    message.height = Math.floor(ele.offsetHeight);
                    message.uiwidth = Math.round(ele.offsetWidth * devicePixelRatio);
                    message.uiheight = Math.round(ele.offsetHeight * devicePixelRatio);
                    if (window.self === window.top) {
                        message.uix = Math.round((ClientRect.left - scrollLeft) * devicePixelRatio);
                        message.uiy = Math.round((ClientRect.top * devicePixelRatio) + (window.outerHeight - (window.innerHeight * devicePixelRatio)));
                    } else {
                        message.uix = Math.round(ClientRect.left * devicePixelRatio);
                        message.uiy = Math.round(ClientRect.top * devicePixelRatio);
                    }
                    if (inIframe() == false) {
                        var isAtMaxWidth = screen.availWidth - window.innerWidth === 0;
                        if (isAtMaxWidth) {
                            var isFirefox = typeof InstallTrigger !== 'undefined';
                            if (isFirefox) {
                                message.uix += 8;
                                message.uiy -= 7;
                            } else {
                                message.uix += 8;
                                message.uiy += 8;
                            }
                        } else {
                            message.uix += 7;
                            message.uiy -= 7;
                        }
                    //} else {
                    //    message.uix += 1;
                    //    message.uiy += 1;
                    }
                },
                // https://stackoverflow.com/questions/53056796/getboundingclientrect-from-within-iframe
                currentFrameAbsolutePosition: function () {
                    let currentWindow = window;
                    let currentParentWindow;
                    let positions = [];
                    let rect;
                    if (inIframe()) {
                    }
                    currentParentWindow = parent;
                    while (currentWindow !== window.top) {
                        for (let idx = 0; idx < currentParentWindow.frames.length; idx++)
                            if (currentParentWindow.frames[idx] === currentWindow) {
                                // for (let frameElement of currentParentWindow.document.getElementsByTagName('iframe')) {
                                for (let t = 0; t < currentParentWindow.frames.length; t++) {
                                    try {
                                        let frameElement = currentParentWindow.frames[t];

                                        if (typeof frameElement.getBoundingClientRect === "function") {
                                            rect = frameElement.getBoundingClientRect();

                                            positions.push({ x: rect.x, y: rect.y });
                                        } else if (frameElement.frameElement != null && typeof frameElement.frameElement.getBoundingClientRect === "function") {
                                            rect = frameElement.frameElement.getBoundingClientRect();
                                            positions.push({ x: rect.x, y: rect.y });
                                        } else if (frameElement.window != null && typeof frameElement.window.getBoundingClientRect === "function") {
                                            rect = frameElement.window.getBoundingClientRect();
                                            positions.push({ x: rect.x, y: rect.y });
                                        } else if (frameElement.contentWindow === currentWindow) {
                                            rect = frameElement.getBoundingClientRect();

                                            positions.push({ x: rect.x, y: rect.y });
                                        } else if (frameElement.window === currentWindow) {
                                            if (typeof frameElement.getBoundingClientRect === "function") {
                                                rect = frameElement.getBoundingClientRect();

                                                positions.push(rect);
                                            } else if (frameElement.frameElement != null && typeof frameElement.frameElement.getBoundingClientRect === "function") {
                                                rect = frameElement.frameElement.getBoundingClientRect();

                                                positions.push(rect);
                                            } else {
                                                positions.push({ x: 0, y: 0 });
                                            }
                                            
                                        }
                                    } catch (e) {
                                        // console.debug(e);
                                        // console.error(e);
                                        break;
                                    }
                                }
                                //for (let frameElement of currentParentWindow.frames) {
                                //}

                                currentWindow = currentParentWindow;
                                currentParentWindow = currentWindow.parent;
                                break;
                            }
                    }
                    
                    var result = positions.reduce((accumulator, currentValue) => {
                        return {
                            x: (accumulator.x + currentValue.x) | 0,
                            y: (accumulator.y + currentValue.y) | 0
                        };
                    }, { x: 0, y: 0 });
                    return result;
                },
                getOffset: function (el) {
                    var _x = 0;
                    var _y = 0;
                    while (el && !isNaN(el.offsetLeft) && !isNaN(el.offsetTop)) {
                        _x += el.offsetLeft - el.scrollLeft;
                        _y += el.offsetTop - el.scrollTop;
                        el = el.offsetParent;
                    }
                    return { top: _y, left: _x };
                },
                pushEvent: function (action, event) {
                    let frame = -1;
                    if (window.frameElement) frame = window.frameElement.id;
                    if (action === 'keydown') {
                        chrome.runtime.sendMessage({ functionName: action, key: String.fromCharCode(event.which) });
                    }
                    else if (action === 'keyup') {
                        chrome.runtime.sendMessage({ functionName: action, key: String.fromCharCode(event.which) });
                    }
                    else {
                        // https://www.jeffersonscher.com/res/resolution.php

                        // https://stackoverflow.com/questions/3437786/get-the-size-of-the-screen-current-web-page-and-browser-window

                        var message = { functionName: action, frame: frame, parents: 0, xpaths: [] };
                        var targetElement = null;
                        targetElement = event.target || event.srcElement;
                        if (targetElement == null) {
                            console.log('targetElement == null');
                            return;
                        }
                        if (action === 'mousemove') {
                            //if (last_mousemove === targetElement) {
                            //    return;
                            //}
                            last_mousemove = targetElement;
                        }
                        try {
                            openrpautil.applyPhysicalCords(message, targetElement);
                        } catch (e) {
                            console.error(e);
                        }
                        // console.log(openrpautil.parent);
                        if (openrpautil.parent != null) {
                            message.parents = openrpautil.parent.parents + 1;
                            message.uix += openrpautil.parent.uix;
                            message.uiy += openrpautil.parent.uiy;
                            message.xpaths = openrpautil.parent.xpaths.slice(0);
                            //message.x += parent.uix;
                            //message.y += parent.uiy;
                            //message.width += parent.width;
                            //message.height += parent.height;
                        } else if (inIframe()) {
                            // TODO: exit?
                            //return;
                            var currentFramePosition = openrpautil.currentFrameAbsolutePosition();
                            // console.log({ uix: message.uix, uiy: message.uiy, parent: message.parents }, currentFramePosition);
                            message.uix += currentFramePosition.x;
                            message.uiy += currentFramePosition.y;
                        }
                        // console.log('inIframe: ' + inIframe());
                        message.cssPath = UTILS.cssPath(targetElement, false);
                        message.xPath = UTILS.xPath(targetElement, true);
                        message.zn_id = openrpautil.getuniqueid(targetElement);
                        message.c = targetElement.childNodes.length;
                        message.result = openrpautil.mapDOM(targetElement, true);
                        //if (targetElement.tagName == "IFRAME" || targetElement.tagName == "FRAME") {
                        message.xpaths.push(message.xPath);
                        //if (document.openrpadebug)
                        // console.log({ uix: message.uix, uiy: message.uiy, parent: message.parents })
                        //console.log({ x: message.x, y: message.y, uix: message.uix, uiy: message.uiy, parent: message.parents })

                        // console.log(targetElement.tagName + ' ' + message.xPath);
                        if (targetElement.contentWindow) {
                            var iframeWin = targetElement.contentWindow;
                            iframeWin.postMessage(message, '*');
                            console.log('targetElement.tagName == iframe or frame');
                            return;
                        }

                        chrome.runtime.sendMessage(message);
                    }
                },
                getuniqueid: function (element) {
                    if (element === null || element === undefined) return null;
                    if (element.attributes === null || element.attributes === undefined) return null;
                    for (var r = 0; r < element.attributes.length; r++) {
                        var name = element.attributes[r].nodeName;
                        if (name === 'zn_id') return element.attributes[r].nodeValue;
                    }
                    if (element === null || element === undefined) return null;
                    if (element.attributes === null || element.attributes === undefined) return null;
                    ++cachecount;
                    element.setAttribute('zn_id', cachecount);
                    return cachecount;
                },
                executescript: function (message) {
                    try {
                        if (document.openrpadebug) console.log('script', message.script);
                        message.result = eval(message.script);
                        if (document.openrpadebug) console.log('result', message.result);
                    } catch (e) {
                        console.error(e);
                        message.error = e;
                    }
                    delete message.script;
                    var test = JSON.parse(JSON.stringify(message));
                    if (document.openrpadebug) console.log(test);
                    return test;                    
                },
                fullPath: function (el) {
                    var names = [];
                    while (el.parentNode) {
                        if (el.id) {
                            names.unshift('#' + el.id);
                            break;
                        } else {
                            if (el === el.ownerDocument.documentElement) names.unshift(el.tagName);
                            else {
                                for (var c = 1, e = el; e.previousElementSibling; e = e.previousElementSibling, c++);
                                names.unshift(el.tagName + ":nth-child(" + c + ")");
                            }
                            el = el.parentNode;
                        }
                    }
                    return names.join(" > ");
                },
                toJSON: function (node, maxiden, ident) {
                    if (ident === null || ident === undefined) ident = 0;
                    if (maxiden === null || maxiden === undefined) ident = 1;

                    node = node || this;
                    var obj = {
                        nodeType: node.nodeType
                    };
                    if (node.tagName) {
                        obj.tagName = node.tagName.toLowerCase();
                    } else
                        if (node.nodeName) {
                            obj.nodeName = node.nodeName;
                        }
                    if (node.nodeValue) {
                        obj.nodeValue = node.nodeValue;
                    }
                    var attrs = node.attributes;
                    if (attrs) {
                        var length = attrs.length;
                        var arr = obj.attributes = new Array(length);
                        for (var i = 0; i < length; i++) {
                            attr = attrs[i];
                            arr[i] = [attr.nodeName, attr.nodeValue];
                        }
                    }
                    var childNodes = node.childNodes;
                    if (childNodes && ident < maxiden) {
                        length = childNodes.length;
                        arr = obj.childNodes = new Array(length);
                        for (i = 0; i < length; i++) {
                            arr[i] = openrpautil.toJSON(childNodes[i], maxiden, ident + 1);
                        }
                    }
                    return obj;
                },
                toDOM: function (obj) {
                    if (typeof obj === 'string') {
                        obj = JSON.parse(obj);
                    }
                    var node, nodeType = obj.nodeType;
                    switch (nodeType) {
                        case 1: //ELEMENT_NODE
                            node = document.createElement(obj.tagName);
                            var attributes = obj.attributes || [];
                            for (var i = 0, len = attributes.length; i < len; i++) {
                                var attr = attributes[i];
                                node.setAttribute(attr[0], attr[1]);
                            }
                            break;
                        case 3: //TEXT_NODE
                            node = document.createTextNode(obj.nodeValue);
                            break;
                        case 8: //COMMENT_NODE
                            node = document.createComment(obj.nodeValue);
                            break;
                        case 9: //DOCUMENT_NODE
                            node = document.implementation.createDocument();
                            break;
                        case 10: //DOCUMENT_TYPE_NODE
                            node = document.implementation.createDocumentType(obj.nodeName);
                            break;
                        case 11: //DOCUMENT_FRAGMENT_NODE
                            node = document.createDocumentFragment();
                            break;
                        default:
                            return node;
                    }
                    if (nodeType === 1 || nodeType === 11) {
                        var childNodes = obj.childNodes || [];
                        for (i = 0, len = childNodes.length; i < len; i++) {
                            node.appendChild(openrpautil.toDOM(childNodes[i]));
                        }
                    }
                    return node;
                },
                mapDOM: function (element, json, mapdom, innerhtml) {
                    var maxiden = 40;
                    if (mapdom !== true) maxiden = 1;
                    if (maxiden === null || maxiden === undefined) maxiden = 20;
                    var treeObject = {};
                    // If string convert to document Node
                    if (typeof element === "string") {
                        if (window.DOMParser) {
                            parser = new DOMParser();
                            docNode = parser.parseFromString(element, "text/xml");
                        } else { // Microsoft strikes again
                            docNode = new ActiveXObject("Microsoft.XMLDOM");
                            docNode.async = false;
                            docNode.loadXML(element);
                        }
                        element = docNode.firstChild;
                    }
                    //Recursively loop through DOM elements and assign properties to object
                    function treeHTML(element, object, maxiden, ident) {
                        if (ident === null || ident === undefined) ident = 0;
                        if (maxiden === null || maxiden === undefined) maxiden = 1;
                        openrpautil.getuniqueid(element);
                        object["tagName"] = element.tagName;
                        if (ident === 0) {
                            object["xPath"] = UTILS.xPath(element, true);
                            object["cssPath"] = UTILS.cssPath(element, false);
                            if (object["tagName"] !== 'STYLE' && object["tagName"] !== 'SCRIPT' && object["tagName"] !== 'HEAD' && object["tagName"] !== 'HTML') {
                                if (element.innerText !== undefined && element.innerText !== null && element.innerText !== '') {
                                    object["innerText"] = element.innerText;
                                }
                            }
                        }
                        var nodeList = element.childNodes;
                        if (nodeList) {
                            if (nodeList.length) {
                                object["content"] = [];
                                for (var i = 0; i < nodeList.length; i++) {
                                    if (nodeList[i].nodeType === 3) {
                                        if (mapdom !== true) {
                                            if (object["tagName"] !== 'STYLE' && object["tagName"] !== 'SCRIPT' && object["tagName"] !== 'HEAD') {
                                                object["content"].push(nodeList[i].nodeValue);
                                            }
                                        }
                                    } else {
                                        if (ident < maxiden) {
                                            object["content"].push({});
                                            treeHTML(nodeList[i], object["content"][object["content"].length - 1], maxiden, ident + 1);
                                        }
                                    }
                                }
                            }
                        }
                        if (element.attributes) {
                            if (element.attributes.length) {
                                var wasDisabled = false;
                                // To read values of disabled objects, we need to undisable them
                                //if (element.disabled === true) {
                                //    console.log('removing disabled!!!!');
                                //    wasDisabled = true;
                                //    //element.disabled == false;
                                //    element.removeAttribute("disabled");
                                //}
                                var attributecount = 0;
                                if (element.attributes["zn_id"] == undefined || element.attributes["zn_id"] == null) {
                                    var zn_id = openrpautil.getuniqueid(element);
                                }
                                object["zn_id"] = element.attributes["zn_id"].nodeValue;
                                for (var r = 0; r < element.attributes.length; r++) {
                                    var name = element.attributes[r].nodeName;
                                    var value = element.attributes[r].nodeValue;
                                    // value, innertext
                                    if (ident === 0) {
                                        if (mapdom !== true || name.toLowerCase() === 'zn_id') {
                                            object[name] = value;
                                            ++attributecount;
                                        }
                                        //if (['zn_id', 'id', 'classname', 'name', 'tagname', 'href', 'src', 'alt', 'clientrects'].includes(name.toLowerCase())) {
                                        //    //object["attributes"][name] = value;
                                        //    object[name] = value;
                                        //    ++attributecount;
                                        //}
                                    }
                                    else if (ident > 0 && mapdom === true) {
                                        if (name.toLowerCase() === 'zn_id') {
                                            //object["attributes"][name] = value;
                                            object[name] = value;
                                            ++attributecount;
                                        }
                                    }
                                }
                                //if (attributecount === 0) delete object["attributes"];
                                if (wasDisabled === true) {
                                    if (ident === 0) {
                                        //element.disabled == true;
                                        element.setAttribute("disabled", "true");
                                    }
                                }
                            }
                        }
                    }
                    treeHTML(element, treeObject, maxiden);
                    treeObject["value"] = element.value;
                    treeObject["isvisible"] = openrpautil.isVisible(element);
                    treeObject["display"] = openrpautil.display(element);
                    treeObject["isvisibleonscreen"] = openrpautil.isVisibleOnScreen(element);
                    treeObject["disabled"] = element.disabled;
                    treeObject["innerText"] = element.innerText;
                    if (innerhtml) {
                        treeObject["innerhtml"] = element.innerHTML;
                    }
                    if (element.tagName == "INPUT" && element.getAttribute("type") == "checkbox" ) {
                        treeObject["checked"] = element.checked;
                    }
                    if (element.tagName && element.tagName.toLowerCase() == "options") {
                        treeObject["selected"] = element.selected;
                    }
                    if (element.tagName && element.tagName.toLowerCase() == "select") {
                        var selectedvalues = [];
                        for (i = 0; i < element.options.length; i++) {
                            if (element.options[i].selected) {
                                selectedvalues.push(element.options[i].value);
                                treeObject["text"] = element.options[i].text;
                            }
                        } 
                        treeObject["values"] = selectedvalues;
                    }

                    //updateelementtext
                    if (treeObject["disabled"] === null || treeObject["disabled"] === undefined) treeObject["disabled"] = false;
                    return json ? JSON.stringify(treeObject) : treeObject;
                },
                isVisibleOnScreen: function (elm) {
                    var rect = elm.getBoundingClientRect();
                    var viewHeight = Math.max(document.documentElement.clientHeight, window.innerHeight);
                    return !(rect.bottom < 0 || rect.top - viewHeight >= 0);
                },
                isVisible: function (elm) {
                    return elm.offsetWidth > 0 && elm.offsetHeight > 0;
                },
                display: function (elm) {
                    return window.getComputedStyle(elm, null).getPropertyValue('display');
                },
                getFrameName: function (frame) {
                    var frames = parent.frames,
                        l = frames.length,
                        name = null;
                    for (var x = 0; x < l; x++) {
                        if (frames[x] === frame) {
                            name = frames[x].name;
                        }
                    }
                    return name;
                },
                screenInfo: function () {
                    return {
                        screen: {
                            availTop: window.screen.availTop,
                            availLeft: window.screen.availLeft,
                            availHeight: window.screen.availHeight,
                            availWidth: window.screen.availWidth,
                            colorDepth: window.screen.colorDepth,
                            height: window.screen.height,
                            left: window.screen.left,
                            orientation: window.screen.orientation,
                            pixelDepth: window.screen.pixelDepth,
                            top: window.screen.top,
                            width: window.screen.width
                        },
                        screenX: window.screenX,
                        screenY: window.screenY,
                        screenLeft: window.screenLeft,
                        screenTop: window.screenTop
                    };
                },
                getXPath(el) {
                    let nodeElem = el;
                    if (nodeElem.id && this.options.shortid) {
                        return `//*[@id="${nodeElem.id}"]`;
                    }
                    const parts = [];
                    while (nodeElem && nodeElem.nodeType === Node.ELEMENT_NODE) {
                        let nbOfPreviousSiblings = 0;
                        let hasNextSiblings = false;
                        let sibling = nodeElem.previousSibling;
                        while (sibling) {
                            if (sibling.nodeType !== Node.DOCUMENT_TYPE_NODE && sibling.nodeName === nodeElem.nodeName) {
                                nbOfPreviousSiblings++;
                            }
                            sibling = sibling.previousSibling;
                        }
                        sibling = nodeElem.nextSibling;
                        while (sibling) {
                            if (sibling.nodeName === nodeElem.nodeName) {
                                hasNextSiblings = true;
                                break;
                            }
                            sibling = sibling.nextSibling;
                        }
                        const prefix = nodeElem.prefix ? nodeElem.prefix + ':' : '';
                        const nth = nbOfPreviousSiblings || hasNextSiblings ? `[${nbOfPreviousSiblings + 1}]` : '';
                        parts.push(prefix + nodeElem.localName + nth);
                        nodeElem = nodeElem.parentNode;
                    }
                    return parts.length ? '/' + parts.reverse().join('/') : '';
                }

            };
            document.openrpautil = openrpautil;
            openrpautil.init();


            function simulate(element, eventName) {
                var options = extend(defaultOptions, arguments[2] || {});
                var oEvent, eventType = null;

                for (var name in eventMatchers) {
                    if (eventMatchers[name].test(eventName)) { eventType = name; break; }
                }

                if (!eventType)
                    throw new SyntaxError('Only HTMLEvents and MouseEvents interfaces are supported');

                if (document.createEvent) {
                    oEvent = document.createEvent(eventType);
                    if (eventType == 'HTMLEvents') {
                        oEvent.initEvent(eventName, options.bubbles, options.cancelable);
                    }
                    else {
                        oEvent.initMouseEvent(eventName, options.bubbles, options.cancelable, document.defaultView,
                            options.button, options.pointerX, options.pointerY, options.pointerX, options.pointerY,
                            options.ctrlKey, options.altKey, options.shiftKey, options.metaKey, options.button, element);
                    }
                    element.dispatchEvent(oEvent);
                }
                else {
                    options.clientX = options.pointerX;
                    options.clientY = options.pointerY;
                    var evt = document.createEventObject();
                    oEvent = extend(evt, options);
                    element.fireEvent('on' + eventName, oEvent);
                }
                return element;
            }

            function extend(destination, source) {
                for (var property in source)
                    destination[property] = source[property];
                return destination;
            }

            var eventMatchers = {
                'HTMLEvents': /^(?:load|unload|abort|error|select|change|submit|reset|focus|blur|resize|scroll)$/,
                'MouseEvents': /^(?:click|dblclick|mouse(?:down|up|over|move|out))$/
            }
            var defaultOptions = {
                pointerX: 0,
                pointerY: 0,
                button: 0,
                ctrlKey: false,
                altKey: false,
                shiftKey: false,
                metaKey: false,
                bubbles: true,
                cancelable: true
            }




            // https://chromium.googlesource.com/chromium/blink/+/master/Source/devtools/front_end/components/DOMPresentationUtils.js
            // https://gist.github.com/asfaltboy/8aea7435b888164e8563
            /*
             * Copyright (C) 2015 Pavel Savshenko
             * Copyright (C) 2011 Google Inc.  All rights reserved.
             * Copyright (C) 2007, 2008 Apple Inc.  All rights reserved.
             * Copyright (C) 2008 Matt Lilek <webkit@mattlilek.com>
             * Copyright (C) 2009 Joseph Pecoraro
             *
             * Redistribution and use in source and binary forms, with or without
             * modification, are permitted provided that the following conditions
             * are met:
             *
             * 1.  Redistributions of source code must retain the above copyright
             *     notice, this list of conditions and the following disclaimer.
             * 2.  Redistributions in binary form must reproduce the above copyright
             *     notice, this list of conditions and the following disclaimer in the
             *     documentation and/or other materials provided with the distribution.
             * 3.  Neither the name of Apple Computer, Inc. ("Apple") nor the names of
             *     its contributors may be used to endorse or promote products derived
             *     from this software without specific prior written permission.
             *
             * THIS SOFTWARE IS PROVIDED BY APPLE AND ITS CONTRIBUTORS "AS IS" AND ANY
             * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
             * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
             * DISCLAIMED. IN NO EVENT SHALL APPLE OR ITS CONTRIBUTORS BE LIABLE FOR ANY
             * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
             * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
             * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
             * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
             * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
             * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
             */

            var UTILS = {};
            UTILS.xPath = function (node, optimized) {
                if (node.nodeType === Node.DOCUMENT_NODE)
                    return "/";
                var steps = [];
                var contextNode = node;
                while (contextNode) {
                    var step = UTILS._xPathValue(contextNode, optimized);
                    if (!step)
                        break; // Error - bail out early.
                    steps.push(step);
                    if (step.optimized)
                        break;
                    contextNode = contextNode.parentNode;
                }
                steps.reverse();
                return (steps.length && steps[0].optimized ? "" : "/") + steps.join("/");
            };
            UTILS._xPathValue = function (node, optimized) {
                var ownValue;
                var ownIndex = UTILS._xPathIndex(node);
                if (ownIndex === -1)
                    return null; // Error.
                switch (node.nodeType) {
                    case Node.ELEMENT_NODE:
                        ownValue = node.localName;
                        if (optimized) {
                            
                            for (var i = 0; i < document.openrpauniquexpathids.length; i++) {
                                var id = document.openrpauniquexpathids[i].toLowerCase();
                                if (node.getAttribute(id))
                                    return new UTILS.DOMNodePathStep("//" + ownValue + "[@" + id + "=\"" + node.getAttribute(id) + "\"]", true);
                                id = id.toUpperCase();
                                if (node.getAttribute(id))
                                    return new UTILS.DOMNodePathStep("//" + ownValue + "[@" + id + "=\"" + node.getAttribute(id) + "\"]", true);
                            }
                        }
                        if (optimized && node.getAttribute("id"))
                            return new UTILS.DOMNodePathStep("//" + ownValue + "[@id=\"" + node.getAttribute("id") + "\"]", true);
                        break;
                    case Node.ATTRIBUTE_NODE:
                        ownValue = "@" + node.nodename;
                        break;
                    case Node.TEXT_NODE:
                    case Node.CDATA_SECTION_NODE:
                        ownValue = "text()";
                        break;
                    case Node.PROCESSING_INSTRUCTION_NODE:
                        ownValue = "processing-instruction()";
                        break;
                    case Node.COMMENT_NODE:
                        ownValue = "comment()";
                        break;
                    case Node.DOCUMENT_NODE:
                        ownValue = "";
                        break;
                    default:
                        ownValue = "";
                        break;
                }
                if (ownIndex > 0)
                    ownValue += "[" + ownIndex + "]";
                return new UTILS.DOMNodePathStep(ownValue, node.nodeType === Node.DOCUMENT_NODE);
            };


            UTILS._xPathIndex = function (node) {
                // Returns -1 in case of error, 0 if no siblings matching the same expression, <XPath index among the same expression-matching sibling nodes> otherwise.
                function areNodesSimilar(left, right) {
                    if (left === right)
                        return true;
                    if (left.nodeType === Node.ELEMENT_NODE && right.nodeType === Node.ELEMENT_NODE)
                        return left.localName === right.localName;
                    if (left.nodeType === right.nodeType)
                        return true;
                    // XPath treats CDATA as text nodes.
                    var leftType = left.nodeType === Node.CDATA_SECTION_NODE ? Node.TEXT_NODE : left.nodeType;
                    var rightType = right.nodeType === Node.CDATA_SECTION_NODE ? Node.TEXT_NODE : right.nodeType;
                    return leftType === rightType;
                }
                var siblings = node.parentNode ? node.parentNode.children : null;
                if (!siblings)
                    return 0; // Root node - no siblings.
                var hasSameNamedElements;
                for (var i = 0; i < siblings.length; ++i) {
                    if (areNodesSimilar(node, siblings[i]) && siblings[i] !== node) {
                        hasSameNamedElements = true;
                        break;
                    }
                }
                if (!hasSameNamedElements)
                    return 0;
                var ownIndex = 1; // XPath indices start with 1.
                for (var z = 0; z < siblings.length; ++z) {
                    if (areNodesSimilar(node, siblings[z])) {
                        if (siblings[z] === node)
                            return ownIndex;
                        ++ownIndex;
                    }
                }
                return -1; // An error occurred: |node| not found in parent's children.
            };

            UTILS.cssPath = function (node, optimized) {
                if (node.nodeType !== Node.ELEMENT_NODE)
                    return "";
                var steps = [];
                var contextNode = node;
                while (contextNode) {
                    var step = UTILS._cssPathStep(contextNode, !!optimized, contextNode === node);
                    if (!step)
                        break; // Error - bail out early.
                    steps.push(step);
                    if (step.optimized)
                        break;
                    contextNode = contextNode.parentNode;
                }
                steps.reverse();
                return steps.join(" > ");
            };
            UTILS._cssPathStep = function (node, optimized, isTargetNode) {
                if (node.nodeType !== Node.ELEMENT_NODE)
                    return null;

                var id = node.getAttribute("id");
                if (optimized) {
                    if (id)
                        return new UTILS.DOMNodePathStep(idSelector(id), true);
                    var nodeNameLower = node.nodeName.toLowerCase();
                    if (nodeNameLower === "body" || nodeNameLower === "head" || nodeNameLower === "html")
                        return new UTILS.DOMNodePathStep(node.nodeName.toLowerCase(), true);
                }
                var nodeName = node.nodeName.toLowerCase();

                if (id && optimized)
                    return new UTILS.DOMNodePathStep(nodeName.toLowerCase() + idSelector(id), true);
                var parent = node.parentNode;
                if (!parent || parent.nodeType === Node.DOCUMENT_NODE)
                    return new UTILS.DOMNodePathStep(nodeName.toLowerCase(), true);
                function prefixedElementClassNames(node) {
                    var classAttribute = node.getAttribute("class");
                    if (!classAttribute)
                        return [];

                    return classAttribute.split(/\s+/g).filter(Boolean).map(function (name) {
                        // The prefix is required to store "__proto__" in a object-based map.
                        return "$" + name;
                    });
                }
                function idSelector(id) {
                    return "#" + escapeIdentifierIfNeeded(id);
                }
                function escapeIdentifierIfNeeded(ident) {
                    if (isCSSIdentifier(ident))
                        return ident;
                    var shouldEscapeFirst = /^(?:[0-9]|-[0-9-]?)/.test(ident);
                    var lastIndex = ident.length - 1;
                    return ident.replace(/./g, function (c, i) {
                        return shouldEscapeFirst && i === 0 || !isCSSIdentChar(c) ? escapeAsciiChar(c, i === lastIndex) : c;
                    });
                }
                function escapeAsciiChar(c, isLast) {
                    return "\\" + toHexByte(c) + (isLast ? "" : " ");
                }
                function toHexByte(c) {
                    var hexByte = c.charCodeAt(0).toString(16);
                    if (hexByte.length === 1)
                        hexByte = "0" + hexByte;
                    return hexByte;
                }
                function isCSSIdentChar(c) {
                    if (/[a-zA-Z0-9_-]/.test(c))
                        return true;
                    return c.charCodeAt(0) >= 0xA0;
                }
                function isCSSIdentifier(value) {
                    return /^-?[a-zA-Z_][a-zA-Z0-9_-]*$/.test(value);
                }
                var prefixedOwnClassNamesArray = prefixedElementClassNames(node);
                var needsClassNames = false;
                var needsNthChild = false;
                var ownIndex = -1;
                var siblings = parent.children;
                for (var i = 0; (ownIndex === -1 || !needsNthChild) && i < siblings.length; ++i) {
                    var sibling = siblings[i];
                    if (sibling === node) {
                        ownIndex = i;
                        continue;
                    }
                    if (needsNthChild)
                        continue;
                    if (sibling.nodeName.toLowerCase() !== nodeName.toLowerCase())
                        continue;

                    needsClassNames = true;
                    var ownClassNames = prefixedOwnClassNamesArray;
                    var ownClassNameCount = 0;
                    for (var name in ownClassNames)
                        ++ownClassNameCount;
                    if (ownClassNameCount === 0) {
                        needsNthChild = true;
                        continue;
                    }
                    var siblingClassNamesArray = prefixedElementClassNames(sibling);
                    for (var j = 0; j < siblingClassNamesArray.length; ++j) {
                        var siblingClass = siblingClassNamesArray[j];
                        if (ownClassNames.indexOf(siblingClass))
                            continue;
                        delete ownClassNames[siblingClass];
                        if (!--ownClassNameCount) {
                            needsNthChild = true;
                            break;
                        }
                    }
                }

                var result = nodeName.toLowerCase();
                if (isTargetNode && nodeName.toLowerCase() === "input" && node.getAttribute("type") && !node.getAttribute("id") && !node.getAttribute("class"))
                    result += "[type=\"" + node.getAttribute("type") + "\"]";
                if (needsNthChild) {
                    result += ":nth-child(" + (ownIndex + 1) + ")";
                } else if (needsClassNames) {
                    for (var prefixedName in prefixedOwnClassNamesArray)
                        // for (var prefixedName in prefixedOwnClassNamesArray.keySet())
                        result += "." + escapeIdentifierIfNeeded(prefixedOwnClassNamesArray[prefixedName].substr(1));
                }

                return new UTILS.DOMNodePathStep(result, false);
            };
            UTILS.DOMNodePathStep = function (value, optimized) {
                this.value = value;
                this.optimized = optimized || false;
            };
            UTILS.DOMNodePathStep.prototype = {
                toString: function () {
                    return this.value;
                }
            };

        }
    }
}

//
// THIS FILE IS AUTOMATICALLY GENERATED! DO NOT EDIT BY HAND!
//
; (function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined'
        ? module.exports = factory()
        : typeof define === 'function' && define.amd
            ? define(factory) :
            // cf. https://github.com/dankogai/js-base64/issues/119
            (function () {
                // existing version for noConflict()
                const _Base64 = global.Base64;
                const gBase64 = factory();
                gBase64.noConflict = () => {
                    global.Base64 = _Base64;
                    return gBase64;
                };
                if (global.Meteor) { // Meteor.js
                    Base64 = gBase64;
                }
                global.Base64 = gBase64;
            })();
}((typeof self !== 'undefined' ? self
    : typeof window !== 'undefined' ? window
        : typeof global !== 'undefined' ? global
            : this
), function () {
    'use strict';

    /**
     *  base64.ts
     *
     *  Licensed under the BSD 3-Clause License.
     *    http://opensource.org/licenses/BSD-3-Clause
     *
     *  References:
     *    http://en.wikipedia.org/wiki/Base64
     *
     * @author Dan Kogai (https://github.com/dankogai)
     */
    const version = '3.4.5';
    /**
     * @deprecated use lowercase `version`.
     */
    const VERSION = version;
    const _hasatob = typeof atob === 'function';
    const _hasbtoa = typeof btoa === 'function';
    const _hasBuffer = typeof Buffer === 'function';
    const b64ch = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=';
    const b64chs = [...b64ch];
    const b64tab = ((a) => {
        let tab = {};
        a.forEach((c, i) => tab[c] = i);
        return tab;
    })(b64chs);
    const b64re = /^(?:[A-Za-z\d+\/]{4})*?(?:[A-Za-z\d+\/]{2}(?:==)?|[A-Za-z\d+\/]{3}=?)?$/;
    const _fromCC = String.fromCharCode.bind(String);
    const _U8Afrom = typeof Uint8Array.from === 'function'
        ? Uint8Array.from.bind(Uint8Array)
        : (it, fn = (x) => x) => new Uint8Array(Array.prototype.slice.call(it, 0).map(fn));
    const _mkUriSafe = (src) => src
        .replace(/[+\/]/g, (m0) => m0 == '+' ? '-' : '_')
        .replace(/=+$/m, '');
    const _tidyB64 = (s) => s.replace(/[^A-Za-z0-9\+\/]/g, '');
    /**
     * polyfill version of `btoa`
     */
    const btoaPolyfill = (bin) => {
        // console.log('polyfilled');
        let u32, c0, c1, c2, asc = '';
        const pad = bin.length % 3;
        for (let i = 0; i < bin.length;) {
            if ((c0 = bin.charCodeAt(i++)) > 255 ||
                (c1 = bin.charCodeAt(i++)) > 255 ||
                (c2 = bin.charCodeAt(i++)) > 255)
                throw new TypeError('invalid character found');
            u32 = (c0 << 16) | (c1 << 8) | c2;
            asc += b64chs[u32 >> 18 & 63]
                + b64chs[u32 >> 12 & 63]
                + b64chs[u32 >> 6 & 63]
                + b64chs[u32 & 63];
        }
        return pad ? asc.slice(0, pad - 3) + "===".substring(pad) : asc;
    };
    /**
     * does what `window.btoa` of web browsers do.
     * @param {String} bin binary string
     * @returns {string} Base64-encoded string
     */
    const _btoa = _hasbtoa ? (bin) => btoa(bin)
        : _hasBuffer ? (bin) => Buffer.from(bin, 'binary').toString('base64')
            : btoaPolyfill;
    const _fromUint8Array = _hasBuffer
        ? (u8a) => Buffer.from(u8a).toString('base64')
        : (u8a) => {
            // cf. https://stackoverflow.com/questions/12710001/how-to-convert-uint8-array-to-base64-encoded-string/12713326#12713326
            const maxargs = 0x1000;
            let strs = [];
            for (let i = 0, l = u8a.length; i < l; i += maxargs) {
                strs.push(_fromCC.apply(null, u8a.subarray(i, i + maxargs)));
            }
            return _btoa(strs.join(''));
        };
    /**
     * converts a Uint8Array to a Base64 string.
     * @param {boolean} [urlsafe] URL-and-filename-safe a la RFC4648 §5
     * @returns {string} Base64 string
     */
    const fromUint8Array = (u8a, urlsafe = false) => urlsafe ? _mkUriSafe(_fromUint8Array(u8a)) : _fromUint8Array(u8a);
    /**
     * @deprecated should have been internal use only.
     * @param {string} src UTF-8 string
     * @returns {string} UTF-16 string
     */
    const utob = (src) => unescape(encodeURIComponent(src));
    //
    const _encode = _hasBuffer
        ? (s) => Buffer.from(s, 'utf8').toString('base64')
        : (s) => _btoa(utob(s));
    /**
     * converts a UTF-8-encoded string to a Base64 string.
     * @param {boolean} [urlsafe] if `true` make the result URL-safe
     * @returns {string} Base64 string
     */
    const encode = (src, urlsafe = false) => urlsafe
        ? _mkUriSafe(_encode(src))
        : _encode(src);
    /**
     * converts a UTF-8-encoded string to URL-safe Base64 RFC4648 §5.
     * @returns {string} Base64 string
     */
    const encodeURI = (src) => encode(src, true);
    /**
     * @deprecated should have been internal use only.
     * @param {string} src UTF-16 string
     * @returns {string} UTF-8 string
     */
    const btou = (src) => decodeURIComponent(escape(src));
    /**
     * polyfill version of `atob`
     */
    const atobPolyfill = (asc) => {
        // console.log('polyfilled');
        asc = asc.replace(/\s+/g, '');
        if (!b64re.test(asc))
            throw new TypeError('malformed base64.');
        asc += '=='.slice(2 - (asc.length & 3));
        let u24, bin = '', r1, r2;
        for (let i = 0; i < asc.length;) {
            u24 = b64tab[asc.charAt(i++)] << 18
                | b64tab[asc.charAt(i++)] << 12
                | (r1 = b64tab[asc.charAt(i++)]) << 6
                | (r2 = b64tab[asc.charAt(i++)]);
            bin += r1 === 64 ? _fromCC(u24 >> 16 & 255)
                : r2 === 64 ? _fromCC(u24 >> 16 & 255, u24 >> 8 & 255)
                    : _fromCC(u24 >> 16 & 255, u24 >> 8 & 255, u24 & 255);
        }
        return bin;
    };
    /**
     * does what `window.atob` of web browsers do.
     * @param {String} asc Base64-encoded string
     * @returns {string} binary string
     */
    const _atob = _hasatob ? (asc) => atob(_tidyB64(asc))
        : _hasBuffer ? (asc) => Buffer.from(asc, 'base64').toString('binary')
            : atobPolyfill;
    const _decode = _hasBuffer
        ? (a) => Buffer.from(a, 'base64').toString('utf8')
        : (a) => btou(_atob(a));
    const _unURI = (a) => _tidyB64(a.replace(/[-_]/g, (m0) => m0 == '-' ? '+' : '/'));
    /**
     * converts a Base64 string to a UTF-8 string.
     * @param {String} src Base64 string.  Both normal and URL-safe are supported
     * @returns {string} UTF-8 string
     */
    const decode = (src) => _decode(_unURI(src));
    /**
     * converts a Base64 string to a Uint8Array.
     */
    const toUint8Array = _hasBuffer
        ? (a) => _U8Afrom(Buffer.from(_unURI(a), 'base64'))
        : (a) => _U8Afrom(_atob(_unURI(a)), c => c.charCodeAt(0));
    const _noEnum = (v) => {
        return {
            value: v, enumerable: false, writable: true, configurable: true
        };
    };
    /**
     * extend String.prototype with relevant methods
     */
    const extendString = function () {
        const _add = (name, body) => Object.defineProperty(String.prototype, name, _noEnum(body));
        _add('fromBase64', function () { return decode(this); });
        _add('toBase64', function (urlsafe) { return encode(this, urlsafe); });
        _add('toBase64URI', function () { return encode(this, true); });
        _add('toBase64URL', function () { return encode(this, true); });
        _add('toUint8Array', function () { return toUint8Array(this); });
    };
    /**
     * extend Uint8Array.prototype with relevant methods
     */
    const extendUint8Array = function () {
        const _add = (name, body) => Object.defineProperty(Uint8Array.prototype, name, _noEnum(body));
        _add('toBase64', function (urlsafe) { return fromUint8Array(this, urlsafe); });
        _add('toBase64URI', function () { return fromUint8Array(this, true); });
        _add('toBase64URL', function () { return fromUint8Array(this, true); });
    };
    /**
     * extend Builtin prototypes with relevant methods
     */
    const extendBuiltins = () => {
        extendString();
        extendUint8Array();
    };
    const gBase64 = {
        version: version,
        VERSION: VERSION,
        atob: _atob,
        atobPolyfill: atobPolyfill,
        btoa: _btoa,
        btoaPolyfill: btoaPolyfill,
        fromBase64: decode,
        toBase64: encode,
        encode: encode,
        encodeURI: encodeURI,
        encodeURL: encodeURI,
        utob: utob,
        btou: btou,
        decode: decode,
        fromUint8Array: fromUint8Array,
        toUint8Array: toUint8Array,
        extendString: extendString,
        extendUint8Array: extendUint8Array,
        extendBuiltins: extendBuiltins,
    };

    //
    // export Base64 to the namespace
    //
    // ES5 is yet to have Object.assign() that may make transpilers unhappy.
    // gBase64.Base64 = Object.assign({}, gBase64);
    gBase64.Base64 = {};
    Object.keys(gBase64).forEach(k => gBase64.Base64[k] = gBase64[k]);
    return gBase64;
}));