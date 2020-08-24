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
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using ExcelDataReader;

namespace OpenRPA.Utilities
{
    [Designer(typeof(SelectFolderDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.selectfolder.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class SelectFolder : CodeActivity
    {
        public InArgument<bool> ShowNewFolderButton { get; set; }
        [Editor(typeof(SelectSpecialFolder), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> RootFolder { get; set; }
        [RequiredArgument]
        public OutArgument<string> Folder { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var shownewfolderbutton = ShowNewFolderButton.Get(context);
            var rootfolder = RootFolder.Get(context);
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.ShowNewFolderButton = shownewfolderbutton;
            if (string.IsNullOrEmpty(rootfolder)) rootfolder = Environment.SpecialFolder.Desktop.ToString();
            Enum.TryParse(rootfolder, out Environment.SpecialFolder specialfolder);
            folderBrowserDialog.RootFolder = specialfolder;
            System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.Cancel;
            GenericTools.RunUI(() =>
            {
                result = folderBrowserDialog.ShowDialog();
            });
            if (result != System.Windows.Forms.DialogResult.OK)
            {
                context.SetValue(Folder, null);
                return;
            }
            context.SetValue(Folder, folderBrowserDialog.SelectedPath);
        }

    }
    class SelectSpecialFolder : CustomSelectEditor
    {
        public override System.Data.DataTable options
        {
            get
            {
                var lst = new System.Data.DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("MyDocuments", "MyDocuments");
                lst.Rows.Add("Desktop", "Desktop");
                lst.Rows.Add("Favorites", "Favorites");
                lst.Rows.Add("MyComputer", "MyComputer");
                lst.Rows.Add("MyMusic", "MyMusic");
                lst.Rows.Add("MyPictures", "MyPictures");
                lst.Rows.Add("MyVideos", "MyVideos");
                lst.Rows.Add("Personal", "Personal");
                lst.Rows.Add("ProgramFiles", "ProgramFiles");
                lst.Rows.Add("ProgramFilesX86", "ProgramFilesX86");
                lst.Rows.Add("Programs", "Programs");
                lst.Rows.Add("StartMenu", "StartMenu");
                lst.Rows.Add("System", "System");
                lst.Rows.Add("UserProfile", "UserProfile");
                lst.Rows.Add("Windows", "Windows");
                return lst;
            }
        }
    }
}