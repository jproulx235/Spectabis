using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.PlatformUI;
using Spectabis_WPF.Domain;
using Spectabis_WPF.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Spectabis_WPF.ViewModels
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private Stopwatch SessionPlaytime = new Stopwatch();
		private DispatcherTimer updatePlaytimeUI = new DispatcherTimer();
		public DiscordRPC DiscordRpc;
		public List<string> NewGamesInDirectory;

		private SnackbarMessageQueue queue = new SnackbarMessageQueue();
		private ICommand exitGameCommand;
		private ICommand aprilFoolsCommand;
		private Visibility blockVisibility = Visibility.Collapsed;
		private string currentGame;
		private ICommand menuLibraryCommand;
		private ICommand menuSettingsCommand;
		private ICommand menuAppearanceCommand;
		private ICommand menuCreditsCommand;
		private Visibility menuVisibility = Visibility.Collapsed;
		private bool menuOpen;
		private Page currentPage;
		private string header;
		private Visibility firstTimeSetupVisibility = Visibility.Collapsed;
		private Visibility aprilFoolsVisibility = Visibility.Collapsed;
		private string sessionLengthMessage;
		private Visibility overlayVisibility = Visibility.Collapsed;
		private Visibility gameSettingsVisibility = Visibility.Collapsed;
		private bool snackBarActive;
		private GameSettings gameSettings;


		public SnackbarMessageQueue SnackBarQueue
		{
			get => queue;
			set
			{
				queue = value;
				OnPropertyChanged();
			}
		}

		public ICommand ExitGameCommand
		{
			get
			{
				if (exitGameCommand == null)
				{
					exitGameCommand = new DelegateCommand(
						param => ExitGame()
					);
				}
				return exitGameCommand;
			}
		}

		public ICommand AprilFoolsCommand
		{
			get
			{
				if (aprilFoolsCommand == null)
				{
					aprilFoolsCommand = new DelegateCommand(
						param => AprilFools()
					);
				}
				return aprilFoolsCommand;
			}
		}

		public Visibility BlockVisibility
		{
			get => blockVisibility;
			set
			{
				blockVisibility = value;
				OnPropertyChanged();
			}
		}

		public string CurrentGame
		{
			get => currentGame;
			set
			{
				currentGame = value;
				OnPropertyChanged();
			}
		}

		public ICommand MenuLibraryCommand
		{
			get
			{
				if (menuLibraryCommand == null)
				{
					menuLibraryCommand = new DelegateCommand(
						param => OpenLibrary()
					);
				}
				return menuLibraryCommand;
			}
		}

		public ICommand MenuSettingsCommand
		{
			get
			{
				if (menuSettingsCommand == null)
				{
					menuSettingsCommand = new DelegateCommand(
						param => OpenSettings()
					);
				}
				return menuSettingsCommand;
			}
		}

		public ICommand MenuAppearanceCommand
		{
			get
			{
				if (menuAppearanceCommand == null)
				{
					menuAppearanceCommand = new DelegateCommand(
						param => OpenAppearance()
					);
				}
				return menuAppearanceCommand;
			}
		}

		public ICommand MenuCreditsCommand
		{
			get
			{
				if (menuCreditsCommand == null)
				{
					menuCreditsCommand = new DelegateCommand(
						param => OpenCredits()
					);
				}
				return menuCreditsCommand;
			}
		}

		public Visibility MenuVisibility
		{
			get => menuVisibility;
			set
			{
				menuVisibility = value;
				OnPropertyChanged();
			}
		}

		public bool MenuOpen
		{
			get => menuOpen;
			set
			{
				menuOpen = value;
				MenuVisibility = OverlayVisibility = value ? Visibility.Visible : Visibility.Collapsed;
				OnPropertyChanged();
			}
		}

		public Page CurrentPage
		{
			get => currentPage;
			set
			{
				currentPage = value;
				OnPropertyChanged();
			}
		}

		public string Header
		{
			get => header;
			set
			{
				header = value;
				OnPropertyChanged();
			}
		}

		public Visibility FirstTimeSetupVisibility
		{
			get => firstTimeSetupVisibility;
			set
			{
				firstTimeSetupVisibility = value;
				OnPropertyChanged();
			}
		}

		public Visibility AprilFoolsVisibility
		{
			get => aprilFoolsVisibility;
			set
			{
				aprilFoolsVisibility = value;
				OnPropertyChanged();
			}
		}

		public string SessionLengthMessage
		{
			get => sessionLengthMessage;
			set
			{
				sessionLengthMessage = value;
				OnPropertyChanged();
			}
		}

		public Visibility OverlayVisibility
		{
			get => overlayVisibility;
			set
			{
				overlayVisibility = value;
				OnPropertyChanged();
			}
		}

		public Visibility GameSettingsVisibility
		{
			get => gameSettingsVisibility;
			set
			{
				gameSettingsVisibility = value;
				OnPropertyChanged();
			}
		}

		public bool SnackbarActive
		{
			get => snackBarActive;
			set
			{
				snackBarActive = value;
				OnPropertyChanged();
			}
		}

		public GameSettings GameSettings
		{
			get => gameSettings;
			set
			{
				gameSettings = value;
				OnPropertyChanged();
			}
		}

		public MainWindowViewModel()
		{
			CheckForUpdates();

			//Create resources folder
			Directory.CreateDirectory(SpectabisFilePath.ResourcesDirectoryPath);

			CatchCommandLineArguments();

			updatePlaytimeUI.Tick += updatePlaytimeUI_Tick;

			//Version
			Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);

			//Sets nightmode from variable
			new PaletteHelper().SetLightDark(Properties.Settings.Default.NightMode);

			//If emuDir is not set, launch first time setup
			if (ShouldShowFirstTimeSetup())
				FirstTimeSetupVisibility = Visibility.Visible;

			SetPrimary(Properties.Settings.Default.Swatch);

			//Copy spinner.gif to temporary files
			if (!Directory.Exists(SpectabisFilePath.TempDirectoryPath))
				Directory.CreateDirectory(SpectabisFilePath.TempDirectoryPath);

			Properties.Resources.spinner.Save(SpectabisFilePath.TempDirectoryPath + "\\spinner.gif");

			//Open game library page
			OpenLibrary();

			//GameSettings.Width = PanelWidth;

			DateTime now = DateTime.Now;

			//Check if it's april fool's day
			if (now == new DateTime(now.Year, 4, 1) && (Properties.Settings.Default.Aprilfooled == false))
			{
				AprilFoolsVisibility = Visibility.Visible;
			}

			DiscordRpc = new DiscordRPC();
			DiscordRpc.UpdatePresence("Menus");
		}

		public void PushSnackbar(string msg)
		{
			//the message queue can be called from any thread
			Task.Factory.StartNew(() => SnackBarQueue.Enqueue(msg));
		}

		public void PushSnackbar(object content)
		{
			//the message queue can be called from any thread
			Task.Factory.StartNew(() => SnackBarQueue.Enqueue(content));
		}

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
			SnackbarActive = false;

			if (button.Content.ToString() == "Show")
			{
				//Navigate to Game Discovery page
				OpenGameDiscovery();
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
			SnackbarActive = false;

			//Yes Button
			if (button.Content.ToString() == "Yes")
			{
				Console.WriteLine("Adding " + game);

				if (Properties.Settings.Default.TitleAsFile)
				{
					GameProfile.Create(game, Path.GetFileNameWithoutExtension(game));
				}
				else
				{
					GameProfile.Create(game, GetGameName.GetName(game));
				}

				if (CurrentPage is Library)
				{
					var lib = CurrentPage as Library;
					var libVm = lib.DataContext as LibraryViewModel;
					libVm.ReloadGames();
				}
			}
		}

		public void BlockInput(bool e)
		{
			if (e)
			{
				//Spectabis input is blocked and game is running
				BlockVisibility = Visibility.Visible;

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
				BlockVisibility = Visibility.Collapsed;

				//Stop playtime session tracker
				if (SessionPlaytime.IsRunning)
				{
					SessionPlaytime.Stop();
					updatePlaytimeUI.Stop();
					Console.WriteLine("Session Lenght: " + SessionPlaytime.Elapsed.TotalMinutes);
					Console.WriteLine($"SessionTimer Working: {SessionPlaytime.IsRunning}");
				}
			}
		}


		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private void SetPrimary(string swatch)
		{
			Console.WriteLine("Setting PrimaryColor to " + swatch);
			new PaletteHelper().ReplacePrimaryColor(swatch);
		}

		private void updatePlaytimeUI_Tick(object sender, EventArgs e)
		{
			if (SessionPlaytime.Elapsed.TotalMinutes == 1)
			{
				Application.Current.Dispatcher.Invoke(new Action(() => SessionLengthMessage = $"Current Session: {SessionPlaytime.Elapsed.TotalMinutes} minute"));
			}
			else
			{
				Application.Current.Dispatcher.Invoke(new Action(() => SessionLengthMessage = $"Current Session: {SessionPlaytime.Elapsed.TotalMinutes} minutes"));
			}

			//As this timer updates every minute, playtime in file gets updated also
			Playtime.AddPlaytime(CurrentGame, TimeSpan.FromMinutes(1));

			Console.WriteLine($"Updating UI with TimerTimer: {SessionPlaytime.Elapsed.TotalMinutes} minutes, adding 1 minute to file");
		}

		private static bool ShouldShowFirstTimeSetup()
		{
			var checkDir = Properties.Settings.Default.EmuExePath;

			if (string.IsNullOrEmpty(checkDir))
				return true;

			if (File.Exists(checkDir) && checkDir.EndsWith(".exe"))
				return false;

			checkDir = Path.Combine(checkDir, "pcsx2.exe");
			if (File.Exists(checkDir))
			{
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

		private void CatchCommandLineArguments()
		{
			//Make alist of all arguments
			List<string> arguments = new List<string>(Environment.GetCommandLineArgs());
			bool findProfile = false;

			foreach (string arg in arguments)
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
					FirstTimeSetupVisibility = Visibility.Visible;
				}
				//Launch a game profile
				else if (arg == "-profile")
				{
					Console.WriteLine("Looking for a game profile to launch...");
					findProfile = true;
				}
				//If profile needs to be found, then do that
				else if (findProfile == true)
				{
					//If there's a game profile with given argument
					if (Directory.Exists(SpectabisFilePath.GetGameConfigDirectoryPath(arg)))
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
						Application.Current.Dispatcher.Invoke(() => SnackBarQueue.Enqueue("A new update is available!"));
					}
					catch
					{
						Console.WriteLine("Couldn't push update notification");
					}

				}
			}
		}

		private void ExitGame()
		{
			if(CurrentPage is Library)
			{
				var lib = CurrentPage as Library;
				var libVm = lib.DataContext as LibraryViewModel;
				libVm.ForceStop();
			}
		}

		private void AprilFools()
		{

		}

		public void OpenLibrary()
		{
			var vm = new LibraryViewModel(this);
			var lib = new Library(vm);
			CurrentPage = lib;
			Header = "Library";
			OverlayVisibility = Visibility.Collapsed;
			MenuOpen = false;
		}

		public void OpenAddGame()
		{
			CurrentPage = new AddGame(this);
			Header = "Add Game";
		}

		public void OpenGameDiscovery()
		{
			CurrentPage = new GameDiscovery(NewGamesInDirectory, this);
			Header = "Game Discovery";
		}

		public void OpenGameSettings(string game)
		{
			GameSettings = new GameSettings(new GameSettingsViewModel(game));
			((MainWindow)Application.Current.MainWindow).SlideInPanelAnimation();
		}

		private void OpenSettings()
		{
			var vm = new SettingsViewModel();
			var settings = new Settings(vm);
			CurrentPage = settings;
			Header = "Settings";
			OverlayVisibility = Visibility.Collapsed;
			MenuOpen = false;
		}

		private void OpenAppearance()
		{
			CurrentPage = new Themes();
			Header = "Color Themes";
			OverlayVisibility = Visibility.Collapsed;
			MenuOpen = false;
		}

		private void OpenCredits()
		{
			CurrentPage = new Credits();
			Header = "Credits";
			OverlayVisibility = Visibility.Collapsed;
			MenuOpen = false;
		}
	}
}
