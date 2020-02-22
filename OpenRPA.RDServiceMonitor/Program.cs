using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDServiceMonitor
{
    class Program
    {
        public const string ServiceName = "OpenRPAMon";
        private static ServiceManager manager = new ServiceManager(ServiceName);
        private static ServiceManager OpenRPAmanager = new ServiceManager("OpenRPA");
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
        [System.Runtime.InteropServices.DllImport("NTDLL.DLL", SetLastError = true)]
        public static extern int NtQueryInformationProcess(IntPtr hProcess, PROCESSINFOCLASS pic, ref PROCESS_BASIC_INFORMATION pbi, int cb, out int pSize);
        public static System.Diagnostics.Process GetParentProcessId(System.Diagnostics.Process p = null)
        {
            if (p == null) p = System.Diagnostics.Process.GetCurrentProcess();
            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();

            //Get a handle to our own process
            IntPtr hProc = p.Handle; // OpenProcess((ProcessAccessFlags)0x001F0FFF, false, Process.GetCurrentProcess().Id);

            try
            {
                int queryStatus = NtQueryInformationProcess(hProc, 0, ref pbi, pbi.Size, out int sizeInfoReturned);
            }
            finally
            {
                if (!hProc.Equals(IntPtr.Zero))
                {
                    //Close handle and free allocated memory
                    // CloseHandle(hProc);
                }
            }
            var pid = (int)pbi.InheritedFromUniqueProcessId;
            if (pid < 1) return null;
            return System.Diagnostics.Process.GetProcessById(pid);
        }

        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                if (args[0].ToLower() == "uninstall" || args[0].ToLower() == "u")
                {
                    if (manager.IsServiceInstalled)
                    {
                        manager.UninstallService(typeof(Program));
                    }
                    return;
                }
            }
            var parentProcess = GetParentProcessId();
            var isService = (parentProcess.ProcessName.ToLower() == "services");
            Console.WriteLine("****** isService: " + isService);
            if (isService)
            {
                System.ServiceProcess.ServiceBase.Run(new MyServiceBase(ServiceName, DoWork));
            }
            else
            {
                MyServiceBase.isRunning = false;
                DoWork();
            }
        }
        private static void DoWork()
        {
            if (!manager.IsServiceInstalled)
            {
                manager.InstallService(typeof(Program), new string[] { });
            }
            while (MyServiceBase.isRunning)
            {
                System.Threading.Thread.Sleep(500);
                if (MyServiceBase.isRunning)
                {
                    if (OpenRPAmanager.Status == System.ServiceProcess.ServiceControllerStatus.Stopped)
                    {
                        _ = OpenRPAmanager.StartService();
                    }
                }
            }
        }
    }
}
