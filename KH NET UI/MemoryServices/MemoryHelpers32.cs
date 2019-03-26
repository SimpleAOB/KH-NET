using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace KH_NET_UI.MemoryServices
{
    public class MemoryHelpers
    {
        // REQUIRED CONSTS
        const int MEM_COMMIT = 0x00001000;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION32 lpBuffer, uint dwLength);
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

        private Process Target;
        internal static int ThreadsExited = 0;
        private string LastScanError = "None";

        public MemoryHelpers(int id)
        {
            this.Target = GetProcess(id);
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
        private ConcurrentStack<MEMORY_BASIC_INFORMATION32> MemoryBlockStack;
        private List<Thread> ThreadList = new List<Thread>();
        public bool PatternScanAOB(string originalSig, string changedSig)
        {
            if (Target == null)
            {
                LastScanError = "Specified target not running";
                return false;
            }
            try
            {
                List<MEMORY_BASIC_INFORMATION32> MemoryBlockList = new List<MEMORY_BASIC_INFORMATION32>();
                int CurrentAddress = 0;

                MEMORY_BASIC_INFORMATION32 mem_basic_info = new MEMORY_BASIC_INFORMATION32();
                uint structSize = (uint)Marshal.SizeOf(mem_basic_info);
                while (VirtualQueryEx(Target.Handle, new IntPtr(CurrentAddress), out mem_basic_info, structSize) != 0)
                {
                    if (mem_basic_info.State == MEM_COMMIT) MemoryBlockList.Add(mem_basic_info);
                    CurrentAddress += (int)mem_basic_info.RegionSize;
                }
                MemoryBlockStack = new ConcurrentStack<MEMORY_BASIC_INFORMATION32>(MemoryBlockList);
                //Spawn threads
                int LP = Environment.ProcessorCount;

                for (int i = 0; i < LP; i++)
                {
                    PatternScanThreads pst = new PatternScanThreads(i, originalSig, changedSig, Target.Handle);
                    Thread newThread = new Thread(() => pst.StartAOB(MemoryBlockStack));
                    newThread.Name = "MemoryProcessT" + i;
                    newThread.Priority = ThreadPriority.Highest;
                    newThread.Start();
                    ThreadList.Add(newThread);
                }
                while (ThreadsExited < LP) { }
                GC.Collect();
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
        private static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
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
        public void StartAOB(ConcurrentStack<MemoryHelpers.MEMORY_BASIC_INFORMATION32> MemoryBlockStack)
        {
            MemoryHelpers.MEMORY_BASIC_INFORMATION32 CurrentRegion;
            while (MemoryBlockStack.TryPop(out CurrentRegion) == true)
            {
                bool EditedMemory = ModifyProcess(CurrentRegion);
            }
            Interlocked.Add(ref MemoryHelpers.ThreadsExited, 1);
            Console.WriteLine("Exiting Thread " + ThreadNum);
        }

        private bool ModifyProcess(MemoryHelpers.MEMORY_BASIC_INFORMATION32 Region)
        {
            byte[] ReadBuffer = new byte[(int)Region.RegionSize];
            int BytesRead = 0;
            bool RPM = ReadProcessMemory((int)Handle, (int)Region.BaseAddress, ReadBuffer, (int)Region.RegionSize, ref BytesRead);
            bool FindPatternSuccess = FindPattern(ReadBuffer, Region);
            ReadBuffer = null;
            return RPM;
        }
        private bool FindPattern(byte[] ReadBuffer, MemoryHelpers.MEMORY_BASIC_INFORMATION32 Region)
        {
            string[] AOBPartsO = SigOriginal.Split(' ');
            byte?[] SearchBuffer = new byte?[AOBPartsO.Length];
            byte[] WriteBuffer = new byte[AOBPartsO.Length];
            int SkipZeroBytes = 0;
            bool IncrementZero = true;
            for (int i = 0; i < SearchBuffer.Length; i++)
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
                        Console.WriteLine("Found AOB at " + i);
                        string[] AOBPartsC = SigChanged.Split(' ');
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
                        bool WriteSuccess = WriteProcessMemory(Handle, (IntPtr)Region.BaseAddress + i - SkipZeroBytes, WriteBuffer, WriteBuffer.Length, out BytesWritten);
                    }
                }
            }
            ReadBuffer = null;
            return true;
        }
    }
}
