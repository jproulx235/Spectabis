using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Cache;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;
using Spectabis_WPF.Domain;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Spectabis_WPF.ViewModels;

namespace Spectabis_WPF.Views
{
    public partial class MainWindow : MetroWindow
    {
        private LibraryViewModel Library { get; set; }

        public static string BaseDirectory = App.BaseDirectory;
    
        //Side panel width value
        public static readonly double PanelWidth = 700;

        //Stopwatch to keep track of playtime
        private Stopwatch SessionPlaytime = new Stopwatch();
        private DispatcherTimer updatePlaytimeUI = new DispatcherTimer();

        public DiscordRPC DiscordRpc;

        public MainWindow()
        {
            InitializeComponent();

            CheckForUpdates();

            //Create resources folder
            Directory.CreateDirectory($"{BaseDirectory}//resources//_temp");

            CatchCommandLineArguments();

            updatePlaytimeUI.Tick += updatePlaytimeUI_Tick;

            //Version
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);

            //Sets nightmode from variable
            new PaletteHelper().SetLightDark(Properties.Settings.Default.NightMode);

			//If emuDir is not set, launch first time setup
	        if (ShouldShowFirstTimeSetup())
		        FirstSetupFrame.Visibility = Visibility.Visible;

			SetPrimary(Properties.Settings.Default.Swatch);

			//Copy spinner.gif to temporary files
			{
				var dir = BaseDirectory + "\\resources\\_temp";
				if (Directory.Exists(dir) == false)
					Directory.CreateDirectory(dir);
				Properties.Resources.spinner.Save(dir + "\\spinner.gif");
			}

            //Open game library page
            Open_Library();

            GameSettings.Width = PanelWidth;

            //Check if it's april fool's day
            if ((DateTime.Now.Month == 4) && (DateTime.Now.Day == 1) && (Properties.Settings.Default.Aprilfooled == false))
            {
                AprilFools_Grid.Visibility = Visibility.Visible;
            }

            DiscordRpc = new DiscordRPC();
            DiscordRpc.UpdatePresence("Menus");
        }

        private void CheckForUpdates()
        {
            Console.WriteLine("Checking for updates...");
            if (Properties.Settings.Default.Checkupdates)
            {
                if (UpdateCheck.isNewUpdate())
                {
                    try
                    {
                        //Push snackbar
                        this.Invoke(new Action(() => PushSnackbar("A new update is available!")));
                    }
                    catch
                    {
                        Console.WriteLine("Couldn't push update notification");
                    }

                }
            }
        }

        private static bool ShouldShowFirstTimeSetup() {
			var checkDir = Properties.Settings.Default.EmuExePath;

			if (string.IsNullOrEmpty(checkDir))
				return true;

			if (File.Exists(checkDir) && checkDir.EndsWith(".exe"))
				return false;

			checkDir = Path.Combine(checkDir, "pcsx2.exe");
			if (File.Exists(checkDir)) {
				Properties.Settings.Default.EmuExePath = checkDir;
				Properties.Settings.Default.Save();
				return false;
			}

			return true;
		}

		//DLLs for console window
		[DllImport("Kernel32")]
        private static extern void AllocConsole();
        [DllImport("Kernel32")]
        private static extern bool FreeConsole();

        //Set primary color scheme
        private void SetPrimary(string swatch)
        {
            Console.WriteLine("Setting PrimaryColor to " + swatch);
            new PaletteHelper().ReplacePrimaryColor(swatch);
        }

