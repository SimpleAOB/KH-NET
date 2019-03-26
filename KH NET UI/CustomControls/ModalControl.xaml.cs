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

namespace KH_NET_UI.CustomControls
{
    /// <summary>
    /// Interaction logic for ModalControl.xaml
    /// </summary>
    public partial class ModalControl : UserControl
    {
        public event EventHandler<ModalEventArgs> OnClose;
        public event EventHandler<GenericEventArgs> OnShow;

        public enum ModalButtons { CLOSE,YESNO }

        public ModalControl()
        {
            InitializeComponent();
            CloseButton.button.Content = "Close";
            YesButton.button.Content = "Yes";
            YesButton.button.Style = (Style)YesButton.Resources["KH_Btn_Style_Sucess"];
            NoButton.button.Content = "No";
            NoButton.button.Style = (Style)NoButton.Resources["KH_Btn_Style_Danger"];
        }
        public void Show(string Caption, string Body)
        {
            Show(Caption, Body, true);
        }
        public void Show(string Caption, string Body, bool CanClose)
        {
            Modal_Title.Content = Caption;
            Modal_Body.Text = Body;
            CloseButton.Visibility = (CanClose ? Visibility.Visible : Visibility.Collapsed);
            YesButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;
            OnShow(this, new GenericEventArgs());
        }
        public void Show(string Caption, string Body, ModalButtons Buttons)
        {
            if (Buttons == ModalButtons.CLOSE)
            {
                Show(Caption, Body);
            } else if (Buttons == ModalButtons.YESNO)
            {
                Modal_Title.Content = Caption;
                Modal_Body.Text = Body;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                CloseButton.Visibility = Visibility.Collapsed;
                OnShow(this, new GenericEventArgs());
            }
        }
        public void Show(string Caption, string Body, ModalButtons Buttons, object _DataToPass)
        {
            if (Buttons == ModalButtons.CLOSE)
            {
                Show(Caption, Body);
            }
            else if (Buttons == ModalButtons.YESNO)
            {
                Modal_Title.Content = Caption;
                Modal_Body.Text = Body;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                CloseButton.Visibility = Visibility.Collapsed;
                DataToPass = _DataToPass;
                OnShow(this, new GenericEventArgs());
            }
        }
        object DataToPass = null;
        private void ExitButton_Handler(object sender, MouseButtonEventArgs e)
        {
            if (((KH_Button)sender) == YesButton)
            {
                if (DataToPass != null)
                {
                    OnClose(this, new ModalEventArgs(ModalEventArgs.ModalResponse.M_YES, DataToPass));
                    DataToPass = null;
                } else
                {
                    OnClose(this, new ModalEventArgs(ModalEventArgs.ModalResponse.M_YES));
                }
            }
            else if (((KH_Button)sender) == NoButton)
            {
                if (DataToPass != null)
                {
                    OnClose(this, new ModalEventArgs(ModalEventArgs.ModalResponse.M_NO, DataToPass));
                    DataToPass = null;
                } else
                {
                    OnClose(this, new ModalEventArgs(ModalEventArgs.ModalResponse.M_NO));
                }
            }
            else if (((KH_Button)sender) == CloseButton)
            {
                OnClose(this, new ModalEventArgs(ModalEventArgs.ModalResponse.M_CLOSE));
            }
        }
    }
    public class ModalEventArgs : EventArgs
    {
        public enum ModalResponse { M_YES, M_NO, M_CLOSE}
        public ModalResponse Response { get; private set; }
        public object Data { get; private set; }
        public ModalEventArgs(ModalResponse _Response, object _Data = null)
        {
            Response = _Response;
            Data = _Data;
        }
    }
}
