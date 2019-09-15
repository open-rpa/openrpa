using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.IE
{
    public static class IEExtensions
    {
        public static System.Windows.Media.Imaging.BitmapFrame GetImageSourceFromResource(string resourceName)
        {
            string[] names = typeof(IEExtensions).Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    return System.Windows.Media.Imaging.BitmapFrame.Create(typeof(IEExtensions).Assembly.GetManifestResourceStream(name));
                }
            }
            return null;
        }
        public static MSHTML.IHTMLElement GetXPath(this SHDocVw.WebBrowser wb, string xpath)
        {
            CallScript(wb);


            //bool completed = false;
            ////wb.ScriptErrorsSuppressed = true;
            ////wb.DocumentCompleted += delegate { completed = true; };
            ////wb.Navigate("http://stackoverflow.com/");

            ////while (!completed)
            ////{
            ////    Application.DoEvents();
            ////    Thread.Sleep(100);
            ////}

            //if (wb.Document != null)
            //{
            //    MSHTML.IHTMLElement head = wb.Document.GetElementsByTagName("head")[0];
            //    HtmlElement scriptEl = wb.Document.CreateElement("script");
            //    MSHTML.IHTMLScriptElement element = (MSHTML.IHTMLScriptElement)scriptEl.DomElement;
            //    element.text = System.IO.File.ReadAllText(@"wgxpath.install.js");
            //    head.AppendChild(scriptEl);

            //    // Call wgxpath.install() from JavaScript code, which will ensure document.evaluate
            //    wb.Document.InvokeScript("eval", new object[] { "wgxpath.install()" });

            //    string xPathQuery = @"count(//script)";
            //    string code = string.Format("document.evaluate('{0}', document, null, XPathResult.NUMBER_TYPE, null).numberValue;", xPathQuery);
            //    int iResult = (int)wb.Document.InvokeScript("eval", new object[] { code });
            //}
            return null;
        }

        // https://stackoverflow.com/questions/15273311/how-to-invoke-scripts-work-in-mshtml
        public static void CallScript(SHDocVw.WebBrowser axWebBrowser)
        {
            //
            // Using C# dynamics, which maps to COM's IDispatch::GetIDsOfNames, 
            // IDispatch::Invoke
            //


            //HtmlElement scriptJS = axWebBrowser.Document.CreateElement("script");
            //MSHTML.IHTMLScriptElement elementJS = (MSHTML.IHTMLScriptElement)scriptJS.DomElement;
            //elementJS.type = "text/javascript";
            //StreamReader streamReader = new StreamReader("TheFile.js");
            //elementJS.text = streamReader.ReadToEnd();
            //streamReader.Close();
            //webBrowser1.Document.Body.AppendChild(scriptJS);


            //MSHTML.HTMLDocumentClass htmlDocument = axWebBrowser.Document;

            //var htmlWindow = (MSHTML.IHTMLWindow2) htmlDocument.parentWindow;
            //MSHTML.HTMLDocument hd = htmlDocument;

            ////htmlDocument.execsc

            ////// make sure the web page has at least one <script> tag for eval to work
            ////htmlDocument.body.appendChild(htmlDocument.createElement("script"));

            ////// can call any DOM window method
            ////htmlWindow.alert("hello from web page!");

            ////// call a global JavaScript function, e.g.:
            ////// <script>function TestFunc(arg) { alert(arg); }</script>
            ////htmlWindow.TestFunc("Hello again!");

            ////// call any JavaScript via "eval"
            //var result = (bool)htmlWindow.eval("(function() { return confirm('Continue?'); })()");
            //MessageBox.Show(result.ToString());

            //
            // Using .NET reflection:
            //

            object htmlWindowObject = GetProperty(axWebBrowser.Document, "parentWindow");

            // call a global JavaScript function
            InvokeScript(htmlWindowObject, "TestFunc", "Hello again!");

            //// call any JavaScript via "eval"
            //result = (bool)InvokeScript(htmlWindowObject, "eval", "(function() { return confirm('Continue?'); })()");
            //MessageBox.Show(result.ToString());
        }

        static object GetProperty(object callee, string property)
        {
            return callee.GetType().InvokeMember(property,
                BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public,
                null, callee, new Object[] { });
        }

        static object InvokeScript(object callee, string method, params object[] args)
        {
            return callee.GetType().InvokeMember(method,
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
                null, callee, args);
        }

        // https://stackoverflow.com/questions/28009093/how-to-using-xpath-in-webbrowser-control

        // https://github.com/mradosta/thousandpass/blob/master/addons/msie/1000Pass_com/1000pass_com/XPath.cs
        public static string getXPath(this MSHTML.IHTMLElement element)
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
                    //if (pe.IndexOf("@id") != -1)
                    //    break;  // Found an ID, no need to go upper, absolute path is OK
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
            string nodeExpr = node.tagName;
            if (nodeExpr == null)  // Eg. node = #text
                return null;
            if (node.id != "" && node.id != null)
            {
                nodeExpr += "[@id='" + node.id + "']";
                // We don't really need to go back up to //HTML, since IDs are supposed
                // to be unique, so they are a good starting point.
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




        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }
            if (obj is System.Activities.Expressions.Literal<T>)
            {
                result = (T)((System.Activities.Expressions.Literal<T>)obj).Value;
                return true;
            }

            result = default(T);
            return false;
        }
        public static T TryCast<T>(this object obj)
        {
            T result = default(T);
            if (TryCast<T>(obj, out result))
                return result;
            return result;
        }
        public static T GetValue<T>(this System.Activities.Presentation.Model.ModelItem model, string name)
        {
            T result = default(T);
            if (model.Properties[name] != null)
            {
                if (model.Properties[name].Value == null) return result;
                if (model.Properties[name].Value.Properties["Expression"] != null)
                {
                    result = model.Properties[name].Value.Properties["Expression"].ComputedValue.TryCast<T>();
                    return result;
                }
                result = model.Properties[name].ComputedValue.TryCast<T>();
                return result;
            }
            return result;
        }

    }
}
