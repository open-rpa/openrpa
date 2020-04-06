using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SetupActions
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    public class MS_Office
    {
        public static int GetOfficeVersion()
        {
            try
            {
                string sVersion = string.Empty;
                int Version = -1;
                var app = (dynamic)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("000209FF-0000-0000-C000-000000000046")));
                // Microsoft.Office.Interop.Word.Application appVersion = new Microsoft.Office.Interop.Word.Application();
                app.Visible = false;
                sVersion = app.Version.ToString();
                switch (sVersion)
                {
                    case "7.0":
                        Version = 95;
                        break;
                    case "8.0":
                        Version = 97;
                        break;
                    case "9.0":
                        Version = 2000;
                        break;
                    case "10.0":
                        Version = 2002;
                        break;
                    case "11.0":
                        Version = 2003;
                        break;
                    case "12.0":
                        Version = 2007;
                        break;
                    case "14.0":
                        Version = 2010;
                        break;
                    case "15.0":
                        Version = 2013;
                        break;
                    case "16.0":
                        Version = 2016;
                        break;
                    default:
                        Version = 9999;
                        break;
                }
                // System.Windows.Forms.MessageBox.Show(sVersion);
                return Version;

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
                return -1;
            }
        }
    }
}
