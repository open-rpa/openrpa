using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;

namespace OpenRPA.SetupActions
{
    public class CustomActions
    {
        // https://www.add-in-express.com/creating-addins-blog/2014/01/29/create-wix-custom-actions/
        // https://stackoverflow.com/questions/2742359/how-to-conditionally-exclude-features-from-featuresdlg-in-wix-3-0-from-a-manag

        private static bool IsOfficeInstalled()
        {
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Winword.exe");
            if (key != null)
            {
                key.Close();
            }
            return key != null;
        }

        [CustomAction]
        public static ActionResult HasExcel(Session session)
        {
            session.Log("Begin CustomAction HasExcel");

            if(IsOfficeInstalled())
            {
                session.Log("set INSTALLOFFICEFEATURE to TRUE");
                session["INSTALLOFFICEFEATURE"] = "TRUE";
            }
            else
            {
                session.Log("set INSTALLOFFICEFEATURE to FALSE");
                session["INSTALLOFFICEFEATURE"] = "FALSE";
            }
            //var result1 = System.Windows.Forms.MessageBox.Show("Enable Office Feature?", "Enable Office Feature?", System.Windows.Forms.MessageBoxButtons.YesNo);
            //if (result1 == System.Windows.Forms.DialogResult.Yes)
            //{
            //    session.Log("set INSTALLOFFICEFEATURE to TRUE");
            //    session["INSTALLOFFICEFEATURE"] = "TRUE";
            //}
            //else
            //{
            //    session.Log("set INSTALLOFFICEFEATURE to FALSE");
            //    session["INSTALLOFFICEFEATURE"] = "FALSE";
            //}
            // session.Database.Execute("UPDATE Feature SET RuntimeLevel=0 WHERE Feature='OfficeFeature'");
            session.Log("End CustomAction HasExcel");
            return ActionResult.Success;
        }



        [CustomAction]
        public static ActionResult FixPaths(Session session)
        {
            session.Log("Begin FixPaths");
            //System.Diagnostics.Debugger.Launch();
            //System.Diagnostics.Debugger.Break();
            string dir = "";
            try
            {
                dir = session.CustomActionData["INSTALLDIR"];
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            if (string.IsNullOrEmpty(dir)) return ActionResult.Failure;

            if (dir.EndsWith("\\")) dir = dir.Substring(0, dir.Length - 1);


            var filename = System.IO.Path.Combine(dir, "chromemanifest.json");
            if (System.IO.File.Exists(filename))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filename);
                    json = json.Replace("REPLACEPATH", dir.Replace("\\", "\\\\"));
                    System.IO.File.WriteAllText(filename, json);
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }
            filename = System.IO.Path.Combine(dir, "ffmanifest.json");
            if (System.IO.File.Exists(filename))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filename);
                    json = json.Replace("REPLACEPATH", dir.Replace("\\", "\\\\"));
                    System.IO.File.WriteAllText(filename, json);
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }
            session.Log("End FixPaths");
            session.Log("FixPaths: return: " + ActionResult.Success);
            return ActionResult.Success;
        }

    }
}
