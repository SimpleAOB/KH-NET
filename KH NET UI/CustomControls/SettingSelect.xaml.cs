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

namespace KH_NET_UI
{
    /// <summary>
    /// Interaction logic for SettingSelect.xaml
    /// </summary>
    public partial class SettingSelectControl : UserControl
    {
        public SettingSelectControl()
        {
            InitializeComponent();
            if (Properties.Settings.Default.KH_NeverRelog)
            {
                NeverRelog_Setting_Checkbox.IsChecked = false;
            } else
            {
                NeverRelog_Setting_Checkbox.IsChecked = true;
            }
        }

        private void NeverRelog_Setting_Checkbox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (((CheckBox)sender).IsChecked == true)
            {
                Properties.Settings.Default.KH_NeverRelog = false;
            } else
            {
                Properties.Settings.Default.KH_NeverRelog = true;
            }
        }
    }
}
