using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    public class IEElement : IElement
    {
        public IEElement(mshtml.IHTMLElement Element)
        {
            rawElement = Element;
            className = Element.className;
            id = Element.id;
            tagName = Element.tagName.ToLower();
            if (tagName == "input")
            {
                mshtml.IHTMLInputElement inputelement = Element as mshtml.IHTMLInputElement;
                type = inputelement.type.ToLower();
            }
            try
            {
                uniqueID = ((dynamic)rawElement).uniqueID;
            }
            catch (Exception)
            {
            }
        }
        public string className { get; set; }
        public string uniqueID { get; set; }
        public string id { get; set; }
        public string tagName { get; set; }
        public string type { get; set; }
        public mshtml.IHTMLElement rawElement { get; private set; }

        //public HTMLElement rawElement { get; private set; }
        public void Click()
        {
            if (rawElement.tagName.ToLower() == "input")
            {
                var ele = (mshtml.IHTMLInputElement)rawElement;
                var _ele2 = ele as mshtml.IHTMLElement;
                _ele2.click();
            }
        }
        public void Focus()
        {
        }

        public string Value
        {
            get
            {
                if(rawElement.tagName.ToLower() == "input")
                {
                    var ele = (mshtml.IHTMLInputElement)rawElement;
                    return ele.value;
                }
                return null;
            }
            set
            {
                if (rawElement.tagName.ToLower() == "input")
                {
                    var ele = (mshtml.IHTMLInputElement)rawElement;
                    ele.value = value;
                }
                
            }
        }
        public override string ToString()
        {
            return tagName + " " + (!string.IsNullOrEmpty(id)? id: className);
        }
        public override bool Equals(object obj)
        {
            var e = obj as IEElement;
            if (e == null) return false;
            if (e.uniqueID == uniqueID) return true;
            if (rawElement.sourceIndex == e.rawElement.sourceIndex) return true;    
            if (rawElement.GetHashCode() == e.rawElement.GetHashCode()) return true;
            return false;
            //return base.Equals(obj);
        }
    }
}
