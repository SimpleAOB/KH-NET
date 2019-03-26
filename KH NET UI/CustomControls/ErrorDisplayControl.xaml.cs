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
    /// Interaction logic for ErrorDisplayControl.xaml
    /// </summary>

    
    public partial class ErrorDisplayControl : UserControl
    {
        public event EventHandler<GenericEventArgs> OnClose;
        public event EventHandler<GenericEventArgs> OnShow;

        public class ModalStyle
        {
            public Brush Success_Background = (Brush)(new BrushConverter().ConvertFromString("#dff0d8"));
            public Brush Success_Border = (Brush)(new BrushConverter().ConvertFromString("#d0e9c6"));
            public Brush Success_FontColor = (Brush)(new BrushConverter().ConvertFromString("#3c763d"));
            public Brush Danger_Background = (Brush)(new BrushConverter().ConvertFromString("#f2dede"));
            public Brush Danger_Border = (Brush)(new BrushConverter().ConvertFromString("#ebcccc"));
            public Brush Danger_FontColor = (Brush)(new BrushConverter().ConvertFromString("#873634"));
        }
        public enum ModalStyles {Success, Danger}

        public ErrorDisplayControl()
        {
            InitializeComponent();
        }
        bool AllowExit = false;
        public void Show(string message, ModalStyles ModalStyle, bool _AllowExit = true)
        {
            if (ModalStyle == ModalStyles.Success)
            {
                ModalStyle m = new ModalStyle();
                Error_Background.Background = m.Success_Background;
                Error_Background.BorderBrush = m.Success_Border;
                Error_Label.Foreground = m.Success_FontColor;
            } else if (ModalStyle == ModalStyles.Danger)
            {
                ModalStyle m = new ModalStyle();
                Error_Background.Background = m.Danger_Background;
                Error_Background.BorderBrush = m.Danger_Border;
                Error_Label.Foreground = m.Danger_FontColor;
            }
            Error_Label.Content = message;
            FormattedText formattedText = new FormattedText(
                (string)Error_Label.Content,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(Error_Label.FontFamily, Error_Label.FontStyle, Error_Label.FontWeight, Error_Label.FontStretch),
                Error_Label.FontSize,
                Brushes.Black);

            Size LabelSize = new Size((int)formattedText.Width, (int)formattedText.Height);

            Error_Grid.Width = LabelSize.Width + 25;
            AllowExit = _AllowExit;
            if (!AllowExit)
            {
                Error_Rectangle.Cursor = Cursors.Arrow;
            } else
            {
                Error_Rectangle.Cursor = Cursors.Hand;
            }
            OnShow(this, new GenericEventArgs());
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AllowExit)
            {
                OnClose(this, new GenericEventArgs());
            }
            else return;
        }
    }
    public class GenericEventArgs : EventArgs
    {
        public GenericEventArgs() { }
    }
}
