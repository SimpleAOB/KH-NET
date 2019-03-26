using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KongHackTrainer;
using System.ComponentModel;

namespace KH_NET_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        LoginControl LoginController = new LoginControl();
        KH_Dashboard DashBoardWindow;

        BackgroundWorker LoginBackground = new BackgroundWorker();
        BackgroundWorker AutoLoginBackground = new BackgroundWorker();
        public LoginWindow()
        {
            InitializeComponent();

            LoginController.HorizontalAlignment = HorizontalAlignment.Left;
            LoginController.VerticalAlignment = VerticalAlignment.Top;
            LoginController.Margin = new Thickness(100, 100, 0, 0);
            LoginController.LoginButtonPressed += HandleLoginPressed;
            MainGrid.Children.Add(LoginController);
            LoginBackground.DoWork += LoginBackground_DoWork;
            LoginBackground.RunWorkerCompleted += LoginBackground_RunWorkerCompleted;
            AutoLoginBackground.DoWork += AutoLoginBackground_DoWork;
            AutoLoginBackground.RunWorkerCompleted += AutoLoginBackground_RunWorkerCompleted;

            this.Title += (Environment.Is64BitProcess ? " - 64bit" : " - 32bit");

            if (Properties.Settings.Default.KH_AutoRelog && Properties.Settings.Default.KHI_Cookies != ""  && !Properties.Settings.Default.KH_NeverRelog)
            {
                this.Hide();
                AutoLoginBackground.RunWorkerAsync();
            }
        }

        private void DashBoardWindow_Closed(object sender, EventArgs e)
        {
            this.Show();
        }

        private void HandleLoginPressed(object sender, LoginHandlerEventArgs e)
        {
           LoginBackground.RunWorkerAsync(new object[] { e.Username, e.Password });
        }
        private void LoginBackground_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker_Work bw = new BackgroundWorker_Work();
            object[] d = (object[])e.Argument;
            bool s = bw.LoginWork((string)d[0], (string)d[1]);
            if (s)
            {
                e.Result = "login:success";
            }
            else
            {
                e.Result = "login:failed";
            }
        }

        private void AutoLoginBackground_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = KongHack.KeepAliveRequest(Properties.Settings.Default.KHI_Cookies);
        }
        private void NewDashboard(string StartupGame = null)
        {
            DashBoardWindow = new KH_Dashboard((StartupGame!=null?StartupGame:null));
            DashBoardWindow.Closed += DashBoardWindow_Closed;
        }
        private void LoginBackground_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            switch ((string)e.Result)
            {
                case "login:success":
                    LoginSuccess(false);
                    break;
                case "login:failed":
                    LoginController.ResetIncorrectPassword();
                    break;
            }
        }
        /// <param name="FromSession">If true, will set KHUserInfo from saved properties</param>
        private void LoginSuccess(bool FromSession)
        {
            if (FromSession)
            {
                KHUserInfo.Username = Properties.Settings.Default.KHI_Username;
                KHUserInfo.Fullname = Properties.Settings.Default.KHI_FullName;
                KHUserInfo.Donation = Properties.Settings.Default.KHI_Donation;
                KHUserInfo.Elite = Properties.Settings.Default.KHI_Elite;
                KHUserInfo.Leethax = Properties.Settings.Default.KHI_Leethax;
                KHUserInfo.Posts = Properties.Settings.Default.KHI_Posts;
                KHUserInfo.Points = Properties.Settings.Default.KHI_Points;
                KHUserInfo.Cookies = Properties.Settings.Default.KHI_Cookies;
            } else
            {
                Properties.Settings.Default.KH_AutoRelog = true;
                Properties.Settings.Default.KHI_Username = KHUserInfo.Username;
                Properties.Settings.Default.KHI_FullName = KHUserInfo.Fullname;
                Properties.Settings.Default.KHI_Donation = KHUserInfo.Donation;
                Properties.Settings.Default.KHI_Elite = KHUserInfo.Elite;
                Properties.Settings.Default.KHI_Leethax = KHUserInfo.Leethax;
                Properties.Settings.Default.KHI_Posts = KHUserInfo.Posts;
                Properties.Settings.Default.KHI_Points = KHUserInfo.Points;
                Properties.Settings.Default.KHI_Cookies = KHUserInfo.Cookies;
                Properties.Settings.Default.Save();
            }
            this.Hide();
            LoginController.ResetControl();

            string[] StartupArgs = Environment.GetCommandLineArgs();
            if (StartupArgs.Length == 1)
            {
                NewDashboard();
            }
            else
            {
                if (StartupArgs[1].IndexOf("konghack:") == 0)
                {
                    NewDashboard(StartupArgs[1]);
                }
            }

            DashBoardWindow.Show();
        }
        private void AutoLoginBackground_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string ret = (string)e.Result;
            if (ret == "false")
            {
                Properties.Settings.Default.KH_AutoRelog = false;
                Properties.Settings.Default.KHI_Cookies = "";
                Properties.Settings.Default.Save();
                this.Show();
            }
            else if (ret == "true")
            {
                LoginSuccess(true);
            }
        }
        private void MainWindow1_Closing(object sender, CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void label_logo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://konghack.com");
        }
    }
}
