using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// Interaction logic for ProcessSelectControl.xaml
    /// </summary>
    public partial class ProcessSelectControl : UserControl
    {
        public delegate void RequestSwitchHandler(object sender, RequestSwitchArgs e);
        public event RequestSwitchHandler RequestSwitchEvent;

        public ProcessSelectControl()
        {
            InitializeComponent();
        }

        Process[] LoadedProccesses;
        public int SelectedPid = -1;
        public string SelectedPidName = "";
        List<object[]> OriginalItems = new List<object[]>();
        List<object[]> CurrentDisplayItems = new List<object[]>();
        public void UpdateFromList(List<object[]> items, bool FromSameClass = false)
        {
            try
            {
                if (!FromSameClass) { OriginalItems = items; }
                CurrentDisplayItems = items;

                Process_lb.Items.Clear();
                LoadedProccesses = new Process[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    ProcessItem s = (ProcessItem)items[i][0];
                    Process p = (Process)items[i][1];
                    ProcessSelect_ProcessItem PS_Item = new ProcessSelect_ProcessItem();
                    PS_Item.SetContent(s.ProcessIcon, s.ProcessName, s.ProcessID_Hex, s.ProcessID_Int);
                    Process_lb.Items.Add(PS_Item);
                    LoadedProccesses[i] = p;
                }
                Process_lb.ScrollIntoView(Process_lb.Items[0]);
            } catch (Exception e)
            {
                using (StreamWriter sw = new StreamWriter("error.log", true))
                {
                    sw.WriteLine(e.Message);
                    sw.WriteLine(e.StackTrace);
                }
            }
        }
        private void Process_lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Process_lb.Items.Count <= 0) return;
                ProcessSelect_ProcessItem ModifiedItem = ((ProcessSelect_ProcessItem)e.AddedItems[0]);
                ModifiedItem.ProccessItem_Grid.Background = (System.Windows.Media.Brush)(new BrushConverter()).ConvertFromString("#dadfe1");
                e.AddedItems[0] = ModifiedItem;
                SelectedPid = ModifiedItem.ProcessID;
                SelectedPidName = ModifiedItem.ProcessName;
                if (e.RemovedItems.Count > 0) e.RemovedItems[0] = (((ProcessSelect_ProcessItem)e.RemovedItems[0]).ProccessItem_Grid.Background = System.Windows.Media.Brushes.Transparent);
            } catch (Exception ex)
            {
                using (StreamWriter sw = new StreamWriter("error.log", true))
                {
                    sw.WriteLine(ex.Message);
                    sw.WriteLine(ex.StackTrace);
                }
            }
        }
        
        private void Process_lb_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RequestSwitchEvent(this, new RequestSwitchArgs("gamesearch"));
        }

        private void ProcessName_lbl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ProcessFilter_Name_Grid.Visibility == Visibility.Hidden)
            {
                ProcessFilter_Name_Grid.Visibility = Visibility.Visible;
                ProcessFilter_Name_tb.Textbox.Focus();
            }
        }

        private void ProcessFilter_Name_tb_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessFilter_Name_Grid.Visibility = Visibility.Hidden;
        }

        private void ProcessFilter_Name_tb_KeyUp(object sender, KeyEventArgs e)
        {
            FilterFromList(ProcessFilter_Name_tb.Textbox.Text, ProcessFilter_HexID_tb.Textbox.Text, ProcessFilter_IntID_tb.Textbox.Text);
        }

        private void ProcessFilter_HexID_tb_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessFilter_HexID_Grid.Visibility = Visibility.Hidden;
        }

        private void ProcessFilter_HexID_tb_KeyUp(object sender, KeyEventArgs e)
        {
            FilterFromList(ProcessFilter_Name_tb.Textbox.Text, ProcessFilter_HexID_tb.Textbox.Text, ProcessFilter_IntID_tb.Textbox.Text);
        }

        private void ProcessID_hex_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ProcessFilter_HexID_Grid.Visibility == Visibility.Hidden)
            {
                ProcessFilter_HexID_Grid.Visibility = Visibility.Visible;
                ProcessFilter_HexID_tb.Textbox.Focus();
            }
        }
        private void FilterFromList(string name = "", string hex = "", string id = "")
        {
            List<object[]> FilteredList = new List<object[]>();
            foreach (object[] o in OriginalItems)
            {
                ProcessItem p = (ProcessItem)(o[0]);
                bool AddToList = true;
                if (name.Length > 0)
                {
                    if (!p.ProcessName.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        AddToList = false;
                    }
                }
                if (hex.Length > 0)
                {
                    if (!p.ProcessID_Hex.ToLowerInvariant().Contains(hex.ToLowerInvariant()))
                    {
                        AddToList = false;
                    }
                }
                if (id.Length > 0)
                {
                    if (!p.ProcessID_Int.ToString().ToLowerInvariant().Contains(id.ToLowerInvariant()))
                    {
                        AddToList = false;
                    }
                }
                if (AddToList)
                {
                    FilteredList.Add(o);
                }
            }
            UpdateFromList(FilteredList, true);
        }

        private void ProcessID_int_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ProcessFilter_IntID_Grid.Visibility == Visibility.Hidden)
            {
                ProcessFilter_IntID_Grid.Visibility = Visibility.Visible;
                ProcessFilter_IntID_tb.Textbox.Focus();
            }
        }

        private void ProcessFilter_IntID_tb_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessFilter_IntID_Grid.Visibility = Visibility.Hidden;
        }

        private void ProcessFilter_IntID_tb_KeyUp(object sender, KeyEventArgs e)
        {
            FilterFromList(ProcessFilter_Name_tb.Textbox.Text, ProcessFilter_HexID_tb.Textbox.Text, ProcessFilter_IntID_tb.Textbox.Text);
        }

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (((System.Windows.Shapes.Rectangle)sender) == ProcessName_Rect)
            {
                ProcessName_lbl.Opacity = 0.6;
            } else if (((System.Windows.Shapes.Rectangle)sender) == ProcessID_Hex_Rect)
            {
                ProcessID_Hex_lbl.Opacity = 0.6;
            } else if (((System.Windows.Shapes.Rectangle)sender) == ProcessID_Int_Rect)
            {
                ProcessID_Int_lbl.Opacity = 0.6;
            }
        }

        private void Rectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            if (((System.Windows.Shapes.Rectangle)sender) == ProcessName_Rect)
            {
                ProcessName_lbl.Opacity = 1;
            }
            else if (((System.Windows.Shapes.Rectangle)sender) == ProcessID_Hex_Rect)
            {
                ProcessID_Hex_lbl.Opacity = 1;
            }
            else if (((System.Windows.Shapes.Rectangle)sender) == ProcessID_Int_Rect)
            {
                ProcessID_Int_lbl.Opacity = 1;
            }
        }
    }
}
