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
    /// Interaction logic for GameSelect_GameItem.xaml
    /// </summary>
    public partial class GameSelect_GameItem : UserControl
    {
        public GameSelect_GameItem()
        {
            InitializeComponent();
        }
        public int GameID { get; private set; }
        public void SetContent(string gname, string gdesc, int gid)
        {
            Game_Name.Content = gname;
            Game_Desc.Content = gdesc;
            GameID = gid;
        }
    }
}
