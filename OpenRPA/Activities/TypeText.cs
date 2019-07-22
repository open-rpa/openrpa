using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces.Input;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(TypeTextDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.typetext.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class TypeText : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Text { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var disposes = new List<IDisposable>();
            var enddisposes = new List<IDisposable>();
            var text = Text.Get<string>(context);
            if (string.IsNullOrEmpty(text)) return;

            //var clickdelay = ClickDelay.Get(context);
            //var linedelay = LineDelay.Get(context);
            //var predelay = PreDelay.Get(context);
            //var postdelay = PostDelay.Get(context);
            var clickdelay = TimeSpan.FromMilliseconds(5);
            var linedelay = TimeSpan.FromMilliseconds(5);
            var predelay = TimeSpan.FromMilliseconds(0);
            var postdelay = TimeSpan.FromMilliseconds(100);
            System.Threading.Thread.Sleep(predelay);

            // string[] lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

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
                        Enum.TryParse<FlaUI.Core.WindowsAPI.VirtualKeyShort>(key, true, out vk);
                        if (down)
                        {
                            if (vk > 0)
                            {
                                enddisposes.Add(FlaUI.Core.Input.Keyboard.Pressing(vk));
                            }
                            else
                            {
                                FlaUI.Core.Input.Keyboard.Type(key);
                            }
                        }
                        else if (up)
                        {
                            if (vk > 0)
                            {
                                FlaUI.Core.Input.Keyboard.Release(vk);
                            }
                            else
                            {
                                FlaUI.Core.Input.Keyboard.Type(key);
                            }
                        }
                        else
                        {
                            if (vk > 0)
                            {
                                disposes.Add(FlaUI.Core.Input.Keyboard.Pressing(vk));
                            }
                            else
                            {
                                FlaUI.Core.Input.Keyboard.Type(key);
                            }
                        }
                        System.Threading.Thread.Sleep(clickdelay);
                    }
                    disposes.ForEach(x => { x.Dispose(); });
                }
                else
                {
                    FlaUI.Core.Input.Keyboard.Type(c);
                    System.Threading.Thread.Sleep(clickdelay);
                }
            }
            enddisposes.ForEach(x => { x.Dispose(); });
            System.Threading.Thread.Sleep(postdelay);

        }

        internal List<vKey> _keys = new List<vKey>();
        internal string result;


        private List<vKey> _downkeys = new List<vKey>();
        public int keysdown
        {
            get
            {
                return _downkeys.Count;
            }
        }
        
        public void AddKey(vKey _key, System.Activities.Presentation.Model.ModelItem lastinsertedmodel)
        {
            if(_keys == null) _keys = new List<vKey>();
            if(!_key.up)
            {
                var isdown = _downkeys.Where(x => x.KeyCode == _key.KeyCode).FirstOrDefault();
                if (isdown != null) return;
                _downkeys.Add(_key);
            } else
            {
                var isdown = _downkeys.Where(x => x.KeyCode == _key.KeyCode).FirstOrDefault();
                if (isdown == null) return;
                _downkeys.Remove(isdown);

            }

            _keys.Add(_key);
            result = "";
            for (var i = 0; i < _keys.Count; i++)
            {
                string val = "";
                var key = _keys[i];
                if (key.up == false && (i + 1) < _keys.Count)
                {
                    if (key.KeyCode == _keys[i + 1].KeyCode && _keys[i + 1].up)
                    {
                        i++;
                        val = "{" + key.KeyCode.ToString() + "}";
                        if (key.KeyCode.ToString().StartsWith("KEY_"))
                        {
                            val = key.KeyCode.ToString().Substring(4).ToLower();
                        }
                        if (key.KeyCode == FlaUI.Core.WindowsAPI.VirtualKeyShort.SPACE)
                        {
                            val = " ";
                        }
                    }
                }
                if (string.IsNullOrEmpty(val))
                {
                    if (key.up == false)
                    {
                        val = "{" + key.KeyCode.ToString() + " down}";
                    }
                    else
                    {
                        val = "{" + key.KeyCode.ToString() + " up}";
                    }

                }
                result += val;
            }
            //Text = result;
            if (result == null) result = "";
            if(lastinsertedmodel!=null) lastinsertedmodel.Properties["Text"].SetValue(new InArgument<string>(result));
        }
    }
}