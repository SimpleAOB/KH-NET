using KH_NET_UI.MemoryServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KH_NET_UI
{
    /// <summary>
    /// Interaction logic for AOBSelect_ListItem.xaml
    /// </summary>
    public partial class AOBSelect_ListItem : UserControl
    {
        public delegate void AOBError(object sender, AOBErrorArgs e);
        public event AOBError AOBErrorEvent;

        KHScript.KHScript KHS;

        public AOBSelect_ListItem()
        {
            InitializeComponent();
            AOBWorker.WorkerSupportsCancellation = true;
            AOBWorker.DoWork += AOBWorker_DoWork;
            AOBWorker.RunWorkerCompleted += AOBWorker_RunWorkerCompleted;
        }
        public KongHackTrainer.HackJSON HackInfo { get; private set; }
        public bool AOBApplied = false;
        private int PID = -1;

        public void SetContent(KongHackTrainer.HackJSON _HackInfo, int _PID)
        {
            HackInfo = _HackInfo;
            AOB_Title.Content = _HackInfo.hack_title;
            AOB_Votes.Content = _HackInfo.votes;
            string tDesc = _HackInfo.hack_desc;
            if (tDesc.Length > 65)
            {
                tDesc = tDesc.Substring(0, 65) + "...";
            }
            int ind = tDesc.IndexOf("\n");
            if (ind > -1)
            {
                AOB_Desc.Content = tDesc.Substring(0, ind) + "...";
            }
            else
            {
                AOB_Desc.Content = tDesc;
            }
            AOB_Apply_btn.button.Content = "Apply";
            PID = _PID;
            if (HackInfo.script != "")
            {
                KHS = new KHScript.KHScript(PID);
                KHS.AddScript(HackInfo.hid, HackInfo.script);
            }
        }
        BackgroundWorker AOBWorker = new BackgroundWorker();
        public bool ApplyHack(int pid)
        {
            MemoryHelpers MemoryScan;
            MemoryScan = new MemoryHelpers(pid);
            for (int i = 0; i < HackInfo.aob.Length; i++)
            {
                KongHackTrainer.AoBJSON sig = HackInfo.aob[i];
                bool res = MemoryScan.PatternScanAOB(sig.B, sig.A);
                bool foundStatus = MemoryScan.GetAOBFoundStatus();
                if (!res || !foundStatus)
                {
                    return false;
                }
            }
            return true;
        }
        private void CancelAOBWorkerSafely()
        {
            //MemoryScan.AbortScan(false);
            AOBWorker.CancelAsync();
        }

        private void AOBWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                AOB_Apply_btn.button.Content = "Apply";
                AOB_Apply_btn.ToolTip = null;
            }
            else
            {
                if ((bool)e.Result == true)
                {
                    AOBApplied = true;
                    AOB_Apply_btn.IsEnabled = false;
                    AOB_Apply_btn.button.Content = "Applied";
                    AOB_Apply_btn.ToolTip = "Hack already applied";
                    AOB_Apply_btn.button.Style = (Style)AOB_Apply_btn.Resources["KH_Btn_Style_Sucess"];
                    var a = AOB_Apply_btn.Resources;
#if !DEBUG
                    KongHackTrainer.KongHack.UpVoteRequest(HackInfo.hid);
#endif
                }
                else if ((bool)e.Result == false)
                {
                    AOB_Apply_btn.button.Content = "Failed";
                    AOB_Apply_btn.ToolTip = null;
                    AOB_Apply_btn.button.Style = (Style)AOB_Apply_btn.Resources["KH_Btn_Style_Danger"];
                    AOBErrorEvent(this, new AOBErrorArgs(2, "I couldn't find this AOB! Looks like the hack doesn't work anymore or the wrong process is selected."));
                }
            }
        }

        private void AOBWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = ApplyHack((int)e.Argument);
        }

        private void AOB_Apply_btn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (PID == -1)
            {
                AOBErrorEvent(this, new AOBErrorArgs(1, "No Process is selected"));
                return;
            }
            if (HackInfo.aob != null)
            {
                if (AOBWorker.CancellationPending) return;
                if (AOBWorker.IsBusy) { CancelAOBWorkerSafely(); return; }
                AOB_Apply_btn.button.Content = "Cancel";
                AOB_Apply_btn.ToolTip = "Click to cancel the hack application process";
                AOBWorker.RunWorkerAsync(PID);
            } else if (HackInfo.script != "")
            {
                if (KHS.CheckEnableStatus(HackInfo.hid))
                    KHS.DoDisable(HackInfo.hid);
                else
                    KHS.DoEnable(HackInfo.hid);
            }
        }
    }
    public class AOBErrorArgs : EventArgs
    {
        public string Message { get; private set; }
        public int Code { get; private set; }
        public AOBErrorArgs(int _code, string _message)
        {
            Message = _message;
            Code = _code;
        }
    }
}
