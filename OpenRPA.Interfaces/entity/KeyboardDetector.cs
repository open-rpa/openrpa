using Newtonsoft.Json;
using OpenRPA.Interfaces.entity;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;


namespace OpenRPA.Interfaces.entity
{
    public class KeyboardDetector : Detector
    {
        public KeyboardDetector()
        {
            _type = "detector";
        }
        public string Keys { get { return GetProperty<string>(); } set { SetProperty(value); } }

    }
}
