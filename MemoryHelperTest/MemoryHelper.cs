using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace KH_NET_UI.MemoryServices
{
    public class MemoryHelpers
    {
        [DllImport(@"C:\Users\SimpleAOB\Documents\Visual Studio 2015\Projects\KH NET UI\DLLBuildOnly\KHNet_MemoryHelperDLL_Win32-x86.dll")]
        static extern void InitiateAOBScan(ulong TARGETHANDLE, string ORIGINALSIG, string CHANGEDSIG);

        private Process Target;
        internal static int ThreadsExited = 0;
        internal static bool FoundAOBs = false;
        private string LastScanError = "None";

        public MemoryHelpers(int id)
        {
            Target = GetProcess(id);
        }
        private Process GetProcess(int pid)
        {
            try
            {
                Process p = Process.GetProcessById(pid);
                if (p.Handle != null) return p;
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
        public string GetLastError()
        {
            return LastScanError;
        }
        public bool GetAOBFoundStatus()
        {
            return FoundAOBs;
        }
        private List<Thread> ThreadList = new List<Thread>();
        public bool PatternScanAOB(string originalSig, string changedSig)
        {
            InitiateAOBScan((ulong)Target.Handle, originalSig, changedSig);
            return true;
        }
    }
}
