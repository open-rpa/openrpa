using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    using System;
    using System.Runtime.InteropServices;

    public static class VersionHelper
    {
        const byte VER_EQUAL = 1;
        const byte VER_GREATER = 2;
        const byte VER_GREATER_EQUAL = 3;
        const byte VER_LESS = 4;
        const byte VER_LESS_EQUAL = 5;
        const byte VER_AND = 6;
        const byte VER_OR = 7;

        const byte VER_CONDITION_MASK = 7;
        const byte VER_NUM_BITS_PER_CONDITION_MASK = 3;

        //
        // RtlVerifyVersionInfo() type mask bits
        //

        const uint VER_MINORVERSION = 0x0000001;
        const uint VER_MAJORVERSION = 0x0000002;
        const uint VER_BUILDNUMBER = 0x0000004;
        const uint VER_PLATFORMID = 0x0000008;
        const uint VER_SERVICEPACKMINOR = 0x0000010;
        const uint VER_SERVICEPACKMAJOR = 0x0000020;
        const uint VER_SUITENAME = 0x0000040;
        const uint VER_PRODUCT_TYPE = 0x0000080;

        // wProductType    
        // Any additional information about the system.This member can be one of the following values.
        const byte VER_NT_DOMAIN_CONTROLLER = 0x0000002;
        const byte VER_NT_SERVER = 0x0000003;
        const byte VER_NT_WORKSTATION = 0x0000001;

        //
        // _WIN32_WINNT version constants
        //
        const ushort _WIN32_WINNT_NT4 = 0x0400;
        const ushort _WIN32_WINNT_WIN2K = 0x0500;
        const ushort _WIN32_WINNT_WINXP = 0x0501;
        const ushort _WIN32_WINNT_WS03 = 0x0502;
        const ushort _WIN32_WINNT_WIN6 = 0x0600;
        const ushort _WIN32_WINNT_VISTA = 0x0600;
        const ushort _WIN32_WINNT_WS08 = 0x0600;
        const ushort _WIN32_WINNT_LONGHORN = 0x0600;
        const ushort _WIN32_WINNT_WIN7 = 0x0601;
        const ushort _WIN32_WINNT_WIN8 = 0x0602;
        const ushort _WIN32_WINNT_WINBLUE = 0x0603;
        const ushort _WIN32_WINNT_WINTHRESHOLD = 0x0A00; /* ABRACADABRA_THRESHOLD*/
        const ushort _WIN32_WINNT_WIN10 = 0x0A00; /* ABRACADABRA_THRESHOLD*/

        const bool FALSE = false;

        static byte LOBYTE(ushort w)
        {
            return ((byte)(w & 0xff));
        }

        static byte HIBYTE(ushort w)
        {
            return ((byte)(w >> 8 & 0xff));
        }


        [DllImport("kernel32.dll")]
        static extern ulong VerSetConditionMask(ulong ConditionMask, uint TypeMask, byte Condition);

        [DllImport("kernel32.dll")]
        static extern bool VerifyVersionInfoW(ref OSVERSIONINFOEXW lpVersionInformation, uint dwTypeMask, ulong dwlConditionMask);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct OSVERSIONINFOEXW
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public UInt16 wServicePackMajor;
            public UInt16 wServicePackMinor;
            public UInt16 wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }


        public static bool
        IsWindowsVersionOrGreater(ushort wMajorVersion, ushort wMinorVersion, ushort wServicePackMajor)
        {
            var osvi = new OSVERSIONINFOEXW
            {
                dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEXW))
            };

            var dwlConditionMask = VerSetConditionMask(
                VerSetConditionMask(
                VerSetConditionMask(
                    0, VER_MAJORVERSION, VER_GREATER_EQUAL),
                       VER_MINORVERSION, VER_GREATER_EQUAL),
                       VER_SERVICEPACKMAJOR, VER_GREATER_EQUAL);

            osvi.dwMajorVersion = wMajorVersion;
            osvi.dwMinorVersion = wMinorVersion;
            osvi.wServicePackMajor = wServicePackMajor;

            return VerifyVersionInfoW(ref osvi, VER_MAJORVERSION | VER_MINORVERSION | VER_SERVICEPACKMAJOR, dwlConditionMask) != FALSE;
        }

        public static bool
        IsWindowsXPOrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINXP), LOBYTE(_WIN32_WINNT_WINXP), 0);
        }

        public static bool
        IsWindowsXPSP1OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINXP), LOBYTE(_WIN32_WINNT_WINXP), 1);
        }

        public static bool
        IsWindowsXPSP2OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINXP), LOBYTE(_WIN32_WINNT_WINXP), 2);
        }

        public static bool
        IsWindowsXPSP3OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINXP), LOBYTE(_WIN32_WINNT_WINXP), 3);
        }

        public static bool
        IsWindowsVistaOrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_VISTA), LOBYTE(_WIN32_WINNT_VISTA), 0);
        }

        public static bool
        IsWindowsVistaSP1OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_VISTA), LOBYTE(_WIN32_WINNT_VISTA), 1);
        }

        public static bool
        IsWindowsVistaSP2OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_VISTA), LOBYTE(_WIN32_WINNT_VISTA), 2);
        }

        public static bool
        IsWindows7OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN7), LOBYTE(_WIN32_WINNT_WIN7), 0);
        }

        public static bool
        IsWindows7SP1OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN7), LOBYTE(_WIN32_WINNT_WIN7), 1);
        }

        public static bool
        IsWindows8OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN8), LOBYTE(_WIN32_WINNT_WIN8), 0);
        }

        public static bool
        IsWindows8Point1OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINBLUE), LOBYTE(_WIN32_WINNT_WINBLUE), 0);
        }

        public static bool
        IsWindowsThresholdOrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINTHRESHOLD), LOBYTE(_WIN32_WINNT_WINTHRESHOLD), 0);
        }

        public static bool
        IsWindows10OrGreater()
        {
            return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINTHRESHOLD), LOBYTE(_WIN32_WINNT_WINTHRESHOLD), 0);
        }

        public static bool
        IsWindowsServer()
        {
            var osvi = new OSVERSIONINFOEXW
            {
                dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEXW)),
                wProductType = VER_NT_WORKSTATION
            };

            var dwlConditionMask = VerSetConditionMask(0, VER_PRODUCT_TYPE, VER_EQUAL);

            return !VerifyVersionInfoW(ref osvi, VER_PRODUCT_TYPE, dwlConditionMask);
        }
    }
}
