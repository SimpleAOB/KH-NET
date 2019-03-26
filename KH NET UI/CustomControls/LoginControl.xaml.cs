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
    /// Interaction logic for LoginControl.xaml
    /// </summary>
    public partial class LoginControl : UserControl
    {
        public delegate void LoginHandler(object sender, LoginHandlerEventArgs e);
        public event LoginHandler LoginButtonPressed;
        
        public LoginControl()
        {
            InitializeComponent();
            Login_btn.button.Content = "Log In";
        }

        private void Username_tb_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Username_lbl.Visibility == Visibility.Visible) Username_lbl.Visibility = Visibility.Hidden;
        }

        private void Username_tb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (((TextBox)Textbox.Content).Text.Length == 0) Username_lbl.Visibility = Visibility.Visible;
        }
        private void Password_tb_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Password_lbl.Visibility == Visibility.Visible) Password_lbl.Visibility = Visibility.Hidden;
        }
        private void Password_tb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (((PasswordBox)Password_tb.Content).Password.Length == 0) Password_lbl.Visibility = Visibility.Visible;
        }

        private void Login_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            TryLogin();
        }
        private void TryLogin()
        {
            string username = Textbox.Textbox.Text;
            string password = Password_tb.passwordBox.Password;
            if (username.Length != 0 && password.Length != 0)
            {
                //RunLoginWorker(username, password);
                LoadingGrid.Visibility = Visibility.Visible;
                LoginButtonPressed(this, new LoginHandlerEventArgs(username, password));
            } else
            {
                //Display some type of error message
            }
        }

        private void TB_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryLogin();
            } if (Password_tb.passwordBox.Password.Length > 0 && Textbox.Textbox.Text.Length > 0)
            {
                Login_btn.IsEnabled = true;
            } else
            {
                Login_btn.IsEnabled = false;
            }
        }
        public void ResetControl()
        {
            Login_btn.IsEnabled = true;
            Textbox.IsEnabled = true;
            Textbox.Textbox.Text = "";
            Password_tb.IsEnabled = true;
            Password_tb.passwordBox.Password = "";
            LoadingGrid.Visibility = Visibility.Hidden;
            if (Textbox.Textbox.IsFocused)
            {
                Username_lbl.Visibility = Visibility.Visible;
            } else
            {
                Username_lbl.Visibility = Visibility.Hidden;
            }
            if (Password_tb.passwordBox.IsFocused)
            {
                Password_lbl.Visibility = Visibility.Visible;
            }
            else
            {
                Password_lbl.Visibility = Visibility.Hidden;
            }
        }
        public void ResetIncorrectPassword()
        {
            Login_btn.IsEnabled = true;
            Textbox.IsEnabled = true;
            Password_tb.IsEnabled = true;
            Password_tb.passwordBox.Password = "";
            LoadingGrid.Visibility = Visibility.Hidden;
            Password_lbl.Visibility = Visibility.Visible;
            if (Textbox.Textbox.IsFocused)
            {
                Username_lbl.Visibility = Visibility.Visible;
            }
            else
            {
                Username_lbl.Visibility = Visibility.Hidden;
            }
            if (Password_tb.passwordBox.IsFocused)
            {
                Password_lbl.Visibility = Visibility.Visible;
            }
            else
            {
                Password_lbl.Visibility = Visibility.Hidden;
            }
        }
    }
    public class LoginHandlerEventArgs : EventArgs
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public LoginHandlerEventArgs(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
