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
    /// Interaction logic for AOBSelect.xaml
    /// </summary>
    public partial class AOBSelect : UserControl
    {
        public delegate void AOBError(object sender, AOBErrorArgs e);
        public event AOBError AOBErrorEvent;

        MemoryHelpers AOBScan;

        public AOBSelect()
        {
            InitializeComponent();
        }
        int SelectedPid = -1;
        public int SelectedGameID { get; private set; } = -1;
        string PidName = "";
        public void UpdateFromString(string res, int _SelectedGameID, int _SelectedPid, string _pidName)
        {
            try
            {
                KongHackTrainer.HacksJSON hacks = new KongHackTrainer.HacksJSON();
                hacks = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<KongHackTrainer.HacksJSON>(res);
                hacks.aobs = HackSorter(hacks.aobs);
                int AOB_Count = hacks.aobs.Length;
                AOB_lb.Items.Clear();
                if (AOB_Count < 1)
                {
                    ViewHiderBottomText.Content = string.Format("Selected Game: {0}", hacks.game_name);
                    ViewHider.Visibility = Visibility.Visible;
                }
                else
                {
                    ViewHider.Visibility = Visibility.Hidden;
                    for (var i = 0; i < AOB_Count; i++)
                    {
                        AOBSelect_ListItem Item = new AOBSelect_ListItem();
                        Item.SetContent(hacks.aobs[i], _SelectedPid);
                        Item.AOBErrorEvent += AOBErrorHandler;
                        AOB_lb.Items.Add(Item);
                    }
                    SelectedPid = _SelectedPid;
                    PidName = _pidName;
                    if (SelectedPid == -1)
                    {
                        PID_Display_Target.Content = "No Process selected";
                    }
                    else
                    {
                        PID_Display_Target.Content = PidName + string.Format(" - ({0})", SelectedPid);
                    }
                    if (AOB_lb.Items.Count > 0)
                    {
                        AOB_lb.SelectedIndex = 0;
                    }
                    SelectedGameID = _SelectedGameID;
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        private KongHackTrainer.HackJSON[] HackSorter(KongHackTrainer.HackJSON[] aobs)
        {
            return aobs.OrderByDescending(x => x.votes).ToArray();
        }
        private void AOB_lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AOB_lb.Items.Count <= 0) return;
            var ci = (AOBSelect_ListItem)e.AddedItems[0];
            e.AddedItems[0] = ci.ProccessItem_Grid.Background = (Brush)(new BrushConverter()).ConvertFromString("#dadfe1");
            AOB_Display_Name.Content = ci.HackInfo.hack_title;
            AOB_Display_Desc.Text = ci.HackInfo.hack_desc;
            if (e.RemovedItems.Count > 0) e.RemovedItems[0] = (((AOBSelect_ListItem)e.RemovedItems[0]).ProccessItem_Grid.Background = Brushes.Transparent);
        }
        private void AOBErrorHandler(object sender, AOBErrorArgs e)
        {
            AOBErrorEvent(this, e);
        }
    }
}
