using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KH_NET_UI.KHScriptMemoryServices
{
    class KHScriptMemoryHelper
    {
        //MBI Consts
        const int MEM_COMMIT = 0x1000;
        const int MEM_FREE = 0x10000;
        const int MEM_RESERVE = 0x2000;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION32 lpBuffer, uint dwLength);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);
        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);
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
        internal static int ThreadsExited = 0;

        private ConcurrentStack<MEMORY_BASIC_INFORMATION_ANY> MemoryBlockStack;
        private List<Thread> ThreadList;
        public List<long> PatternScan(int TargetID, string AOB)
        {
            ThreadsExited = 0;
            ThreadList = new List<Thread>();
            IntPtr TargetHandle = (Process.GetProcessById(TargetID)).Handle;

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

                    VQExRes = VirtualQueryEx(TargetHandle, new IntPtr(CurrentAddress), out mem_basic_info32, structSize);
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

                    VQExRes = VirtualQueryEx(TargetHandle, new IntPtr(CurrentAddress), out mem_basic_info64, structSize);
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
            List<long> AddressResults = new List<long>();

            for (int i = 0; i < LP; i++)
            {
                KHScriptPatternScanThread KHPST = new KHScriptPatternScanThread(i, AOB, TargetHandle);
                Thread newThread = new Thread(() => KHPST.StartAOBScan(MemoryBlockStack, ref AddressResults));
                newThread.Name = "MemoryProcessT" + i;
                newThread.Start();
                ThreadList.Add(newThread);
            }
            while (ThreadsExited < LP) { }
            GC.Collect();
            return AddressResults;
        }
        public bool WriteBytes(int PID, long Address, string Bytes)
        {
            try
            {
                IntPtr Handle = (Process.GetProcessById(PID)).Handle;
                if (Handle == null) return false;

                string[] StringBytes = Bytes.Split(' ');
                byte[] BytesToWrite = new byte[StringBytes.Length];
                for (int i = 0; i < BytesToWrite.Length; i++) { BytesToWrite[i] = Convert.ToByte(StringBytes[i], 16); }

                int BytesWritten = 0;
                bool WPM = WriteProcessMemory(Handle, (IntPtr)Address, BytesToWrite, BytesToWrite.Length, out BytesWritten);

                if (BytesWritten == 0 || !WPM) return false;
            } catch (Exception e) { Console.WriteLine(e.Message); return false; }
            return true;
        }
        public bool ReadBytes(int PID, long Address, int Length, out byte[] BytesRead)
        {
            BytesRead = null;
            try
            {
                IntPtr Handle = (Process.GetProcessById(PID)).Handle;
                if (Handle == null && Length == 0) return false;

                byte[] _BytesRead = new byte[Length];
                int NumBytesRead = 0;
                bool RPM = ReadProcessMemory(Handle, (IntPtr)Address, _BytesRead, Length, ref NumBytesRead);

                if (NumBytesRead == 0 || !RPM) return false;

                BytesRead = _BytesRead;
            }
            catch (Exception e) { Console.WriteLine(e.Message); return false; }
            return true;
        }
        internal class KHScriptPatternScanThread
        {
            [DllImport("kernel32.dll")]
            private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

            private int ThreadNum;
            private string AOBToFind;
            IntPtr Handle;

            public KHScriptPatternScanThread(int _ThreadNum, string _AOBToFind, IntPtr _Handle)
            {
                ThreadNum = _ThreadNum;
                AOBToFind = _AOBToFind;
                Handle = _Handle;
            }
            public void StartAOBScan(ConcurrentStack<KHScriptMemoryHelper.MEMORY_BASIC_INFORMATION_ANY> MemoryBlockStack, ref List<long> AddressResults)
            {
                KHScriptMemoryHelper.MEMORY_BASIC_INFORMATION_ANY CurrentRegion;
                while (MemoryBlockStack.TryPop(out CurrentRegion) == true)
                {
                    bool FoundPattern = FindPattern(CurrentRegion, ref AddressResults);
                }
                Interlocked.Add(ref KHScriptMemoryHelper.ThreadsExited, 1);
                Console.WriteLine("Exiting Thread " + ThreadNum);
                GC.Collect();
            }

            private bool FindPattern(KHScriptMemoryHelper.MEMORY_BASIC_INFORMATION_ANY Region, ref List<long> AddressResults)
            {
                byte[] ReadBuffer = new byte[(int)Region.RegionSize];
                int BytesRead = 0;
                bool RPM = ReadProcessMemory(Handle, Region.BaseAddress, ReadBuffer, (int)Region.RegionSize, ref BytesRead);
                bool FindPatternSuccess = FindPattern(ReadBuffer, Region, ref AddressResults);
                ReadBuffer = null;
                return FindPatternSuccess;
            }
            private bool FindPattern(byte[] ReadBuffer, KHScriptMemoryHelper.MEMORY_BASIC_INFORMATION_ANY Region, ref List<long> AddressResults)
            {
                int location = 0;
                bool FoundAOB = false;
                try
                {
                    string[] AOBPartsO = AOBToFind.Split(' ');
                    byte?[] SearchBuffer = new byte?[AOBPartsO.Length];
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
                                lock (AddressResults) { AddressResults.Add((long)Region.BaseAddress + i); }
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
                    catch { }
                }
                return FoundAOB;
            }
        }
    }
}