        private void CatchCommandLineArguments()
        {
            //Make alist of all arguments
            List<string> arguments = new List<string>(Environment.GetCommandLineArgs());
            bool findProfile = false;

            foreach(string arg in arguments)
            {
                //Open Console Window
                if (arg == "-console")
                {
                    AllocConsole();
                    Thread.Sleep(10);
                    Console.WriteLine("Opening debug console");
                    Console.WriteLine($"Current Build: {Assembly.GetExecutingAssembly().GetName().Version}");
                }
                //Force first time setup
                else if (arg == "-firsttime")
                {
                    Console.WriteLine("Forcing first time setup");
                    FirstSetupFrame.Visibility = Visibility.Visible;
                }
                //Launch a game profile
                else if(arg == "-profile")
                {
                    Console.WriteLine("Looking for a game profile to launch...");
                    findProfile = true;
                }
                //If profile needs to be found, then do that
                else if(findProfile == true)
                {
                    //If there's a game profile with given argument
                    if (Directory.Exists($"{BaseDirectory}resources\\configs\\{arg}"))
                    {
                        //A given game name is valid
                        Console.WriteLine("Launching " + arg);

                        //Launch game
                        Domain.LaunchPCSX2.CreateGameProcess(arg, true);
                    }
                    else
                    {
                        //If game profile does not exist, then don't look for another one
                        Console.WriteLine("Not a valid game profile name!");
                        findProfile = false;
                    }
                }
            }
        }

        //Hides first time setup frame
        public void HideFirsttimeSetup()
        {
            FirstSetupFrame.Visibility = Visibility.Collapsed;
        }

