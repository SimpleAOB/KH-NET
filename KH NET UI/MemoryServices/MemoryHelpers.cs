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
        //MBI Consts
        const int MEM_COMMIT = 0x1000;
        const int MEM_FREE = 0x10000;
        const int MEM_RESERVE = 0x2000;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION32 lpBuffer, uint dwLength);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION64
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public int AllocationProtect;
            public int __alignment1;
            public ulong RegionSize;
            public int State;
            public int Protect;
            public int Type;
            public int __alignment2;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION32
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }
        public struct MEMORY_BASIC_INFORMATION_ANY
        {
            public IntPtr BaseAddress;
            public IntPtr RegionSize;
            public MEMORY_BASIC_INFORMATION_ANY(IntPtr _BaseAddress, IntPtr _RegionSize)
            {
                BaseAddress = _BaseAddress;
                RegionSize = _RegionSize;
            }
        }

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
        private ConcurrentStack<MEMORY_BASIC_INFORMATION_ANY> MemoryBlockStack;
        private List<Thread> ThreadList;
        public bool PatternScanAOB(string originalSig, string changedSig)
        {
            if (Target == null)
            {
                LastScanError = "Specified target not running";
                return false;
            }
            try
            {
                ThreadsExited = 0;
                ThreadList = new List<Thread>();
                FoundAOBs = false;
                LastScanError = "None";

                List<MEMORY_BASIC_INFORMATION_ANY> MemoryBlockList = new List<MEMORY_BASIC_INFORMATION_ANY>();
                long CurrentAddress = 0;

                bool EnvironmentBits = Environment.Is64BitProcess;

                int VQExRes = -1;
                while (VQExRes != 0)
                {
                    MEMORY_BASIC_INFORMATION_ANY MBIAny;
                    if (!EnvironmentBits)
                    {
                        MEMORY_BASIC_INFORMATION32 mem_basic_info32 = new MEMORY_BASIC_INFORMATION32();
                        uint structSize = (uint)Marshal.SizeOf(mem_basic_info32);

                        VQExRes = VirtualQueryEx(Target.Handle, new IntPtr(CurrentAddress), out mem_basic_info32, structSize);
                        MBIAny = new MEMORY_BASIC_INFORMATION_ANY((IntPtr)mem_basic_info32.BaseAddress, (IntPtr)mem_basic_info32.RegionSize);

                        if (mem_basic_info32.State == MEM_COMMIT)
                        {
                            MemoryBlockList.Add(MBIAny);
                        }
                    }
                    else
                    {
                        MEMORY_BASIC_INFORMATION64 mem_basic_info64 = new MEMORY_BASIC_INFORMATION64();
                        uint structSize = (uint)Marshal.SizeOf(mem_basic_info64);

                        VQExRes = VirtualQueryEx(Target.Handle, new IntPtr(CurrentAddress), out mem_basic_info64, structSize);
                        MBIAny = new MEMORY_BASIC_INFORMATION_ANY((IntPtr)mem_basic_info64.BaseAddress, (IntPtr)mem_basic_info64.RegionSize);

                        if (mem_basic_info64.State == MEM_COMMIT)
                        {
                            MemoryBlockList.Add(MBIAny);
                        }
                    }
                    CurrentAddress += (long)MBIAny.RegionSize;
                }
                MemoryBlockStack = new ConcurrentStack<MEMORY_BASIC_INFORMATION_ANY>(MemoryBlockList);
                //Spawn threads
                int LP = Environment.ProcessorCount;

                for (int i = 0; i < LP; i++)
                {
                    PatternScanThreads pst = new PatternScanThreads(i, originalSig, changedSig, Target.Handle);
                    Thread newThread = new Thread(() => pst.StartAOB(MemoryBlockStack, ref FoundAOBs));
                    newThread.Name = "MemoryProcessT" + i;
                    newThread.Start();
                    ThreadList.Add(newThread);
                }
                while (ThreadsExited < LP) { }
                GC.Collect();
                if (!FoundAOBs)
                {
                    LastScanError = "Failed to find one or more signatures";
                }
                return true;
            }
            catch (ThreadAbortException)
            {
                LastScanError = "Scan aborted";
                GC.Collect();
                return false;
            }
        }
        public void AbortScan(bool force)
        {
            lock (MemoryBlockStack) { MemoryBlockStack.Clear(); }
            if (force)
            {
                ThreadList.ForEach((t) => { t.Abort(); });
            }
        }
    }
    internal class PatternScanThreads
    {
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        private int ThreadNum;
        private string SigOriginal;
        private string SigChanged;
        IntPtr Handle;

        public PatternScanThreads(int _ThreadNum, string _SigOriginal, string _SigChanged, IntPtr _Handle)
        {
            ThreadNum = _ThreadNum;
            SigOriginal = _SigOriginal;
            SigChanged = _SigChanged;
            Handle = _Handle;
        }
        public void StartAOB(ConcurrentStack<MemoryHelpers.MEMORY_BASIC_INFORMATION_ANY> MemoryBlockStack, ref bool FoundAOBs)
        {
            MemoryHelpers.MEMORY_BASIC_INFORMATION_ANY CurrentRegion;
            while (MemoryBlockStack.TryPop(out CurrentRegion) == true)
            {
                bool EditedMemory = ModifyProcess(CurrentRegion);
                if (EditedMemory) FoundAOBs = true;
            }
            Interlocked.Add(ref MemoryHelpers.ThreadsExited, 1);
            Console.WriteLine("Exiting Thread " + ThreadNum);
            GC.Collect();
        }

        private bool ModifyProcess(MemoryHelpers.MEMORY_BASIC_INFORMATION_ANY Region)
        {
            byte[] ReadBuffer = new byte[(int)Region.RegionSize];
            int BytesRead = 0;
            bool RPM = ReadProcessMemory(Handle, Region.BaseAddress, ReadBuffer, (int)Region.RegionSize, ref BytesRead);
            bool FindPatternSuccess = FindPattern(ReadBuffer, Region);
            ReadBuffer = null;
            return FindPatternSuccess;
        }
        private bool FindPattern(byte[] ReadBuffer, MemoryHelpers.MEMORY_BASIC_INFORMATION_ANY Region)
        {
            int location = 0;
            bool FoundAOB = false;
            try
            {
                string[] AOBPartsO = SigOriginal.Split(' ');
                string[] AOBPartsC = SigChanged.Split(' ');
                byte?[] SearchBuffer = new byte?[AOBPartsO.Length];
                byte[] WriteBuffer = new byte[AOBPartsC.Length];
                int SkipZeroBytes = 0;
                bool IncrementZero = true;
                for (int i = 0; i < SearchBuffer.Length; i++, location++)
                {
                    if (AOBPartsO[i] == "??")
                    {
                        SearchBuffer[i] = null;
                    }
                    else
                    {
                        if (AOBPartsO[i] == "00" && IncrementZero)
                        {
                            SkipZeroBytes++;
                        }
                        else
                        {
                            IncrementZero = false;
                        }
                        SearchBuffer[i] = Convert.ToByte(AOBPartsO[i], 16);
                    }
                }
                for (int i = 0; i < ReadBuffer.Length; i++)
                {
                    if (ReadBuffer[i] == SearchBuffer[SkipZeroBytes])
                    {
                        bool found = true;
                        for (int j = 0; j < SearchBuffer.Length; j++)
                        {
                            if (SearchBuffer[j] == null) continue;
                            try
                            {
                                if (ReadBuffer[i + j - SkipZeroBytes] != SearchBuffer[j])
                                {
                                    i += j;
                                    found = false;
                                    break;
                                }
                            }
                            catch (IndexOutOfRangeException)
                            {
                                //Throw this shit away, just means that it found a match at the end of a memory block
                            }
                        }
                        if (found)
                        {
                            Console.WriteLine("Found AOB at " + Region.BaseAddress + i);
                            byte?[] ChangeBuffer = new byte?[AOBPartsC.Length];
                            for (int j = 0; j < ChangeBuffer.Length; j++)
                            {
                                if (AOBPartsC[j] == "??")
                                {
                                    ChangeBuffer[j] = null;
                                }
                                else
                                {
                                    ChangeBuffer[j] = Convert.ToByte(AOBPartsC[j], 16);
                                }
                            }

                            for (int j = 0; j < ChangeBuffer.Length; j++)
                            {
                                if (ChangeBuffer[j] == null)
                                {
                                    WriteBuffer[j] = ReadBuffer[i + j - SkipZeroBytes];
                                }
                                else
                                {
                                    WriteBuffer[j] = (byte)ChangeBuffer[j];
                                }
                            }

                            //Changed it for TNC: https://i.imgur.com/BQckoSl.png
                            int BytesWritten = 0;
                            bool WriteSuccess = WriteProcessMemory(Handle, Region.BaseAddress + i - SkipZeroBytes, WriteBuffer, WriteBuffer.Length, out BytesWritten);
                            FoundAOB = true;
                        }
                    }
                }
                ReadBuffer = null;
            }
            catch (Exception)
            {
                bool written = false;
                int tried = 0;
                while (!written || tried>1000)
                {
                    try
                    {

                        using (System.IO.StreamWriter sw = new System.IO.StreamWriter("error.log", true))
                        {
                            sw.WriteLine(DateTime.Now);
                            sw.WriteLine("RegionAddr::" + Region.BaseAddress);
                            sw.WriteLine("RegionSize::" + Region.RegionSize);
                            sw.WriteLine("Location::" + location);
                        }
                        written = true;
                    }
                    catch { tried++; }
                }
            }
            return FoundAOB;
        }
    }
}
