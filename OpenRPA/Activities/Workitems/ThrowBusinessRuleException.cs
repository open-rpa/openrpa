using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces.Input;
using OpenRPA.Interfaces.entity;

namespace OpenRPA.WorkItems
{
    [System.ComponentModel.Designer(typeof(ThrowBusinessRuleExceptionDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.throwbusinessruleexception.png")]
    [LocalizedToolboxTooltip("activity_throwbusinessruleexception_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_throwbusinessruleexception", typeof(Resources.strings))]
    public class ThrowBusinessRuleException : CodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_throwbusinessruleexception_message", typeof(Resources.strings)), LocalizedDescription("activity_throwbusinessruleexception_message_help", typeof(Resources.strings)), OverloadGroup("By ID")]
        public InArgument<string> Message { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var message = Message.Get(context);
            if (string.IsNullOrEmpty(message)) message = "Unknown Business Rule Exception";
            throw new BusinessRuleException(message);
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
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