        //Shows & hides overlay, when appropriate
        private void MenuToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if(GameSettings.Visibility == Visibility.Visible)
            {
                MenuToggleButton.IsChecked = false;
                return;
            }
            sideMenu.Visibility = Visibility.Visible;
            Overlay(true);
        }

        private void MenuToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GameSettings.Visibility == Visibility.Visible)
            {
                MenuToggleButton.IsChecked = false;
                return;
            }
            sideMenu.Visibility = Visibility.Collapsed;
            Overlay(false);
        }

        //Show or hide black overlay
        public void Overlay(bool _show)
        {
            if (_show == true)
            {
                overlay.Opacity = .5;
                overlay.IsEnabled = true;
                overlay.IsHitTestVisible = true;
            }
            else
            {
                overlay.Opacity = 0;
                overlay.IsEnabled = false;
                overlay.IsHitTestVisible = false;
                MenuToggleButton.IsChecked = false;
            }
        }

        //Menu - Library Button
        private void Menu_Library_Click(object sender, RoutedEventArgs e)
        {
            Open_Library();
            Overlay(false);
        }

        //Menu - Settings Button
        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            var vm = new SettingsViewModel();
            var settings = new Settings(vm);
            mainFrame.Content = settings;
            MainWindow_Header.Text = "Settings";
            Overlay(false);
        }

        private void Menu_Credits_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Source = new Uri("Credits.xaml", UriKind.Relative);
            MainWindow_Header.Text = "Credits";
            Overlay(false);
        }

        public void Open_AddGame()
        {
            mainFrame.Source = new Uri("AddGame.xaml", UriKind.Relative);
            MainWindow_Header.Text = "Add Game";
        }

        public void Open_Library()
        {
            var vm = new LibraryViewModel();
            var lib = new Library(vm);
            Library = vm;
            mainFrame.Content = lib;
            MainWindow_Header.Text = "Library";
        }

        public List<string> NewGamesInDirectory;

        public void Open_GameDiscovery()
        {
            mainFrame.Source = new Uri("GameDiscovery.xaml", UriKind.Relative);
            MainWindow_Header.Text = "Game Discovery";
        }

        //Open settings sidewindow
        //Bool true, to show - false to hide
        public void Open_Settings(bool e, [Optional] string _name)
        {
            if(e == true)
            {
                GameSettingsPanel.DataContext = new GameSettingsViewModel(_name);

                //Show the panel and overlay
                Overlay(true);
                GameSettings.Visibility = Visibility.Visible;
                SlideInPanelAnimation();

            }
            else
            {
                //Hide panel
                Overlay(false);
                SlideOutPanelAnimation();
            }
        }

        public static readonly Duration PanelSlideTime = TimeSpan.FromMilliseconds(120);

        //Side panel sliding in animation, must be triggered after visiblity change
        public void SlideInPanelAnimation()
        {
            Overlay(true);
            DoubleAnimation da = new DoubleAnimation();
            da.From = 0;
            da.To = PanelWidth;
            da.Duration = PanelSlideTime;
            GameSettings.BeginAnimation(System.Windows.Controls.Grid.WidthProperty, da);
        }

        //Side Panel sliding out animation
        public void SlideOutPanelAnimation()
        {
            DoubleAnimation da = new DoubleAnimation();
            da.Completed += new EventHandler(SlideOutPanelAnimation_Finished);
            da.From = PanelWidth;
            da.To = 0;
            da.Duration = PanelSlideTime;
            GameSettings.BeginAnimation(System.Windows.Controls.Grid.WidthProperty, da);
        }

        //When sliding out animated has finished, hide the panel, because things rely on the panel's visiblity
        private void SlideOutPanelAnimation_Finished(object sender, EventArgs e)
        {
            GameSettings.Visibility = Visibility.Collapsed;
            Overlay(false);
        }

        //Close Game Settings button click
        public void CloseSettings_Button(object sender, RoutedEventArgs e)
        {
            
        }

        public void reloadLibrary()
        {
            mainFrame.NavigationService.Refresh();
        }

        public void RenameTile(string _old, string _new)
        {
            Library.ReloadGames();
        }

        public void RefreshGameArt(string game)
		{
            Library.ReloadGames();
		}

        private void Menu_Themes_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Source = new Uri("Themes.xaml", UriKind.Relative);
            MainWindow_Header.Text = "Color Themes";
            Overlay(false);
        }

        //When Window closes
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Clear Temp files
            ClearTemp();
        }

        //Clears _temp folder
        private void ClearTemp()
        {
            string[] _tempdir = Directory.GetFiles(BaseDirectory + @"\resources\_temp\");
            foreach(var file in _tempdir)
            {
                File.Delete(file);
            }
        }

        //Minimize Spectabis
        public void MainWindow_Minimize()
        {
            this.Invoke(new Action(() => WindowState = WindowState.Minimized));
        }

        //Bring Spectabis to front
        public void MainWindow_Maximize()
        {
            this.Invoke(new Action(() => WindowState = WindowState.Normal));
        }

        public void SetRunningGame(string e)
        {
            RunningGame.Text = e;
            CurrentGame = e;
        }

        public string CurrentGame = null;

        //Block Spectabis while PCSX2 is running
        public void BlockInput(bool e)
        {
            if(e)
            {
                //Spectabis input is blocked and game is running
                Block.Visibility = Visibility.Visible;

                //Start Playtime session tracker
                SessionPlaytime.Reset();
                SessionPlaytime.Start();

                //Timer that updates playtime in UI
                updatePlaytimeUI.Interval = TimeSpan.FromSeconds(60);
                updatePlaytimeUI.Start();

                DiscordRpc.UpdatePresence(CurrentGame);

            }
            else
            {
                //Default Rich Presence
                DiscordRpc.UpdatePresence("Menus");

                //Input is not blocked and game is not running
                Block.Visibility = Visibility.Collapsed;

                //Stop playtime session tracker
                if(SessionPlaytime.IsRunning)
                {
                    SessionPlaytime.Stop();
                    updatePlaytimeUI.Stop();
                    Console.WriteLine("Session Lenght: " + SessionPlaytime.Elapsed.TotalMinutes);
                    Console.WriteLine($"SessionTimer Working: {SessionPlaytime.IsRunning}");
                }
            }
        }

        //Update session timer in UI
        private void updatePlaytimeUI_Tick(object sender, EventArgs e)
        {
            if(SessionPlaytime.Elapsed.TotalMinutes == 1)
            {
                this.Invoke(new Action(() => SessionLenght.Text = $"Current Session: {SessionPlaytime.Elapsed.TotalMinutes} minute"));
            }
            else
            {
                this.Invoke(new Action(() => SessionLenght.Text = $"Current Session: {SessionPlaytime.Elapsed.TotalMinutes} minutes"));
            }

            //As this timer updates every minute, playtime in file gets updated also
            Playtime.AddPlaytime(CurrentGame, TimeSpan.FromMinutes(1));

            Console.WriteLine($"Updating UI with TimerTimer: {SessionPlaytime.Elapsed.TotalMinutes} minutes, adding 1 minute to file");
        }


        //Force Stop PCSX2 button
        private void ForceStop_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() => Library.ForceStop()));
        }

        private void AprilFools_Button(object sender, RoutedEventArgs e)
        {
            //Fade out
            DoubleAnimation da = new DoubleAnimation();
            da.From = 1;
            da.To = 0;
            da.Duration = TimeSpan.FromSeconds(1.5);
            AprilFools_Grid.BeginAnimation(System.Windows.Controls.Grid.OpacityProperty, da);

            AprilFools_Grid.IsHitTestVisible = false;

            Dispatcher.Invoke(new Action(() => PushSnackbar("Happy April Fools' Day! Pre-order 'Horse Armor DLC' now! ")));

            //Don't show this message again
            Properties.Settings.Default.Aprilfooled = true;
            Properties.Settings.Default.Save();
        }

        public void PushSnackbar(string msg)
        {
            var messageQueue = SnackBar.MessageQueue;

            //the message queue can be called from any thread
            Task.Factory.StartNew(() => messageQueue.Enqueue(msg));
        }

        public void PushSnackbar(object content)
        {
            var messageQueue = SnackBar.MessageQueue;

            //the message queue can be called from any thread
            Task.Factory.StartNew(() => messageQueue.Enqueue(content));
        }

        //Push a snackbar when there's a huge number of new games in a directory
        public void PushMultipleDirectoryDialog(int count, List<string> list)
        {
            NewGamesInDirectory = list;

            TextBlock text = new TextBlock();
            text.FontFamily = new FontFamily("Roboto Light");
            text.Text = $"There are {count} new games in your directory";
            text.TextWrapping = TextWrapping.Wrap;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Margin = new Thickness(0, 0, 10, 0);

            Button OpenAll = new Button();
            OpenAll.Content = "Show";
            OpenAll.Click += MultipleDirectory_Click;
            OpenAll.Margin = new Thickness(0, 0, 10, 0);

            Button Dismiss = new Button();
            Dismiss.Content = "Dismiss";
            Dismiss.Click += MultipleDirectory_Click;

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.Children.Add(text);
            panel.Children.Add(OpenAll);
            panel.Children.Add(Dismiss);

            PushSnackbar(panel);
        }

        private void MultipleDirectory_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            SnackBar.IsActive = false;

            if (button.Content.ToString() == "Show")
            {
                //Navigate to Game Discovery page
                Open_GameDiscovery();
            }
        }

        public void PushDirectoryDialog(string game)
        {
            TextBlock text = new TextBlock();
            text.FontFamily = new FontFamily("Roboto Light");
            text.Text = $"Would you like to add \"{GetGameName.GetName(game)}\" ?";
            text.TextWrapping = TextWrapping.Wrap;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Margin = new Thickness(0, 0, 10, 0);

            Button YesButton = new Button();
            YesButton.Content = "Yes";
            YesButton.Margin = new Thickness(0, 0, 10, 0);
            YesButton.Click += DirectoryDialog_Click;

            Button NoButton = new Button();
            NoButton.Content = "No";
            NoButton.Click += DirectoryDialog_Click;

            //Set file path as tag, so it can be accessed by Dialog_Click
            YesButton.Tag = game;
            NoButton.Tag = game;

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.Children.Add(text);
            panel.Children.Add(YesButton);
            panel.Children.Add(NoButton);

            PushSnackbar(panel);
        }

        //Directory Snackbar notification Yes/No buttons
        private void DirectoryDialog_Click(object sender, EventArgs e)
        {
            //Get the game file from button tag
            Button button = (Button)sender;
            string game = button.Tag.ToString();

            //Hide Snackbar
            SnackBar.IsActive = false;

            //Yes Button
            if (button.Content.ToString() == "Yes")
            {
                Console.WriteLine("Adding " + game);

                if (Properties.Settings.Default.TitleAsFile)
                {
                    GameProfile.Create(null, game, Path.GetFileNameWithoutExtension(game));
                }
                else
                {
                    GameProfile.Create(null, game, GetGameName.GetName(game));
                }

                Library.ReloadGames();
            }
        }
    }
}