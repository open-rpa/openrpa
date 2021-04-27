using ArtisanCode.SimpleAesEncryption;
using OpenRPA.Interfaces;
using System.Activities;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenRPA.Activities
{
    [Designer(typeof(GetPasswordDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.password.png")]
    [LocalizedToolboxTooltip("activity_getpassword_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getpassword", typeof(Resources.strings))]
    public sealed class GetPassword : CodeActivity, INotifyPropertyChanged
    {
        private string _text;

        public event PropertyChangedEventHandler PropertyChanged;

        [RequiredArgument]
        [RefreshProperties(RefreshProperties.Repaint)]
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                _text = _text.Trim();
                if (_text.StartsWith("ENC(") && _text.EndsWith(")"))
                {
                    return;
                }

                var encryptor = new RijndaelMessageEncryptor();
                _text = "ENC(" + encryptor.Encrypt(_text) + ")";
                this.OnPropertyChanged();
            }
        }

        public OutArgument<string> Decrypted { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var decryptor = new RijndaelMessageDecryptor();
            var plainText = decryptor.Decrypt(_text.Substring(4, _text.Length - 5));

            context.SetValue(Decrypted, plainText);
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}
