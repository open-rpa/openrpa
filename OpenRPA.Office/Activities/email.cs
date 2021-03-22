using Microsoft.Office.Interop.Outlook;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Office.Activities
{
    //using Microsoft.Office.Interop.Outlook;
    public class email
    {
        public email() { }
        public email(Microsoft.Office.Interop.Outlook.MailItem mailItem)
        {
            this.mailItem = mailItem;
        }
        [System.Runtime.Serialization.IgnoreDataMember, Newtonsoft.Json.JsonIgnore]
        public Microsoft.Office.Interop.Outlook.MailItem mailItem { get; private set; }
        //public Application Application { get { return mailItem.Application; } }

        //public OlObjectClass Class { get { return mailItem.Class; } }

        //public NameSpace Session { get { return mailItem.Session; } }
        //public dynamic Parent { get { return mailItem.Parent; } }
        //public Actions Actions { get { return mailItem.Actions; } }
        public Attachment[] Attachments { 
            get {
                var result = new List<Attachment>();
                foreach (Microsoft.Office.Interop.Outlook.Attachment a in mailItem.Attachments) result.Add(new Attachment(a));
                return result.ToArray(); 
            } 
        }
        public bool SaveAttachments(string Path, bool Overwrite = false)
        {
            foreach(var a in Attachments)
            {
                if (!a.SaveTo(Path, Overwrite)) return false;
            }
            return true;
        }
        private Microsoft.Office.Interop.Outlook.Application CreateOutlookInstance()
        {
            var outlookApplication = new Microsoft.Office.Interop.Outlook.Application();
            if (outlookApplication.ActiveExplorer() == null)
            {
                // mOutlookExplorer = mOutlookApplication.Session.GetDefaultFolder(OlDefaultFolders.olFolderCalendar).GetExplorer();
                var mOutlookExplorer = outlookApplication.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox).GetExplorer();
                mOutlookExplorer.Activate();
            }
            return outlookApplication;
        }
        public MAPIFolder GetFolder(MAPIFolder folder, string FullFolderPath)
        {
            if (folder.Folders.Count == 0)
            {
                if (folder.FullFolderPath == FullFolderPath)
                {
                    return folder;
                }
            }
            else
            {
                foreach (MAPIFolder subFolder in folder.Folders)
                {
                    if (folder.FullFolderPath == FullFolderPath)
                    {
                        return folder;
                    }
                    var temp = GetFolder(subFolder, FullFolderPath);
                    if (temp != null) return temp;
                }
            }
            return null;
        }
        public bool Move(string targetfolder)
        {
            var outlookApplication = CreateOutlookInstance();
            if (outlookApplication.ActiveExplorer() == null)
            {
                Log.Warning("Outlook not running!");
                return false;
            }
            var oNS = outlookApplication.GetNamespace("MAPI");
            foreach (MAPIFolder folder in oNS.Folders)
            {
                MAPIFolder mfolder = GetFolder(folder, targetfolder);
                if(mfolder != null)
                {
                    mailItem.Move(mfolder);
                    return true;
                }
            }
            Log.Error("Fail locating " + targetfolder);
            return false;
        }
        public string BillingInformation { get { return mailItem.BillingInformation; } set { mailItem.BillingInformation = value; } }
        public string Body { get { return mailItem.Body; } set { mailItem.Body = value; } }
        public string Categories { get { return mailItem.Categories; } set { mailItem.Categories = value; } }
        public string Companies { get { return mailItem.Companies; } set { mailItem.Companies = value; } }
        public string ConversationIndex { get { return mailItem.ConversationIndex; } }
        public string ConversationTopic { get { return mailItem.ConversationTopic; } }
        public DateTime CreationTime { get { return mailItem.CreationTime; } }
        public string EntryID { get { return mailItem.EntryID; } }
        //public FormDescription FormDescription { get { return mailItem.FormDescription; } }
        //public Inspector GetInspector { get { return mailItem.GetInspector; } }
        //public OlImportance Importance { get { return mailItem.Importance; } set { mailItem.Importance = value; } }
        public DateTime LastModificationTime { get { return mailItem.LastModificationTime; } }
        //public dynamic MAPIOBJECT { get { return mailItem.MAPIOBJECT; } }
        public string MessageClass { get { return mailItem.MessageClass; } set { mailItem.MessageClass = value; } }
        public string Mileage { get { return mailItem.Mileage; } set { mailItem.Mileage = value; } }
        public bool NoAging { get { return mailItem.NoAging; } set { mailItem.NoAging = value; } }
        public int OutlookInternalVersion { get { return mailItem.OutlookInternalVersion; } }
        public string OutlookVersion { get { return mailItem.OutlookVersion; } }
        public bool Saved { get { return mailItem.Saved; } }
        //public OlSensitivity Sensitivity { get { return mailItem.Sensitivity; } set { mailItem.Sensitivity = value; } }
        public int Size { get { return mailItem.Size; } }
        public string Subject { get { return mailItem.Subject; } set { mailItem.Subject = value; } }
        public bool UnRead { get { return mailItem.UnRead; } set { mailItem.UnRead = value; } }
        //public UserProperties UserProperties { get { return mailItem.UserProperties; } }
        public bool AlternateRecipientAllowed { get { return mailItem.AlternateRecipientAllowed; } set { mailItem.AlternateRecipientAllowed = value; } }
        public bool AutoForwarded { get { return mailItem.AutoForwarded; } set { mailItem.AutoForwarded = value; } }
        public string BCC { get { return mailItem.BCC; } set { mailItem.BCC = value; } }
        public string CC { get { return mailItem.CC; } set { mailItem.CC = value; } }
        public DateTime DeferredDeliveryTime { get { return mailItem.DeferredDeliveryTime; } set { mailItem.DeferredDeliveryTime = value; } }
        public bool DeleteAfterSubmit { get { return mailItem.DeleteAfterSubmit; } set { mailItem.DeleteAfterSubmit = value; } }
        public DateTime ExpiryTime { get { return mailItem.ExpiryTime; } set { mailItem.ExpiryTime = value; } }
        public DateTime FlagDueBy { get { return mailItem.FlagDueBy; } set { mailItem.FlagDueBy = value; } }
        public string FlagRequest { get { return mailItem.FlagRequest; } set { mailItem.FlagRequest = value; } }
        //public OlFlagStatus FlagStatus { get { return mailItem.FlagStatus; } set { mailItem.FlagStatus = value; } }
        public string HTMLBody { get { return mailItem.HTMLBody; } set { mailItem.HTMLBody = value; } }
        public bool OriginatorDeliveryReportRequested { get { return mailItem.OriginatorDeliveryReportRequested; } set { mailItem.OriginatorDeliveryReportRequested = value; } }
        public bool ReadReceiptRequested { get { return mailItem.ReadReceiptRequested; } set { mailItem.ReadReceiptRequested = value; } }
        public string ReceivedByEntryID { get { return mailItem.ReceivedByEntryID; } }
        public string ReceivedByName { get { return mailItem.ReceivedByName; } }
        public string ReceivedOnBehalfOfEntryID { get { return mailItem.ReceivedOnBehalfOfEntryID; } }
        public string ReceivedOnBehalfOfName { get { return mailItem.ReceivedOnBehalfOfName; } }
        public DateTime ReceivedTime { get { return mailItem.ReceivedTime; } }
        public bool RecipientReassignmentProhibited { get { return mailItem.RecipientReassignmentProhibited; } set { mailItem.RecipientReassignmentProhibited = value; } }
        //public Recipients Recipients { get { return mailItem.Recipients; } }
        public bool ReminderOverrideDefault { get { return mailItem.ReminderOverrideDefault; } set { mailItem.ReminderOverrideDefault = value; } }
        public bool ReminderPlaySound { get { return mailItem.ReminderPlaySound; } set { mailItem.ReminderPlaySound = value; } }
        public bool ReminderSet { get { return mailItem.ReminderSet; } set { mailItem.ReminderSet = value; } }
        public string ReminderSoundFile { get { return mailItem.ReminderSoundFile; } set { mailItem.ReminderSoundFile = value; } }
        public DateTime ReminderTime { get { return mailItem.ReminderTime; } set { mailItem.ReminderTime = value; } }
        //public OlRemoteStatus RemoteStatus { get { return mailItem.RemoteStatus; } set { mailItem.RemoteStatus = value; } }
        public string ReplyRecipientNames { get { return mailItem.ReplyRecipientNames; } }
        //public Recipients ReplyRecipients { get { return mailItem.ReplyRecipients; } }
        //public MAPIFolder SaveSentMessageFolder { get { return mailItem.SaveSentMessageFolder; } set { mailItem.SaveSentMessageFolder = value; } }
        public string SenderName { get { return mailItem.SenderName; } }
        public bool Sent { get { return mailItem.Sent; } }
        public DateTime SentOn { get { return mailItem.SentOn; } }
        public string SentOnBehalfOfName { get { return mailItem.SentOnBehalfOfName; } set { mailItem.SentOnBehalfOfName = value; } }
        public bool Submitted { get { return mailItem.Submitted; } }
        public string To { get { return mailItem.To; } set { mailItem.To = value; } }
        public string VotingOptions { get { return mailItem.VotingOptions; } set { mailItem.VotingOptions = value; } }
        public string VotingResponse { get { return mailItem.VotingResponse; } set { mailItem.VotingResponse = value; } }
        //public Links Links { get { return mailItem.Links; } }
        //public ItemProperties ItemProperties { get { return mailItem.ItemProperties; } }
        //public OlBodyFormat BodyFormat { get { return mailItem.BodyFormat; } set { mailItem.BodyFormat = value; } }
        //public OlDownloadState DownloadState { get { return mailItem.DownloadState; } }
        public int InternetCodepage { get { return mailItem.InternetCodepage; } set { mailItem.InternetCodepage = value; } }
        //public OlRemoteStatus MarkForDownload { get { return mailItem.MarkForDownload; } set { mailItem.MarkForDownload = value; } }
        public bool IsConflict { get { return mailItem.IsConflict; } }
        public bool IsIPFax { get { return mailItem.IsIPFax; } set { mailItem.IsIPFax = value; } }
        //public OlFlagIcon FlagIcon { get { return mailItem.FlagIcon; } set { mailItem.FlagIcon = value; } }
        public bool HasCoverSheet { get { return mailItem.HasCoverSheet; } set { mailItem.HasCoverSheet = value; } }
        public bool AutoResolvedWinner { get { return mailItem.AutoResolvedWinner; } }
        //public Conflicts Conflicts { get { return mailItem.Conflicts; } }
        public string SenderEmailAddress { get { return mailItem.SenderEmailAddress; } }
        public string SenderEmailType { get { return mailItem.SenderEmailType; } }
        public bool EnableSharedAttachments { get { return mailItem.EnableSharedAttachments; } set { mailItem.EnableSharedAttachments = value; } }
        //public OlPermission Permission { get { return mailItem.Permission; } set { mailItem.Permission = value; } }
        //public OlPermissionService PermissionService { get { return mailItem.PermissionService; } set { mailItem.PermissionService = value; } }
        //public PropertyAccessor PropertyAccessor { get { return mailItem.PropertyAccessor; } }
        //public Account SendUsingAccount { get { return mailItem.SendUsingAccount; } set { mailItem.SendUsingAccount = value; } }
        public string TaskSubject { get { return mailItem.TaskSubject; } set { mailItem.TaskSubject = value; } }
        public DateTime TaskDueDate { get { return mailItem.TaskDueDate; } set { mailItem.TaskDueDate = value; } }
        public DateTime TaskStartDate { get { return mailItem.TaskStartDate; } set { mailItem.TaskStartDate = value; } }
        public DateTime TaskCompletedDate { get { return mailItem.TaskCompletedDate; } set { mailItem.TaskCompletedDate = value; } }
        public DateTime ToDoTaskOrdinal { get { return mailItem.ToDoTaskOrdinal; } set { mailItem.ToDoTaskOrdinal = value; } }
        public bool IsMarkedAsTask { get { return mailItem.IsMarkedAsTask; } }
        //public string ConversationID { get { return mailItem.ConversationID; } }
        //public AddressEntry Sender { get { return mailItem.Sender; } set { mailItem.Sender = value; } }
        //public string PermissionTemplateGuid { get { return mailItem.PermissionTemplateGuid; } set { mailItem.PermissionTemplateGuid = value; } }
        //public dynamic RTFBody { get { return mailItem.RTFBody; } set { mailItem.RTFBody = value; } }
        //public string RetentionPolicyName { get { return mailItem.RetentionPolicyName; } }
        //public DateTime RetentionExpirationDate { get { return mailItem.RetentionExpirationDate; } }
    } 
}
