using FlaUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class AutomationUtil
    {
        // private static AutomationBase _AutomationBase = null;
        public static AutomationBase getAutomation()
        {
            //if (_AutomationBase == null) _AutomationBase = new FlaUI.UIA3.UIA3Automation();
            //return _AutomationBase;
            return new FlaUI.UIA3.UIA3Automation();
        }

    }
}
