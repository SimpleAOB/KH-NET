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
    /// Interaction logic for MenuItem.xaml
    /// </summary>
    public partial class MenuItem : UserControl
    {
        public delegate void MenuItemClickHandler(object sender, MenuItemClickHandlerEventArgs e);
        public event MenuItemClickHandler Clicked;

        public MenuItem()
        {
            InitializeComponent();
        }

        public void SetContent(string MenuText)
        {
            //this.Item_Glyph.Source = new BitmapImage(new Uri(ImagePath, UriKind.Relative));
            this.Item_Text.Content = MenuText;
            this.Item_Text.FontWeight = FontWeights.Normal;
        }
        public void SetContent(string MenuText, bool active)
        {
            this.Item_Text.Content = MenuText;
            if (active) this.Item_Text.FontWeight = FontWeights.Bold;
        }

        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Background = (Brush)(new BrushConverter().ConvertFrom("#dcdcdc"));
        }

        private void MenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Background = (Brush)(new BrushConverter().ConvertFrom("#f5f5f5"));
        }

        private void MenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Background = (Brush)(new BrushConverter().ConvertFrom("#c4c4c4"));
        }

        private void MenuItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Background = (Brush)(new BrushConverter().ConvertFrom("#dcdcdc"));
            Clicked(this, new MenuItemClickHandlerEventArgs());
        }
    }
    public class MenuItemClickHandlerEventArgs : EventArgs
    {
        public bool Manual { get; private set; }
        public string Location { get; private set; }
        public MenuItemClickHandlerEventArgs() { }
        public MenuItemClickHandlerEventArgs(bool _manual, string _location)
        {
            Manual = _manual;
            Location = _location;
        }
    }
}
