using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Office.Activities
{
    public class Attachment
    {
        public Attachment(Microsoft.Office.Interop.Outlook.Attachment Attachment)
        {
            _Attachment = Attachment;
            Class = Attachment.Class.ToString();
            DisplayName = Attachment.DisplayName;
            FileName = Attachment.FileName;
            Index = Attachment.Index;
            Size = Attachment.Size;
        }
        private Microsoft.Office.Interop.Outlook.Attachment _Attachment;
        public string Class { get; set; }
        public string DisplayName { get; set; }
        public string FileName { get; set; }
        public int Index { get; set; }
        public int Size { get; set; }
        public bool Delete()
        {
            _Attachment.Delete();
            return true;
        }
        public bool SaveTo(string Path, bool Overwrite = false)
        {
            return SaveWithFilename(System.IO.Path.Combine(Path, FileName), Overwrite); ;
        }
        public bool SaveWithFilename(string FilePath, bool Overwrite = false)
        {
            try
            {
                if (System.IO.File.Exists(FilePath))
                {
                    if(Overwrite) System.IO.File.Delete(FilePath);
                    if (!Overwrite) return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
            _Attachment.SaveAsFile(FilePath);
            return true;
        }
    }
}
