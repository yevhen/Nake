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
}
