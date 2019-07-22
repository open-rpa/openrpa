using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.Input
{
    public class vKey
    {
        public vKey() { }
        public vKey(int KeyValue, bool up)
        {
            this.KeyValue = KeyValue;
            this.up = up;
        }
        public vKey(FlaUI.Core.WindowsAPI.VirtualKeyShort k, bool up)
        {
            KeyCode = k;
            KeyValue = (int)k;
            this.up = up;
        }
        public FlaUI.Core.WindowsAPI.VirtualKeyShort KeyCode { get; set; }
        public int KeyValue { get; set; }
        public bool up { get; set; }

        public static List<vKey> parseText(string text)
        {
            List<vKey> keys = new List<vKey>();

            for (var i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{')
                {
                    int indexEnd = text.IndexOf('}', i + 1);
                    int indexNextStart = text.IndexOf('{', indexEnd + 1);
                    int indexNextEnd = text.IndexOf('}', indexEnd + 1);
                    if (indexNextStart > indexNextEnd || (indexNextStart == -1 && indexNextEnd > -1)) indexEnd = indexNextEnd;
                    var sub = text.Substring(i + 1, (indexEnd - i) - 1);
                    i = indexEnd;
                    foreach (var k in sub.Split(','))
                    {
                        string key = k.Trim();
                        bool down = false;
                        bool up = false;
                        if (key.EndsWith("down"))
                        {
                            down = true;
                            key = key.Replace(" down", "");
                        }
                        else if (key.EndsWith("up"))
                        {
                            up = true;
                            key = key.Replace(" up", "");
                        }
                        //Keys specialkey;
                        FlaUI.Core.WindowsAPI.VirtualKeyShort vk;
                        Enum.TryParse(key, true, out vk);
                        if (vk == 0)
                        {
                            Enum.TryParse("KEY_" + key, true, out vk);
                        }
                        if (down)
                        {
                            keys.Add(new vKey(vk, false));
                        }
                        else if (up)
                        {
                            keys.Add(new vKey(vk, true));
                        }
                        else
                        {
                            keys.Add(new vKey(vk, false));
                            keys.Add(new vKey(vk, true));
                        }
                    }
                }
                else
                {
                    FlaUI.Core.WindowsAPI.VirtualKeyShort vk;
                    Enum.TryParse("KEY_" + c.ToString(), true, out vk);
                    if (vk == 0)
                    {
                        Enum.TryParse(c.ToString(), true, out vk);
                    }
                    keys.Add(new vKey(vk, false));
                    keys.Add(new vKey(vk, true));

                }
            }

            return keys;
        }
    }
}
