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
using System.Data;

namespace OpenRPA.Forms.Activities
{
    using ToastNotifications.Messages;
    using ToastNotifications;
    using ToastNotifications.Lifetime;
    using ToastNotifications.Position;

    [System.ComponentModel.Designer(typeof(ShowNotificationDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.prompt.png")]
    [LocalizedToolboxTooltip("activity_shownotification_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_shownotification", typeof(Resources.strings))]
    public class ShowNotification : CodeActivity
    {
        public static bool FirstRun = true;
        public ShowNotification()
        {
            NotificationType = "Information";
            if(FirstRun)
            {
                var dict = new System.Windows.ResourceDictionary();
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(
                    new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/ToastNotifications.Messages;component/Themes/Default.xaml") });
                FirstRun = false;
            }
        }
        [RequiredArgument, Category("Input")]
        public InArgument<string> Message { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Misc")]
        [Editor(typeof(SelectNotificationTypeEditor), typeof(System.Activities.Presentation.PropertyEditing.ExtendedPropertyValueEditor))]
        public InArgument<string> NotificationType { get; set; }
        public static Notifier notifier;
        protected override void Execute(CodeActivityContext context)
        {
            var message = Message.Get(context);
            //var title = Title.Get(context);
            //var duration = Duration.Get(context);
            var notificationType = NotificationType.Get(context);
            if(notifier==null)
            {
                notifier = new Notifier(cfg =>
                {
                    //cfg.PositionProvider = new WindowPositionProvider(
                    //    parentWindow: GenericTools.mainWindow,
                    //    corner: Corner.TopRight,
                    //    offsetX: 10,
                    //    offsetY: 10);
                    cfg.PositionProvider = new PrimaryScreenPositionProvider(
           corner: Corner.BottomRight,
           offsetX: 10,
           offsetY: 10);
                    cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                        notificationLifetime: TimeSpan.FromSeconds(3),
                        maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                    cfg.Dispatcher = System.Windows.Application.Current.Dispatcher;
                });
            }
            if (notificationType == "Information") notifier.ShowInformation(message);
            if (notificationType == "Success") notifier.ShowSuccess(message);
            if (notificationType == "Warning") notifier.ShowWarning(message);
            if (notificationType == "Error") notifier.ShowError(message);

            //Notifications.Wpf.NotificationType nt = Notifications.Wpf.NotificationType.Information;
            //nt = (Notifications.Wpf.NotificationType)Enum.Parse(typeof(Notifications.Wpf.NotificationType), notificationType);
            //GenericTools.notificationManager.Show(new NotificationContent
            //{
            //    Title = title,
            //    Message = message,
            //    Type = nt
            //}, expirationTime: duration);
        }
        class SelectNotificationTypeEditor : CustomSelectEditor
        {
            public override DataTable options
            {
                get
                {
                    DataTable lst = new DataTable();
                    lst.Columns.Add("ID", typeof(string));
                    lst.Columns.Add("TEXT", typeof(string));
                    lst.Rows.Add("Information", "Information");
                    lst.Rows.Add("Success", "Success");
                    lst.Rows.Add("Warning", "Warning");
                    lst.Rows.Add("Error", "Error");
                    return lst;
                }
            }
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