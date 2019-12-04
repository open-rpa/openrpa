//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OpenRPA.RDService
//{
//    using System.Runtime.InteropServices;
//    class SessionFinder
//    {
//        //Get the currently active local console session.
//        public IntPtr GetLocalInteractiveSession()
//        {
//            IntPtr hToken = IntPtr.Zero;

//            int sessionID = WTSGetActiveConsoleSessionId();

//            if (sessionID != -1) //-1 no console session.
//            {
//                if (!WTSQueryUserToken(sessionID, out hToken))
//                {
//                    throw new System.ComponentModel.Win32Exception();
//                }
//            }
//            return hToken;
//        }
//        //Get the session of the provided user. The same user could have 
//        //more than one session, this just retrieves the first one found. 
//        //More sophisticated checks could easily be added by requesting 
//        //different types of information from WTSQuerySessionInformation.
//        public IntPtr GetSessionByUser(string domain, string userName)
//        {
//            IntPtr hToken = IntPtr.Zero;
//            int sessionID = -1;

//            IntPtr pSessionsBuffer;
//            int sessionsCount;

//            if (!WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1,
//                out pSessionsBuffer, out sessionsCount))
//            {
//                throw new System.ComponentModel.Win32Exception();
//            }

//            try
//            {
//                for (int i = 0; i < sessionsCount; ++i)
//                {
//                    IntPtr pCurrentSession = new IntPtr(pSessionsBuffer.ToInt64() +
//                        Marshal.SizeOf(typeof(WTS_SESSION_INFO)) * i);
//                    WTS_SESSION_INFO sessionInfo = (WTS_SESSION_INFO)Marshal.PtrToStructure(pCurrentSession,
//                        typeof(WTS_SESSION_INFO));

//                    if (string.Compare(GetUserName(sessionInfo.SessionId), userName, true) == 0 &&
//                        string.Compare(GetDomain(sessionInfo.SessionId), domain, true) == 0)
//                    {
//                        sessionID = sessionInfo.SessionId;
//                        break;
//                    }
//                }
//            }
//            finally
//            {
//                WTSFreeMemory(pSessionsBuffer);
//            }

//            if (sessionID != -1)
//            {
//                if (!WTSQueryUserToken(sessionID, out hToken))
//                {
//                    throw new System.ComponentModel.Win32Exception();
//                }
//            }

//            return hToken;
//        }
//        private string GetUserName(int sessionID)
//        {
//            IntPtr pNameBuffer;
//            int nameLength;
//            string name;

//            if (!WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, sessionID,
//                    WTSUserName, out pNameBuffer, out nameLength))
//            {
//                throw new System.ComponentModel.Win32Exception();
//            }

//            try
//            {
//                name = Marshal.PtrToStringUni(pNameBuffer);
//            }
//            finally
//            {
//                WTSFreeMemory(pNameBuffer);
//            }

//            return name;
//        }
//        private string GetDomain(int sessionID)
//        {
//            IntPtr pDomainBuffer;
//            int domainLength;
//            string domain;

//            if (!WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, sessionID,
//                    WTSDomainName, out pDomainBuffer, out domainLength))
//            {
//                throw new System.ComponentModel.Win32Exception();
//            }

//            try
//            {
//                domain = Marshal.PtrToStringUni(pDomainBuffer);
//            }
//            finally
//            {
//                WTSFreeMemory(pDomainBuffer);
//            }

//            return domain;
//        }
//        [DllImport("WtsApi32.dll", SetLastError = true)]
//        [return: MarshalAs(UnmanagedType.Bool)]
//        private static extern bool WTSQueryUserToken(int SessionId,
//            out IntPtr phToken);
//        [DllImport("kernel32.dll", SetLastError = true)]
//        private static extern int WTSGetActiveConsoleSessionId();
//        [DllImport("WtsApi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
//        [return: MarshalAs(UnmanagedType.Bool)]
//        private static extern bool WTSQuerySessionInformation(IntPtr hServer,
//            int SessionId, int WTSInfoClass, out IntPtr ppBuffer, out int pBytesReturned);
//        [DllImport("WtsApi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
//        [return: MarshalAs(UnmanagedType.Bool)]
//        private static extern bool WTSEnumerateSessions(IntPtr hServer, int Reserved,
//            int Version, out IntPtr ppSessionInfo, out int pCount);
//        [DllImport("WtsApi32.dll", SetLastError = true)]
//        private static extern void WTSFreeMemory(IntPtr pMemory);
//        private static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = (IntPtr)0;
//        private const int WTSUserName = 5;
//        private const int WTSDomainName = 7;
//        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
//        struct WTS_SESSION_INFO
//        {
//            public int SessionId;
//            public string pWinStationName;
//            public int State;
//        }

//    }
//}
