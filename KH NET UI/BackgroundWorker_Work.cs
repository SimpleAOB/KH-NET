using KongHackTrainer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KH_NET_UI
{
    class BackgroundWorker_Work
    {
        public BackgroundWorker_Work()
        {

        }
        public string LoadAOBsWork(int gameid)
        {
            string res = KongHack.HackRequestByID(gameid);
            return res;
        }
        public bool LoginWork(string u, string p)
        {
            bool s = KongHack.LoginRequest(u,p);
            return s;
        }
        public string GameSearchWork(string searchterm)
        {
            string res = KongHack.GameSearchRequest(searchterm);
            return res;
        }
        public List<object[]> LoadProcessesWork(bool adv)
        {
            Process[] ProcList = Process.GetProcesses().OrderBy(p => p.Id).Reverse().ToArray();
            List<object[]> Items = new List<object[]>();
            Process[] TList = new Process[ProcList.Length];
            Process[] ProcessFilteredList;
            if (!adv)
            {
                int filtercount = 0;
                for (var i = 0; i < ProcList.Length; i++)
                {
                    if (ProcList[i].ProcessName == "chrome")
                    {
                        TList[filtercount] = ProcList[i];
                        filtercount++;
                    }
                    else if (ProcList[i].ProcessName == "firefox")
                    {
                        TList[filtercount] = ProcList[i];
                        filtercount++;
                    }
                    else if (ProcList[i].ProcessName == "plugin-container")
                    {
                        TList[filtercount] = ProcList[i];
                        filtercount++;
                    }
                    else if (ProcList[i].ProcessName.ToLowerInvariant().Contains("flashplayerplugin"))
                    {
                        TList[filtercount] = ProcList[i];
                        filtercount++;
                    }
                }
                ProcessFilteredList = new Process[filtercount];
                for (var i = 0; i < filtercount; i++)
                {
                    ProcessFilteredList[i] = TList[i];
                }
                for (var i = 0; i < ProcessFilteredList.Length; i++)
                {
                    Process p = ProcessFilteredList[i];
                    string procname = p.ProcessName + ".exe";
                    string hex = p.Id.ToString("X8");
                    Icon icon = new Icon(Application.GetResourceStream(new Uri("static/EmptyIcon.ico", UriKind.Relative)).Stream);
                    try
                    {
                        icon = Icon.ExtractAssociatedIcon(p.MainModule.FileName);
                    }
                    catch (System.ComponentModel.Win32Exception) { }
                    ProcessItem Item = new ProcessItem(procname, hex, p.Id, icon);
                    Items.Add(new object[] { Item, p });
                }
            }
            else
            {
                for (var i = 0; i < ProcList.Length; i++)
                {
                    Process p = ProcList[i];
                    string procname = p.ProcessName + ".exe";
                    string hex = p.Id.ToString("X8");
                    Icon icon = new Icon(Application.GetResourceStream(new Uri("static/EmptyIcon.ico", UriKind.Relative)).Stream);
                    try
                    {
                        icon = Icon.ExtractAssociatedIcon(p.MainModule.FileName);
                    }
                    catch (System.ComponentModel.Win32Exception) { }
                    catch (System.IO.FileNotFoundException) { }
                    catch (InvalidOperationException) { /*Process has exited*/ }
                    ProcessItem Item = new ProcessItem(procname, hex, p.Id, icon);
                    Items.Add(new object[] { Item, p });
                }
            }
            return Items;
        }
    }
    public struct ProcessItem
    {
        public string ProcessName { get; private set; }
        public string ProcessID_Hex { get; private set; }
        public int ProcessID_Int { get; private set; }
        public Icon ProcessIcon { get; private set; }
        public ProcessItem(string pn, string pidh, int pidi, Icon pi)
        {
            ProcessName = pn;
            ProcessID_Hex = pidh;
            ProcessID_Int = pidi;
            ProcessIcon = pi;
        }
    }
}
