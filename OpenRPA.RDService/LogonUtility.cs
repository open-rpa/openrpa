//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OpenRPA.RDService
//{
//    using System.Runtime.InteropServices; 
//    using System.Security.Principal; 
//    public class LogonUtility
//    {
//        //import LSA functions
//        [DllImport("advapi32.dll")]
//        private static extern bool LogonUser(
//            String lpszUsername,
//            String lpszDomain,
//            String lpszPassword,
//            int dwLogonType,
//            int dwLogonProvider,
//            ref IntPtr phToken
//            );

//        [DllImport("advapi32.dll")]
//        private static extern bool DuplicateToken(
//            IntPtr ExistingTokenHandle,
//            int ImpersonationLevel,
//            ref IntPtr DuplicateTokenHandle
//            );

//        [DllImport("kernel32.dll")]
//        private static extern bool CloseHandle(IntPtr hObject);

//        [DllImport("advapi32.dll")]
//        private static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

//        [DllImport("kernel32.dll")]
//        private static extern int GetLastError();

//        //enum impersonation levels an logon types

//        private enum SecurityImpersonationLevel
//        {
//            SecurityAnonymous,
//            SecurityIdentification,
//            SecurityImpersonation,
//            SecurityDelegation
//        }

//        private enum LogonTypes
//        {
//            LOGON32_PROVIDER_DEFAULT = 0,
//            LOGON32_LOGON_INTERACTIVE = 2,
//            LOGON32_LOGON_NETWORK = 3,
//            LOGON32_LOGON_BATCH = 4,
//            LOGON32_LOGON_SERVICE = 5,
//            LOGON32_LOGON_UNLOCK = 7,
//            LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
//            LOGON32_LOGON_NEW_CREDENTIALS = 9
//        }

//        /// <summary>impersonates a user</summary>
//        /// <param name="sUsername">domain\name of the user account</param>
//        /// <param name="sPassword">the user's password</param>
//        /// <returns>the new WindowsImpersonationContext</returns>
//        public static WindowsImpersonationContext ImpersonateUser(String username, String password)
//        {
//            //define the handles
//            IntPtr existingTokenHandle = IntPtr.Zero;
//            IntPtr duplicateTokenHandle = IntPtr.Zero;

//            String domain;
//            if (username.IndexOf("\\") > 0)
//            {
//                //split domain and name
//                String[] splitUserName = username.Split('\\');
//                domain = splitUserName[0];
//                username = splitUserName[1];
//            }
//            else
//            {
//                domain = String.Empty;
//            }

//            bool isOkay = true;

//            try
//            {
//                //get a security token

//                isOkay = LogonUser(username, domain, password,
//                    (int)LogonTypes.LOGON32_LOGON_INTERACTIVE, (int)LogonTypes.LOGON32_PROVIDER_DEFAULT,
//                    ref existingTokenHandle);

//                if (!isOkay)
//                {
//                    int lastWin32Error = Marshal.GetLastWin32Error();
//                    int lastError = GetLastError();

//                    throw new Exception("LogonUser Failed: " + lastWin32Error + " - " + lastError);
//                }

//                // copy the token

//                isOkay = DuplicateToken(existingTokenHandle,
//                    (int)SecurityImpersonationLevel.SecurityImpersonation,
//                    ref duplicateTokenHandle);

//                if (!isOkay)
//                {
//                    int lastWin32Error = Marshal.GetLastWin32Error();
//                    int lastError = GetLastError();
//                    CloseHandle(existingTokenHandle);
//                    throw new Exception("DuplicateToken Failed: " + lastWin32Error + " - " + lastError);
//                }
//                else
//                {

//                    // create an identity from the token

//                    WindowsIdentity newId = new WindowsIdentity(duplicateTokenHandle);
//                    WindowsImpersonationContext impersonatedUser = newId.Impersonate();

//                    return impersonatedUser;
//                }
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//            finally
//            {
//                //free all handles
//                if (existingTokenHandle != IntPtr.Zero)
//                {
//                    CloseHandle(existingTokenHandle);
//                }
//                if (duplicateTokenHandle != IntPtr.Zero)
//                {
//                    CloseHandle(duplicateTokenHandle);
//                }
//            }
//        }
//    }
//}
