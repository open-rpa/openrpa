using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    class XPath
    {
        public static string getXPath(MSHTML.IHTMLElement element)
        {
            if (element == null)
                return "";
            MSHTML.IHTMLElement currentNode = element;
            ArrayList path = new ArrayList();

            while (currentNode != null)
            {
                string pe = getNode(currentNode);
                if (pe != null)
                {
                    path.Add(pe);
                    // if (pe.IndexOf("@id") != -1)
                    if (pe.StartsWith("/"))
                        break;
                }
                currentNode = currentNode.parentElement;
            }
            path.Reverse();
            return join(path, "/");
        }
        private static string join(ArrayList items, string delimiter)
        {
            StringBuilder sb = new StringBuilder();
            foreach (object item in items)
            {
                if (item == null)
                    continue;

                sb.Append(delimiter);
                sb.Append(item);
            }
            return sb.ToString();
        }
        private static string getNode(MSHTML.IHTMLElement node)
        {
            string nodeExpr = node.tagName.ToLower();
            if (nodeExpr == null)  // Eg. node = #text
                return null;
            var ngreflectname = node.getAttribute("ng-reflect-name") as string;
            if (!string.IsNullOrEmpty(ngreflectname) && nodeExpr == "input")
            {
                nodeExpr += "[@ng-reflect-name='" + ngreflectname + "']";
                return "/" + nodeExpr;
            }
            if (node.id != "" && node.id != null)
            {
                nodeExpr += "[@id='" + node.id + "']";
                return "/" + nodeExpr;
            }
            var name = node.getAttribute("name") as string;
            if (!string.IsNullOrEmpty(name) && nodeExpr == "input")
            {
                nodeExpr += "[@name='" + name + "']";
                return "/" + nodeExpr;
            }

            // Find rank of node among its type in the parent
            int rank = 1;
            MSHTML.IHTMLDOMNode nodeDom = node as MSHTML.IHTMLDOMNode;
            MSHTML.IHTMLDOMNode psDom = nodeDom.previousSibling;
            MSHTML.IHTMLElement ps = psDom as MSHTML.IHTMLElement;
            while (ps != null)
            {
                if (ps.tagName == node.tagName)
                {
                    rank++;
                }
                psDom = psDom.previousSibling;
                ps = psDom as MSHTML.IHTMLElement;
            }
            if (rank > 1)
            {
                nodeExpr += "[" + rank + "]";
            }
            else
            { // First node of its kind at this level. Are there any others?
                MSHTML.IHTMLDOMNode nsDom = nodeDom.nextSibling;
                MSHTML.IHTMLElement ns = nsDom as MSHTML.IHTMLElement;
                while (ns != null)
                {
                    if (ns.tagName == node.tagName)
                    { // Yes, mark it as being the first one
                        nodeExpr += "[1]";
                        break;
                    }
                    nsDom = nsDom.nextSibling;
                    ns = nsDom as MSHTML.IHTMLElement;
                }
            }
            return nodeExpr;
        }
        public static string xpath2css(string xpath, bool striptobody)
        {
            if (xpath.Contains("/body") && striptobody)
            {
                xpath = xpath.Substring(xpath.IndexOf("/body") + 1);
            }
            xpath = xpath.Replace("//", "/");
            xpath = xpath.Replace("/", " ");
            var prog = new Regex(@"(?<=\[)(.*?)(?=\])", RegexOptions.IgnoreCase);
            var matched = prog.Matches(xpath);
            for (int i = 0; i < matched.Count; i++)
            {
                xpath = xpath.Replace("[" + matched[i].Value + "]", ":nth-of-type(" + matched[i].Value + ")");
            }
            return xpath;
        }
    }
    class CSSPath
    {
        public static string getCSSPath(HtmlNode element, bool optimized)
        {
            if (element == null)
                return "";
            var currentNode = element;
            ArrayList path = new ArrayList();

            while (currentNode != null)
            {
                string pe = getNode(currentNode, optimized);
                if (pe != null)
                {
                    path.Add(pe);
                    if (pe.Contains("#"))
                        break;
                }
                currentNode = currentNode.ParentNode;
            }
            path.Reverse();
            return join(path, " > ");
        }
        private static string join(ArrayList items, string delimiter)
        {
            StringBuilder sb = new StringBuilder();
            foreach (object item in items)
            {
                if (item == null)
                    continue;

                if(sb.Length > 0) sb.Append(delimiter);
                sb.Append(item);
            }
            return sb.ToString();
        }
        private static string getNode(HtmlNode node, bool optimized)
        {
            string nodeExpr = node.Name.ToLower();
            if (string.IsNullOrEmpty(nodeExpr)) return null;
            if (nodeExpr == "#document") return null;
            if(optimized)
            {
                if (nodeExpr == null)  // Eg. node = #text
                    return null;
                var id = node.Id;
                if (!string.IsNullOrEmpty(id))
                {
                    nodeExpr += "#" + id;
                    return nodeExpr;
                }
            }
            // Find rank of node among its type in the parent
            int rank = 1;
            var ps = node.PreviousSibling;
            while (ps != null)
            {
                if (ps.Name == node.Name)
                {
                    rank++;
                }
                ps = ps.PreviousSibling;
            }
            if (rank > 1)
            {
                nodeExpr += ":nth-of-type(" + rank + ")";
            }
            else
            { 
                //// First node of its kind at this level. Are there any others?
                //MSHTML.IHTMLDOMNode nsDom = nodeDom.nextSibling;
                //MSHTML.IHTMLElement ns = nsDom as MSHTML.IHTMLElement;
                //while (ns != null)
                //{
                //    if (ns.tagName == node.tagName)
                //    { // Yes, mark it as being the first one
                //        nodeExpr += "[1]";
                //        break;
                //    }
                //    nsDom = nsDom.nextSibling;
                //    ns = nsDom as MSHTML.IHTMLElement;
                //}

            }
            return nodeExpr;
        }
    }
}
