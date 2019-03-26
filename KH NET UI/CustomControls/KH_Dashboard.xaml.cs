using KongHackTrainer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace KH_NET_UI
{
    /// <summary>
    /// Interaction logic for KH_Dashboard.xaml
    /// </summary>
    public partial class KH_Dashboard : Window
    {
        ProcessSelectControl ProcessSelect = new ProcessSelectControl();
        GameSelectControl GameSelect = new GameSelectControl();
        SettingSelectControl SettingSelect = new SettingSelectControl();
        AOBSelect AOBSelect = new AOBSelect();
        AOBSelect LastAOBSelect = null;
        MenuItem ProcessMenuItem = new MenuItem();
        MenuItem GameSearchMenuItem = new MenuItem();
        MenuItem AOBsMenuItem = new MenuItem();
        MenuItem SettingsMenuItem = new MenuItem();
        MenuItem LogoutMenuItem = new MenuItem();

        BackgroundWorker LoadProcessWorker = new BackgroundWorker();
        BackgroundWorker LoadAOBsWorker = new BackgroundWorker();

        Timer KHKeepAliveTimer = new Timer();

        int StartupGame = -1;

        public KH_Dashboard()
        {
            InitializeComponent();
        }
        public KH_Dashboard(string _StartupGame)
        {
            InitializeComponent();
            if (_StartupGame != null)
            {
                StartupGame = Convert.ToInt32((_StartupGame.Split(':'))[1]);
                MessageBox.Show("1"+_StartupGame.ToString());
                MessageBox.Show("2"+StartupGame.ToString());
            }
        }

        bool UserWantsLogout = false;
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!UserWantsLogout) Environment.Exit(0);
        }
        const string ProcessMenuItemName = "Process Select";
        const string GameMenuItemName = "Game Search";
        const string AOBMenuItemName = "Array of Bytes (AOB)";
        const string SettingsMenuItemName = "Settings";
        const string LogoutMenuItemName = "Logout";
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProcessSelect.Name = "ProcessSelectControl";

            ProcessMenuItem.SetContent(ProcessMenuItemName);
            ProcessMenuItem.Name = "ProccessSelectMenuItem";
            ProcessMenuItem.Margin = new Thickness(0, 10, 0, 0);
            ProcessMenuItem.Clicked += MenuItem_Clicked;
            GameSearchMenuItem.SetContent(GameMenuItemName);
            GameSearchMenuItem.Name = "GameSearchMenuItem";
            GameSearchMenuItem.Clicked += MenuItem_Clicked;
            AOBsMenuItem.SetContent(AOBMenuItemName);
            AOBsMenuItem.Name = "AOBSelectMenuItem";
            AOBsMenuItem.Clicked += MenuItem_Clicked;
            LogoutMenuItem.SetContent("Logout");
            LogoutMenuItem.Name = "LogoutMenuItem";
            LogoutMenuItem.Clicked += MenuItem_Clicked;
            SettingsMenuItem.SetContent("Settings");
            SettingsMenuItem.Name = "SettingsMenuItem";
            SettingsMenuItem.Clicked += MenuItem_Clicked;
            this.Menu_Bottom_Left.Children.Add(LogoutMenuItem);
            this.Menu_Bottom_Left.Children.Add(SettingsMenuItem);
            this.Menu_Left.Children.Add(ProcessMenuItem);
            this.Menu_Left.Children.Add(GameSearchMenuItem);
            this.Menu_Left.Children.Add(AOBsMenuItem);
            ProcessSelect.Margin = new Thickness(150, 50, 0, 0);
            ProcessSelect.Height = 650;
            ProcessSelect.RequestSwitchEvent += PageSwitchRequest;
            GameSelect.Margin = new Thickness(150, 50, 0, 0);
            GameSelect.Height = 650;
            GameSelect.RequestSwitchEvent += PageSwitchRequest;
            AOBSelect.Margin = new Thickness(150, 50, 0, 0);
            AOBSelect.Height = 650;
            AOBSelect.AOBErrorEvent += AOBSelect_AOBErrorEvent;
            SettingSelect.Margin = new Thickness(150, 50, 0, 0);
            SettingSelect.Height = 650;

            LoadProcessWorker.DoWork += LoadProcessWorker_DoWork;
            LoadProcessWorker.RunWorkerCompleted += LoadProcessWorker_RunWorkerCompleted;
            LoadAOBsWorker.DoWork += LoadAOBsWorker_DoWork;
            LoadAOBsWorker.RunWorkerCompleted += LoadAOBsWorker_RunWorkerCompleted;

            WelcomeMessageTop.Content = "Welcome " + KHUserInfo.Username.Replace("_","__") + "!";
            this.Title += (Environment.Is64BitProcess ? " - 64bit" : " - 32bit");

            if (StartupGame != -1)
            {
                MenuItem_Clicked(this, new MenuItemClickHandlerEventArgs(true, "ToAOBs"));
            } else
            {
                MessageBox.Show("3"+StartupGame.ToString());
            }

            KHKeepAliveTimer.Interval = 300000;
            KHKeepAliveTimer.Elapsed += KHKeepAliveTimer_Elapsed;
            KHKeepAliveTimer.Enabled = true;
        }

        private void KHKeepAliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string m = KongHack.KeepAliveRequest().ToLower();
            switch (m)
            {
                case "false":
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        ModalDisplay.Show("KH NET Session Expired", "Your KongHack Trainer session has expired. Please exit and log in again", false);
                        Properties.Settings.Default.KH_AutoRelog = false;
                        Properties.Settings.Default.Save();
                        KHKeepAliveTimer.Enabled = false;
                    }));
                    break;
                case "true":
                    break;
                default:
                    throw new Exception("Unhandled reponse from KeepAliveRequest");
            }
        }

        private void AOBSelect_AOBErrorEvent(object sender, AOBErrorArgs e)
        {
            ModalDisplay.Show("AOB Selection Error", e.Message);
        }

        private void PageSwitchRequest(object sender, RequestSwitchArgs e)
        {
            if (e.Location == "aob")
            {
                MenuItem_Clicked(GameSearchMenuItem, new MenuItemClickHandlerEventArgs(true, "ToAOBs"));
                SetSelectedMenuItem(AOBsMenuItem);
            } else if (e.Location == "gamesearch")
            {
                MenuItem_Clicked(ProcessMenuItem, new MenuItemClickHandlerEventArgs(true, "ToGameSearch"));
                SetSelectedMenuItem(GameSearchMenuItem);
            }
        }

        private void RemoveActivePanels()
        {
            for (var i = 0; i < Dashboard_Grid.Children.Count; i++)
            {
                if (Dashboard_Grid.Children[i].GetType() == typeof(ProcessSelectControl))
                {
                    Dashboard_Grid.Children.Remove(ProcessSelect);
                } else if (Dashboard_Grid.Children[i].GetType() == typeof(GameSelectControl))
                {
                    Dashboard_Grid.Children.Remove(GameSelect);
                } else if (Dashboard_Grid.Children[i].GetType() == typeof(AOBSelect))
                {
                    LastAOBSelect = AOBSelect;
                    Dashboard_Grid.Children.Remove(AOBSelect);
                } else if (Dashboard_Grid.Children[i].GetType() == typeof(SettingSelectControl))
                {
                    Dashboard_Grid.Children.Remove(SettingSelect);
                }
            }
        }
        private void LoadAOBsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int ManualSelectGameId = GameSelect.SelectedGameID;
            int SelectedGame = -1;
            if (ManualSelectGameId == -1 && StartupGame == -1)
            {
                e.Result = "error:nogameselected";
                return;
            } else if (StartupGame != -1)
            {
                SelectedGame = StartupGame;
                StartupGame = -1;
            } else if (ManualSelectGameId != -1)
            {
                SelectedGame = ManualSelectGameId;
            }
            if (SelectedGame == -1) return;
            BackgroundWorker_Work Work = new BackgroundWorker_Work();
            string AOBString = Work.LoadAOBsWork(SelectedGame);
            e.Result = AOBString;
        }
        private void LoadAOBsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((string)e.Result == "error:nogameselected")
            {
                GifLoadOverlay.Visibility = Visibility.Hidden;
                ModalDisplay.Show("No Game Selected", "Select a game to load AOBs");
                DeselectMenuItems();
                RemoveActivePanels();
                WelcomeMessage.Visibility = Visibility.Visible;
                return;
            }
            if (LastAOBSelect != null)
            {
                if (LastAOBSelect.SelectedGameID == GameSelect.SelectedGameID)
                {
                    ModalDisplay.Show("Previous AOB State", "Would you like to reload your previous AOB application state?", CustomControls.ModalControl.ModalButtons.YESNO, e.Result);
                } else
                {
                    FinishLoadAOBs(false, (string)e.Result);
                }
            } else
            {
                FinishLoadAOBs(false, (string)e.Result);
            }
            
        }
        private void FinishLoadAOBs(bool ReloadLastState, string ResultString = "" )
        {
            if (!ReloadLastState)
            {
                RemoveActivePanels();
                AOBSelect.UpdateFromString(ResultString, GameSelect.SelectedGameID, ProcessSelect.SelectedPid, ProcessSelect.SelectedPidName);
            } else
            {
                AOBSelect = LastAOBSelect;
            }
            Dashboard_Grid.Children.Add(AOBSelect);
            GifLoadOverlay.Visibility = Visibility.Hidden;
        }
        private void LoadProcessWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                RemoveActivePanels();
                ProcessSelect.UpdateFromList((List<object[]>)e.Result);
                Dashboard_Grid.Children.Add(ProcessSelect);
                GifLoadOverlay.Visibility = Visibility.Hidden;
            } catch (Exception ex)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter("khnet-error.log", true))
                {
                    sw.WriteLine(ex.Message);
                    sw.WriteLine(ex.StackTrace);
                }
            }
        }

        private void LoadProcessWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker_Work Work = new BackgroundWorker_Work();
            List<object[]> Items = Work.LoadProcessesWork((bool)e.Argument);
            e.Result = Items;
        }
        private void LoadGameSelect()
        {
            RemoveActivePanels();
            Dashboard_Grid.Children.Add(GameSelect);
            GifLoadOverlay.Visibility = Visibility.Hidden;
        }
        private void MenuItem_Clicked(object sender, MenuItemClickHandlerEventArgs e)
        {
            if (WelcomeMessage.Visibility == Visibility.Visible)
            {
                WelcomeMessage.Visibility = Visibility.Hidden;
            }
            GifLoadOverlay.Visibility = Visibility.Visible;
            if (e.Manual)
            {
                switch (e.Location)
                {
                    case "ToAOBs":
                        if (LoadAOBsWorker.IsBusy) return;
                        LoadAOBsWorker.RunWorkerAsync();
                        break;
                    case "ToGameSearch":
                        LoadGameSelect();
                        break;
                }
            } else
            {
                MenuItem s = (MenuItem)sender;
                switch (s.Name)
                {
                    case "ProccessSelectMenuItem":
                        if (LoadProcessWorker.IsBusy) return;
                        LoadProcessWorker.RunWorkerAsync(true);
                        break;
                    case "GameSearchMenuItem":
                        LoadGameSelect();
                        break;
                    case "AOBSelectMenuItem":
                        if (LoadAOBsWorker.IsBusy) return;
                        LoadAOBsWorker.RunWorkerAsync();
                        break;
                    case "SettingsMenuItem":
                        LoadSettingsSelect();
                        break;
                    case "LogoutMenuItem":
                        Properties.Settings.Default.KH_AutoRelog = false;
                        Properties.Settings.Default.Save();
                        KHKeepAliveTimer.Enabled = false;
                        UserWantsLogout = true;
                        this.Close();
                        break;
                    default:
                        break;
                }
                SetSelectedMenuItem(s);
            }
        }
        private void LoadSettingsSelect()
        {
            RemoveActivePanels();
            Dashboard_Grid.Children.Add(SettingSelect);
            GifLoadOverlay.Visibility = Visibility.Hidden;
        }
        private void SetSelectedMenuItem(MenuItem s)
        {
            ProcessMenuItem.SetContent(ProcessMenuItemName);
            GameSearchMenuItem.SetContent(GameMenuItemName);
            AOBsMenuItem.SetContent(AOBMenuItemName);
            SettingsMenuItem.SetContent(SettingsMenuItemName);

            switch (s.Name)
            {
                case "ProccessSelectMenuItem":
                    ProcessMenuItem.SetContent(ProcessMenuItemName, true);
                    break;
                case "GameSearchMenuItem":
                    GameSearchMenuItem.SetContent(GameMenuItemName, true);
                    break;
                case "AOBSelectMenuItem":
                    AOBsMenuItem.SetContent(AOBMenuItemName, true);
                    break;
                case "SettingsMenuItem":
                    SettingsMenuItem.SetContent(SettingsMenuItemName, true);
                    break;
            }
        }
        private void DeselectMenuItems()
        {
            ProcessMenuItem.SetContent(ProcessMenuItemName);
            GameSearchMenuItem.SetContent(GameMenuItemName);
            AOBsMenuItem.SetContent(AOBMenuItemName);
            SettingsMenuItem.SetContent(SettingsMenuItemName);
        }
        private void ModalDisplay_OnShow(object sender, GenericEventArgs e)
        {
            ModalDisplay.Visibility = Visibility.Visible;
        }
        private void ModalDisplay_OnClose(object sender, CustomControls.ModalEventArgs e)
        {
            if (e.Response == CustomControls.ModalEventArgs.ModalResponse.M_YES)
            {
                FinishLoadAOBs(true);
            } else if (e.Response == CustomControls.ModalEventArgs.ModalResponse.M_NO)
            {
                FinishLoadAOBs(false, (string)e.Data);
            } else if (e.Response == CustomControls.ModalEventArgs.ModalResponse.M_CLOSE)
            {

            }
            ModalDisplay.Visibility = Visibility.Hidden;
        }
        private void Logo_label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://konghack.com");
        }

        
    }
    public class RequestSwitchArgs : EventArgs
    {
        public string Location { private set; get; }
        public RequestSwitchArgs(string _location)
        {
            Location = _location;
        }
    }
}
