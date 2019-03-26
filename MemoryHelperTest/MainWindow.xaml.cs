using KH_NET_UI.MemoryServices;
using System;
using System.Collections.Generic;
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

namespace MemoryHelperTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            label.Content = Environment.Is64BitProcess ? "64bit" : "32bit";
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
                System.Diagnostics.Stopwatch _Timer = new System.Diagnostics.Stopwatch();
                _Timer.Start();
                MemoryHelpers MH64 = new MemoryHelpers(Convert.ToInt32(textBox1.Text.Trim()));
                bool Result = MH64.PatternScanAOB(textBox.Text.Trim(), textBox_Copy.Text.Trim());
                _Timer.Stop();
                MessageBox.Show(string.Format("Scanner finished with state \"{0}\" in {1}ms", Result, _Timer.ElapsedMilliseconds));
        }
    }
}
