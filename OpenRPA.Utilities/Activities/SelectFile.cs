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
    [Designer(typeof(SelectFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.selectfile.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class SelectFile : CodeActivity
    {
        [Category("Input")]
        public InArgument<bool> IsSaveAs { get; set; }
        [Editor(typeof(SelectSpecialFolder), typeof(ExtendedPropertyValueEditor)),Category("Input")]
        public InArgument<string> InitialDirectory { get; set; }
        [Category("Input")] 
        public InArgument<string> Title { get; set; }
        [Category("Input")]
        public InArgument<string> DefaultExt { get; set; }
        [Category("Input")]
        public InArgument<string> Filter { get; set; }
        [Category("Input")]
        public InArgument<int> FilterIndex { get; set; }
        [Category("Input")]
        public InArgument<bool> CheckFileExists { get; set; }
        [Category("Input")]
        public InArgument<bool> CheckPathExists { get; set; }
        [Category("Input")]
        public InArgument<bool> Multiselect { get; set; }
        [RequiredArgument, OverloadGroup("Filename"), Category("Output")]
        public OutArgument<string> FileName { get; set; }
        [RequiredArgument, OverloadGroup("Filenames"), Category("Output")]
        public OutArgument<string[]> FileNames { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var isSaveAs = IsSaveAs.Get(context);
            var initialDirectory = InitialDirectory.Get(context);
            var title = Title.Get(context);
            var defaultExt = DefaultExt.Get(context);
            var filter = Filter.Get(context);
            var filterIndex = FilterIndex.Get(context);
            var checkFileExists = CheckFileExists.Get(context);
            var checkPathExists = CheckPathExists.Get(context);
            var multiselect = Multiselect.Get(context);
            System.Windows.Forms.FileDialog dialog;
            if(isSaveAs)
            {
                dialog = new System.Windows.Forms.SaveFileDialog();
            }
            else
            {
                dialog = new System.Windows.Forms.OpenFileDialog();
                ((System.Windows.Forms.OpenFileDialog)dialog).Multiselect = multiselect;
            }
            if(!string.IsNullOrEmpty(title)) dialog.Title = title;
            if (!string.IsNullOrEmpty(defaultExt)) dialog.DefaultExt = defaultExt;
            if (!string.IsNullOrEmpty(filter))
            {
                dialog.Filter = filter;
                dialog.FilterIndex = filterIndex;
            }
            try
            {
                if (!string.IsNullOrEmpty(initialDirectory) && !initialDirectory.Contains("\\"))
                {
                    Enum.TryParse(initialDirectory, out Environment.SpecialFolder specialfolder);
                    initialDirectory = Environment.GetFolderPath(specialfolder);
                }
            }
            catch (Exception)
            {

                throw;
            }
            if (!string.IsNullOrEmpty(initialDirectory)) dialog.InitialDirectory = initialDirectory;
            dialog.CheckFileExists = checkFileExists;
            dialog.CheckPathExists = checkPathExists;
            
            
            System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.Cancel;
            GenericTools.RunUI(() =>
            {
                result = dialog.ShowDialog();
            });
            if (result != System.Windows.Forms.DialogResult.OK)
            {
                context.SetValue(FileName, null);
                context.SetValue(FileNames, null);
                return;
            }
            context.SetValue(FileName, dialog.FileName);
            context.SetValue(FileNames, dialog.FileNames);
        }

    }

}