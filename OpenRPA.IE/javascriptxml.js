// xpath.js
// ------------------------------------------------------------------
//
// a cross-browser xpath class.
// Derived form code at http://jmvidal.cse.sc.edu/talks/javascriptxml/xpathexample.html.
//
// Tested in Chrome, IE9, and FF6.0.2
//
// Author     : Dino
// Created    : Sun Sep 18 18:39:58 2011
// Last-saved : <2011-September-19 15:07:20>
//
// ------------------------------------------------------------------

/*jshint browser:true */

(function (globalScope) {
    'use strict';

    /**
     * The first argument to this constructor is the text of the XPath expression.
     *
     * If the expression uses any XML namespaces, the second argument must
     * be a JavaScript object that maps namespace prefixes to the URLs that define
     * those namespaces.  The properties of this object are taken as prefixes, and
     * the values associated to those properties are the URLs.
     *
     * There's no way to specify a non-null default XML namespace. You need to use
     * prefixes in order to reference a non-null namespace in a query.
     *
     */

    var expr = function (xpathText, namespaces) {
        var prefix;
        this.xpathText = xpathText;    // Save the text of the expression
        this.namespaces = namespaces || null;  // And the namespace mapping

        if (document.createExpression) {
            this.xpathExpr = true;
            // I tried using a compiled xpath expression, it worked on Chrome,
            // but it did not work on FF6.0.2.  Threw various exceptions.
            // So I punt on "compiling" the xpath and just evaluate it.
            //
            // This flag serves only to store the result of the check.
            //

            // document.createExpression(xpathText,
            // // This function is passed a
            // // namespace prefix and returns the URL.
            // function(prefix) {
            //     return namespaces[prefix];
            // });
        }
        else {
            // assume IE and convert the namespaces object into the
            // textual form that IE requires.
            this.namespaceString = "";
            if (namespaces !== null) {
                for (prefix in namespaces) {
                    // Add a space if there is already something there
                    if (this.namespaceString.length > 1) this.namespaceString += ' ';
                    // And add the namespace
                    this.namespaceString += 'xmlns:' + prefix + '="' +
                        namespaces[prefix] + '"';
                }
            }
        }
    };

    /**
     * This is the getNodes() method of XPath.Expression.  It evaluates the
     * XPath expression in the specified context.  The context argument should
     * be a Document or Element object.  The return value is an array
     * or array-like object containing the nodes that match the expression.
     */
    expr.prototype.getNodes = function (xmlDomCtx) {
        var self = this, a, i,
            doc = xmlDomCtx.ownerDocument;

        // If the context doesn't have ownerDocument, it is the Document
        if (doc === null) doc = xmlDomCtx;

        if (this.xpathExpr) {
            // could not get a compiled XPathExpression to work in FF6
            // var result = this.xpathExpr.evaluate(xmlDomCtx,
            //     // This is the result type we want
            //     XPathResult.ORDERED_NODE_SNAPSHOT_TYPE,
            //     null);

            var result = doc.evaluate(this.xpathText,
                xmlDomCtx,
                function (prefix) {
                    return self.namespaces[prefix];
                },
                XPathResult.ORDERED_NODE_SNAPSHOT_TYPE,
                null);

            // Copy the results into an array.
            a = [];
            for (i = 0; i < result.snapshotLength; i++) {
                a.push(result.snapshotItem(i));
            }
            return a;
        }
        else {
            // evaluate the expression using the IE API.
            try {
                // This is IE-specific magic to specify prefix-to-URL mapping
                doc.setProperty("SelectionLanguage", "XPath");
                doc.setProperty("SelectionNamespaces", this.namespaceString);

                // In IE, the context must be an Element not a Document,
                // so if context is a document, use documentElement instead
                if (xmlDomCtx === doc) xmlDomCtx = doc.documentElement;
                // Now use the IE method selectNodes() to evaluate the expression
                return xmlDomCtx.selectNodes(this.xpathText);
            }
            catch (e2) {
                throw "XPath is not supported by this browser.";
            }
        }
    };


    /**
     * This is the getNode() method of XPath.Expression.  It evaluates the
     * XPath expression in the specified context and returns a single matching
     * node (or null if no node matches).  If more than one node matches,
     * this method returns the first one in the document.
     * The implementation differs from getNodes() only in the return type.
     */
    expr.prototype.getNode = function (xmlDomCtx) {
        var self = this,
            doc = xmlDomCtx.ownerDocument;
        if (doc === null) doc = xmlDomCtx;
        if (this.xpathExpr) {

            // could not get compiled "XPathExpression" to work in FF4
            // var result =
            //     this.xpathExpr.evaluate(xmlDomCtx,
            //     // We just want the first match
            //     XPathResult.FIRST_ORDERED_NODE_TYPE,
            //     null);

            var result = doc.evaluate(this.xpathText,
                xmlDomCtx,
                function (prefix) {
                    return self.namespaces[prefix];
                },
                XPathResult.FIRST_ORDERED_NODE_TYPE,
                null);
            return result.singleNodeValue;
        }
        else {
            try {
                doc.setProperty("SelectionLanguage", "XPath");
                doc.setProperty("SelectionNamespaces", this.namespaceString);
                if (xmlDomCtx == doc) xmlDomCtx = doc.documentElement;
                return xmlDomCtx.selectSingleNode(this.xpathText);
            }
            catch (e) {
                throw "XPath is not supported by this browser.";
            }
        }
    };


    var getNodes = function (context, xpathExpr, namespaces) {
        return (new globalScope.XPath.Expression(xpathExpr, namespaces)).getNodes(context);
    };

    var getNode = function (context, xpathExpr, namespaces) {
        return (new globalScope.XPath.Expression(xpathExpr, namespaces)).getNode(context);
    };


    /**
     * XPath is a global object, containing three members.  The
     * Expression member is a class modelling an Xpath expression.  Use
     * it like this:
     *
     *   var xpath1 = new XPath.Expression("/kml/Document/Folder");
     *   var nodeList = xpath1.getNodes(xmldoc);
     *
     *   var xpath2 = new XPath.Expression("/a:kml/a:Document",
     *                                   { a : 'http://www.opengis.net/kml/2.2' });
     *   var node = xpath2.getNode(xmldoc);
     *
     * The getNodes() and getNode() methods are just utility methods for
     * one-time use. Example:
     *
     *   var oneNode = XPath.getNode(xmldoc, '/root/favorites');
     *
     *   var nodeList = XPath.getNodes(xmldoc, '/x:derp/x:twap', { x: 'urn:0190djksj-xx'} );
     *
     */

    // place XPath into the global scope.
    globalScope.XPath = {
        Expression: expr,
        getNodes: getNodes,
        getNode: getNode
    };

}(this));