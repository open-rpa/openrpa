using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;

namespace CheckForOffice
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult HasExcel(Session session)
        {
            session.Log("Begin CustomAction1");

            var result1 = System.Windows.Forms.MessageBox.Show("OfficeFeature?", "OfficeFeature", System.Windows.Forms.MessageBoxButtons.YesNo);

            if(result1 == System.Windows.Forms.DialogResult.Yes)
            {
                session["INSTALLMYFEATURE"] = "TRUE";
            } else
            {
                session["INSTALLMYFEATURE"] = "FALSE";
            }
            // session.Database.Execute("UPDATE Feature SET RuntimeLevel=0 WHERE Feature='OfficeFeature'");
            // session["HASEXCEL"] = "2";
            // session["INSTALLMYFEATURE"] = "TRUE";
            // session["INSTALLMYFEATURE"] = "FALSE";
            return ActionResult.Success;
        }
    }
}
