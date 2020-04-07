using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM.Activities
{
    public class NMEqualityComparer : IEqualityComparer<NMElement>
    {
        public bool Equals(NMElement element1, NMElement element2)
        {
            if (element2 == null && element1 == null)
                return true;
            else if (element1 == null || element2 == null)
                return false;
            if (!string.IsNullOrEmpty(element1.xpath) && !string.IsNullOrEmpty(element2.xpath))
            {
                if (element1.xpath != element2.xpath) return false;
            }
            if (!string.IsNullOrEmpty(element1.Text) && !string.IsNullOrEmpty(element2.Text))
            {
                if (element1.Text != element2.Text) return false;
            }
            if (!string.IsNullOrEmpty(element1.Value) && !string.IsNullOrEmpty(element2.Value))
            {
                if (element1.Value != element2.Value) return false;
            }
            return (GetHashCode(element1) == GetHashCode(element2));
        }
        public int GetHashCode(NMElement element)
        {
            return element.GetHashCode();
        }
    }
}
