using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Nake
{
    class CaseInsensitiveEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.ToLower().GetHashCode();
        }
    }

    static class Runner
    {
        public static string Label = ParentProcess.IsPowerShell ? @".\nake" : "nake";
    }

    static class ParentProcess
    {
        public static bool IsPowerShell;

        static ParentProcess()
        {
            var parent = GetParentProcess();

            while (parent != null)
            {
                if (parent.ProcessName == "powershell")
                {
                    IsPowerShell = true;
                    break;
                }

                parent = GetParentProcess(parent);
            }
        }

        static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess());
        }

        static Process GetParentProcess(Process process)
        {
            var parentPid = 0;
            var processPid = process.Id;

            var oHnd = IntPtr.Zero;
            try
            {
                oHnd = CreateToolhelp32Snapshot(Th32CsSnapprocess, 0);
                if (oHnd == IntPtr.Zero)
                    return null;

                var procInfo = new PROCESSENTRY32 {dwSize = (uint) Marshal.SizeOf(typeof(PROCESSENTRY32))};
                if (Process32First(oHnd, ref procInfo) == false)
                    return null;

                do
                {
                    if (processPid == procInfo.th32ProcessID)
                        parentPid = (int) procInfo.th32ParentProcessID;

                }
                while (parentPid == 0 && Process32Next(oHnd, ref procInfo));

                try
                {
                    return parentPid > 0 ? Process.GetProcessById(parentPid) : null;
                }
                catch (ArgumentException) 
                {
                    return null;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
            finally
            {
                if (oHnd != IntPtr.Zero)
                    CloseHandle(oHnd);
            }
        }

        const uint Th32CsSnapprocess = 2;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public readonly uint cntUsage;
            public readonly uint th32ProcessID;
            public readonly IntPtr th32DefaultHeapID;
            public readonly uint th32ModuleID;
            public readonly uint cntThreads;
            public readonly uint th32ParentProcessID;
            public readonly int pcPriClassBase;
            public readonly uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] 
            public readonly string szExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }

    public static class GacUtil
    {
        [DllImport("fusion.dll")]
        private static extern IntPtr CreateAssemblyCache(
            out IAssemblyCache ppAsmCache,
            int reserved);

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
        private interface IAssemblyCache
        {
            int Dummy1();

            [PreserveSig()]
            IntPtr QueryAssemblyInfo(
                int flags,
                [MarshalAs(UnmanagedType.LPWStr)] string assemblyName,
                ref AssemblyInfo assemblyInfo);

            int Dummy2();
            int Dummy3();
            int Dummy4();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AssemblyInfo
        {
            public int cbAssemblyInfo;
            public int assemblyFlags;
            public long assemblySizeInKB;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string currentAssemblyPath;

            public int cchBuf;
        }

        public static bool IsAssemblyInGac(string assemblyName)
        {
            var assembyInfo = new AssemblyInfo { cchBuf = 512 };
            assembyInfo.currentAssemblyPath = new string('\0', assembyInfo.cchBuf);

            IAssemblyCache assemblyCache;
            var hr = CreateAssemblyCache(out assemblyCache, 0);

            if (hr == IntPtr.Zero)
            {
                hr = assemblyCache.QueryAssemblyInfo(1, assemblyName, ref assembyInfo);
                return hr == IntPtr.Zero;
            }

            Marshal.ThrowExceptionForHR(hr.ToInt32());
            return false;
        }
    }
}
