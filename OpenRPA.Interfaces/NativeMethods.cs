using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    using System.Runtime.InteropServices;
    public static class NativeMethods
    {

        public static bool Launch(System.Diagnostics.Process p, string CurrentDirectory, string appCmdLine)
        {
            string ErrorMessage; System.Diagnostics.Process newProcess;
            var token = GetPrimaryToken(p);
            if (token == IntPtr.Zero) return false;
            IntPtr envBlock = IntPtr.Zero;
            try
            {
                if (!CreateEnvironmentBlock(out envBlock, token, false)) return false;
                if (!LaunchProcessAsUser(CurrentDirectory, appCmdLine, token, envBlock, out newProcess, true, out ErrorMessage))
                {
                    Log.Error(ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
            finally
            {
                if (envBlock != IntPtr.Zero) DestroyEnvironmentBlock(envBlock);
            }
            return true;
        }
        public static bool LaunchProcessAsUser(string CurrentDirectory, string cmdLine, IntPtr token, IntPtr envBlock, out System.Diagnostics.Process newProcess, bool AllowClosingProcess, out string ErrorMessage)
        {
            bool result = false;
            newProcess = null;
            ErrorMessage = null;

            var pi = new PROCESS_INFORMATION();
            var saProcess = new SECURITY_ATTRIBUTES();
            var saThread = new SECURITY_ATTRIBUTES();
            saProcess.nLength = uint.Parse(Marshal.SizeOf(saProcess).ToString());
            saThread.nLength = uint.Parse(Marshal.SizeOf(saThread).ToString());
            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = "WinSta0\\Default";
            if (AllowClosingProcess) si.lpDesktop = "";
            si.dwFlags = (int)(STARTF.STARTF_USESHOWWINDOW | STARTF.STARTF_FORCEONFEEDBACK);
            si.wShowWindow = SW_SHOW;

            result = CreateProcessAsUser(token, null, cmdLine, ref saProcess, ref saThread, false,
                (uint)CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT, envBlock, CurrentDirectory, ref si, out pi);
            if(result == false)
            {
                var hResult = Marshal.GetLastWin32Error();
                ErrorMessage = String.Format("CreateProcessAsUser Error: {0}", hResult);
            } else
            {
                newProcess = System.Diagnostics.Process.GetProcessById(pi.dwProcessId);
            }
            return result;
        }
        public static string GetDomainName() => System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
        public static string GetHostName() => System.Net.Dns.GetHostName();
        public static string GetFQDN()
        {
            string domainName = GetDomainName();
            string hostName = GetHostName().ToLower();
            if (!hostName.EndsWith(domainName) && !string.IsNullOrEmpty(domainName)) // if hostname does not already include domain name
            {
                domainName = "." + domainName.ToLower();
                hostName += domainName; // add the domain name part
            }
            return hostName; // return the fully qualified name
        }
        public static bool MachineMemberOfDomain() => GetDomainName().Contains(".");
        public static bool IsAdmin(string username)
        {
            var wi = new System.Security.Principal.WindowsIdentity(username);
            var wp = new System.Security.Principal.WindowsPrincipal(wi);
            return wp.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        public static bool IsAdmin(IntPtr p_token)
        {
            var wi = new System.Security.Principal.WindowsIdentity(p_token);
            var wp = new System.Security.Principal.WindowsPrincipal(wi);
            return wp.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        public static bool IsDomainAdmin(IntPtr p_token)
        {
            var DomainName = GetDomainName();
            if (string.IsNullOrEmpty(DomainName)) return false;
            var d = System.DirectoryServices.ActiveDirectory.Domain.GetDomain(new System.DirectoryServices.ActiveDirectory.DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType.Domain, DomainName));
            var de = d.GetDirectoryEntry();
            byte[] domainSidArray = de.Properties["objectSid"].Value as byte[];
            var domainSid = new System.Security.Principal.SecurityIdentifier(domainSidArray, 0);
            var domainAdminsSid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.AccountDomainAdminsSid, domainSid);

            var wi = new System.Security.Principal.WindowsIdentity(p_token);
            var wp = new System.Security.Principal.WindowsPrincipal(wi);
            return wp.IsInRole(domainAdminsSid);
        }
        public static IntPtr GetPrimaryToken(System.Diagnostics.Process p)
        {
            IntPtr token = IntPtr.Zero;
            IntPtr primaryToken = IntPtr.Zero;
            bool retVal = false;
            //Gets impersonation token 
            retVal = OpenProcessToken(p.Handle, TOKEN_DUPLICATE, out token);
            if (retVal == true)
            {
                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                sa.nLength = (uint)Marshal.SizeOf(sa);
                //Convert the impersonation token into Primary token 
                retVal = DuplicateTokenEx(
                    token,
                    TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY,
                    ref sa,
                    (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                    (int)TOKEN_TYPE.TokenPrimary,
                    ref primaryToken);

                //Close the Token that was previously opened. 
                CloseHandle(token);
                if (retVal == false)
                {
                    string message = String.Format("DuplicateTokenEx Error: {0}", Marshal.GetLastWin32Error());
                    System.Diagnostics.Debug.WriteLine(message);
                }
            }
            else
            {
                string message = String.Format("OpenProcessToken Error: {0}", Marshal.GetLastWin32Error());
                System.Diagnostics.Debug.WriteLine(message);

            }
            //We'll Close this token after it is used. 
            return primaryToken;
        }
        public static System.Diagnostics.Process GetParentProcessId(System.Diagnostics.Process p = null)
        {
            if (p == null) p = System.Diagnostics.Process.GetCurrentProcess();
            NativeMethods.PROCESS_BASIC_INFORMATION pbi = new NativeMethods.PROCESS_BASIC_INFORMATION();

            //Get a handle to our own process
            IntPtr hProc = p.Handle; // OpenProcess((ProcessAccessFlags)0x001F0FFF, false, Process.GetCurrentProcess().Id);

            try
            {
                int sizeInfoReturned;
                int queryStatus = NativeMethods.NtQueryInformationProcess(hProc, 0, ref pbi, pbi.Size, out sizeInfoReturned);
            }
            finally
            {
                if (!hProc.Equals(IntPtr.Zero))
                {
                    //Close handle and free allocated memory
                    // CloseHandle(hProc);
                    hProc = IntPtr.Zero;
                }
            }
            var pid = (int)pbi.InheritedFromUniqueProcessId;
            if (pid < 1) return null;
            return System.Diagnostics.Process.GetProcessById(pid);
        }
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out SafeTokenHandle TokenHandle);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(SafeTokenHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);
        public static byte[] GetSIDByteArr(IntPtr processHandle)
        {
            int MAX_INTPTR_BYTE_ARR_SIZE = 512;
            IntPtr tokenHandle;
            byte[] sidBytes;

            // Get the Process Token
            if (!OpenProcessToken(processHandle, TOKEN_READ, out tokenHandle))
                throw new ApplicationException("Could not get process token.  Win32 Error Code: " + Marshal.GetLastWin32Error());

            uint tokenInfoLength = 0;
            bool result;
            result = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, tokenInfoLength, out tokenInfoLength);  // get the token info length
            IntPtr tokenInfo = Marshal.AllocHGlobal((int)tokenInfoLength);
            result = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, tokenInfo, tokenInfoLength, out tokenInfoLength);  // get the token info

            // Get the User SID
            if (result)
            {
                TOKEN_USER tokenUser = (TOKEN_USER)Marshal.PtrToStructure(tokenInfo, typeof(TOKEN_USER));
                sidBytes = new byte[MAX_INTPTR_BYTE_ARR_SIZE];  // Since I don't yet know how to be more precise w/ the size of the byte arr, it is being set to 512
                Marshal.Copy(tokenUser.User.Sid, sidBytes, 0, MAX_INTPTR_BYTE_ARR_SIZE);  // get a byte[] representation of the SID
            }
            else throw new ApplicationException("Could not get process token.  Win32 Error Code: " + Marshal.GetLastWin32Error());

            return sidBytes;
        }
        // Using IntPtr for pSID insted of Byte[]
        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);
        [System.Runtime.InteropServices.DllImport("NTDLL.DLL", SetLastError = true)]
        public static extern int NtQueryInformationProcess(IntPtr hProcess, PROCESSINFOCLASS pic, ref PROCESS_BASIC_INFORMATION pbi, int cb, out int pSize);
        public static string GetProcessSID(System.Diagnostics.Process process = null)
        {
            if (process == null) process = System.Diagnostics.Process.GetCurrentProcess();
            SafeTokenHandle hToken = null;
            try
            {
                if (!NativeMethods.OpenProcessToken(process.Handle, NativeMethods.TOKEN_QUERY, out hToken))
                {
                    return "";
                }
            }
            catch (Exception)
            {
                return "";
            }

            uint TokenInfLength = 0;
            bool Result;

            // first call gets lenght of TokenInformation
            Result = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, TokenInfLength, out TokenInfLength);
            IntPtr TokenInformation = Marshal.AllocHGlobal(int.Parse(TokenInfLength.ToString()));
            Result = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, TokenInformation, TokenInfLength, out TokenInfLength);
            if (Result)
            {
                TOKEN_USER TokenUser = (TOKEN_USER)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_USER));
                IntPtr pstr = IntPtr.Zero;
                Boolean ok = ConvertSidToStringSid(TokenUser.User.Sid, out pstr);
                string sidstr = Marshal.PtrToStringAuto(pstr);
                LocalFree(pstr);
                return sidstr;
            }
            return "";
        }
        public static string GetProcessUserName(System.Diagnostics.Process process = null)
        {
            if (process == null) process = System.Diagnostics.Process.GetCurrentProcess();
            if (process.HasExited) return "";
            SafeTokenHandle hToken = null;
            try
            {
                if (!NativeMethods.OpenProcessToken(process.Handle, NativeMethods.TOKEN_QUERY, out hToken))
                {
                    return "";
                }
            }
            catch (Exception)
            {
                return "";
            }

            uint TokenInfLength = 0;
            bool Result;

            // first call gets lenght of TokenInformation
            Result = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, TokenInfLength, out TokenInfLength);
            IntPtr TokenInformation = Marshal.AllocHGlobal(int.Parse(TokenInfLength.ToString()));
            Result = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, TokenInformation, TokenInfLength, out TokenInfLength);
            if (Result)
            {
                TOKEN_USER TokenUser = (TOKEN_USER)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_USER));

                var a = new System.Security.Principal.SecurityIdentifier(TokenUser.User.Sid);
                return a.Translate(typeof(System.Security.Principal.NTAccount)).ToString();
            }
            return "";
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr LocalFree(IntPtr hMem);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean CloseHandle(IntPtr hObject);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpThreadAttributes, Int32 ImpersonationLevel,
            Int32 dwTokenType, ref IntPtr phNewToken);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
    ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
    ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
        [DllImport("userenv.dll", SetLastError = true)]
        static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);
        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        public const int PROCESSBASICINFORMATION = 0;
        private static uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        private static uint TOKEN_DUPLICATE = 0x0002;
        private static uint TOKEN_IMPERSONATE = 0x0004;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_QUERY_SOURCE = 0x0010;
        private static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private static uint TOKEN_ADJUST_GROUPS = 0x0040;
        private static uint TOKEN_ADJUST_DEFAULT = 0x0080;
        private static uint TOKEN_ADJUST_SESSIONID = 0x0100;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        private static uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
            TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
            TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
            TOKEN_ADJUST_SESSIONID);
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MINIMIZE = 6;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_SHOWNOACTIVATE = 4;

        public const int SW_NORMAL = 1;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOW = 5;
        [Flags]
        enum CreateProcessFlags
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }
        [Flags]
        public enum STARTF : uint
        {
            STARTF_USESHOWWINDOW = 0x00000001,
            STARTF_USESIZE = 0x00000002,
            STARTF_USEPOSITION = 0x00000004,
            STARTF_USECOUNTCHARS = 0x00000008,
            STARTF_USEFILLATTRIBUTE = 0x00000010,
            STARTF_RUNFULLSCREEN = 0x00000020,  // ignored for non-x86 platforms
            STARTF_FORCEONFEEDBACK = 0x00000040,
            STARTF_FORCEOFFFEEDBACK = 0x00000080,
            STARTF_USESTDHANDLES = 0x00000100,
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        internal enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }
        internal enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            public uint nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }
        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }
        public class SafeTokenHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeTokenHandle() : base(true) { }
            public SafeTokenHandle(IntPtr handle) : base(true)
            {
                SetHandle(handle);
            }
            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }
        }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public UIntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
            public int Size
            {
                get { return (int)System.Runtime.InteropServices.Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)); }
                // get { return (1 * sizeof(int) + 5 * IntPtr.Size); }
            }
        }
        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin
        }
        public enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0, // 0, q: PROCESS_BASIC_INFORMATION, PROCESS_EXTENDED_BASIC_INFORMATION
            ProcessQuotaLimits, // qs: QUOTA_LIMITS, QUOTA_LIMITS_EX
            ProcessIoCounters, // q: IO_COUNTERS
            ProcessVmCounters, // q: VM_COUNTERS, VM_COUNTERS_EX
            ProcessTimes, // q: KERNEL_USER_TIMES
            ProcessBasePriority, // s: KPRIORITY
            ProcessRaisePriority, // s: ULONG
            ProcessDebugPort, // q: HANDLE
            ProcessExceptionPort, // s: HANDLE
            ProcessAccessToken, // s: PROCESS_ACCESS_TOKEN
            ProcessLdtInformation, // 10
            ProcessLdtSize,
            ProcessDefaultHardErrorMode, // qs: ULONG
            ProcessIoPortHandlers, // (kernel-mode only)
            ProcessPooledUsageAndLimits, // q: POOLED_USAGE_AND_LIMITS
            ProcessWorkingSetWatch, // q: PROCESS_WS_WATCH_INFORMATION[]; s: void
            ProcessUserModeIOPL,
            ProcessEnableAlignmentFaultFixup, // s: BOOLEAN
            ProcessPriorityClass, // qs: PROCESS_PRIORITY_CLASS
            ProcessWx86Information,
            ProcessHandleCount, // 20, q: ULONG, PROCESS_HANDLE_INFORMATION
            ProcessAffinityMask, // s: KAFFINITY
            ProcessPriorityBoost, // qs: ULONG
            ProcessDeviceMap, // qs: PROCESS_DEVICEMAP_INFORMATION, PROCESS_DEVICEMAP_INFORMATION_EX
            ProcessSessionInformation, // q: PROCESS_SESSION_INFORMATION
            ProcessForegroundInformation, // s: PROCESS_FOREGROUND_BACKGROUND
            ProcessWow64Information, // q: ULONG_PTR
            ProcessImageFileName, // q: UNICODE_STRING
            ProcessLUIDDeviceMapsEnabled, // q: ULONG
            ProcessBreakOnTermination, // qs: ULONG
            ProcessDebugObjectHandle, // 30, q: HANDLE
            ProcessDebugFlags, // qs: ULONG
            ProcessHandleTracing, // q: PROCESS_HANDLE_TRACING_QUERY; s: size 0 disables, otherwise enables
            ProcessIoPriority, // qs: ULONG
            ProcessExecuteFlags, // qs: ULONG
            ProcessResourceManagement,
            ProcessCookie, // q: ULONG
            ProcessImageInformation, // q: SECTION_IMAGE_INFORMATION
            ProcessCycleTime, // q: PROCESS_CYCLE_TIME_INFORMATION
            ProcessPagePriority, // q: ULONG
            ProcessInstrumentationCallback, // 40
            ProcessThreadStackAllocation, // s: PROCESS_STACK_ALLOCATION_INFORMATION, PROCESS_STACK_ALLOCATION_INFORMATION_EX
            ProcessWorkingSetWatchEx, // q: PROCESS_WS_WATCH_INFORMATION_EX[]
            ProcessImageFileNameWin32, // q: UNICODE_STRING
            ProcessImageFileMapping, // q: HANDLE (input)
            ProcessAffinityUpdateMode, // qs: PROCESS_AFFINITY_UPDATE_MODE
            ProcessMemoryAllocationMode, // qs: PROCESS_MEMORY_ALLOCATION_MODE
            ProcessGroupInformation, // q: USHORT[]
            ProcessTokenVirtualizationEnabled, // s: ULONG
            ProcessConsoleHostProcess, // q: ULONG_PTR
            ProcessWindowInformation, // 50, q: PROCESS_WINDOW_INFORMATION
            ProcessHandleInformation, // q: PROCESS_HANDLE_SNAPSHOT_INFORMATION // since WIN8
            ProcessMitigationPolicy, // s: PROCESS_MITIGATION_POLICY_INFORMATION
            ProcessDynamicFunctionTableInformation,
            ProcessHandleCheckingMode,
            ProcessKeepAliveCount, // q: PROCESS_KEEPALIVE_COUNT_INFORMATION
            ProcessRevokeFileHandles, // s: PROCESS_REVOKE_FILE_HANDLES_INFORMATION
            MaxProcessInfoClass
        };
    }
}
