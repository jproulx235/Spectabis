using Microsoft.VisualStudio.PlatformUI;
using Spectabis_WPF.Domain;
using Spectabis_WPF.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Spectabis_WPF.View_Models
{
	public class LibraryListGameViewModel : INotifyPropertyChanged
	{
		private string gameConfigs = App.BaseDirectory + @"\resources\configs\";

		private LibraryListControl list;


		private string gameName;
		public string GameName
		{
			get => gameName;
			set
			{
				gameName = value;
				OnPropertyChanged(nameof(GameName));
			}
		}

		private BitmapImage boxArt;
		public BitmapImage BoxArt 
		{ 
			get => boxArt;
			set
			{
				boxArt = value;
				OnPropertyChanged(nameof(BoxArt));
			} 
		}
		public int PlaytimeMinutes { get; set; }
		public decimal PlaytimeHours => PlaytimeMinutes / 60;

		public string Playtime 
		{ 
			get 
			{ 
				if (PlaytimeHours >= 2) 
				{
					return $"{PlaytimeHours} h";
				}
				else
				{
					return $"{PlaytimeMinutes} m";
				}
			} 
		}

		private ICommand _playCommand;

		public ICommand PlayCommand
		{
			get
			{
				if (_playCommand == null)
				{
					_playCommand = new DelegateCommand(
						param => PlayGame()
					);
				}
				return _playCommand;
			}
		}

		private ICommand _configPcsx2Command;

		public ICommand ConfigPcsx2Command
		{
			get
			{
				if (_configPcsx2Command == null)
				{
					_configPcsx2Command = new DelegateCommand(
						param => PCSX2ConfigureGame()
					);
				}
				return _configPcsx2Command;
			}
		}

		private ICommand _configCommand;

		public ICommand ConfigCommand
		{
			get
			{
				if (_configCommand == null)
				{
					_configCommand = new DelegateCommand(
						param => SpectabisConfig()
					);
				}
				return _configCommand;
			}
		}

		private ICommand _removeCommand;

		public ICommand RemoveCommand
		{
			get
			{
				if (_removeCommand == null)
				{
					_removeCommand = new DelegateCommand(
						param => RemoveGame()
					);
				}
				return _removeCommand;
			}
		}

		private ICommand _refetchCommand;

		public ICommand RefetchCommand
		{
			get
			{
				if (_refetchCommand == null)
				{
					_refetchCommand = new DelegateCommand(
						param => RedownloadArt()
					);
				}
				return _refetchCommand;
			}
		}

		public LibraryListGameViewModel(string gameName, LibraryListControl list)
		{
			this.list = list;
			GameName = gameName;

			if (Properties.Settings.Default.playtime == true)
			{
				IniFile spectabis = new IniFile($"{gameConfigs}//{gameName}//spectabis.ini");
				string minutes = spectabis.Read("playtime", "Spectabis");

				PlaytimeMinutes = minutes != "" ? Convert.ToInt32(minutes) : 0;
			}

			refreshArt();
		}

		private void refreshArt()
		{
			BoxArt = null;
			var art = new System.Windows.Media.Imaging.BitmapImage();

			art.BeginInit();

			//Fixes the caching issues, where cached copy would just hang around and bother me for two days
			art.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.None;
			art.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
			art.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;

			art.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
			art.UriSource = new Uri(gameConfigs + gameName + @"\art.jpg", UriKind.RelativeOrAbsolute);

			art.EndInit();
			art.Freeze();

			Application.Current.Dispatcher.Invoke(() => BoxArt = art);
		}

		public LibraryListGameViewModel()
		{ }

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		public void PlayGame()
		{
			Console.WriteLine($"CopyGlobalProfile({GameName})");
			GameProfile.CopyGlobalProfile(GameName);

			list.PCSX = LaunchPCSX2.CreateGameProcess(GameName);
			list.PCSX.EnableRaisingEvents = true;
			list.PCSX.Exited += new EventHandler(PCSX_Exited);

			list.PCSX.Start();

			//Minimize Window
			Application.Current.Dispatcher.Invoke(new Action(() => ((MainWindow)Application.Current.MainWindow).MainWindow_Minimize()));

			//Set running game text
			Application.Current.Dispatcher.Invoke(new Action(() => ((MainWindow)Application.Current.MainWindow).SetRunningGame(GameName)));

			BlockInput(true);
		}

		private void PCSX_Exited(object sender, EventArgs e)
		{
			//Bring Spectabis to front
			BlockInput(false);
			Application.Current.Dispatcher.Invoke(new Action(() => ((MainWindow)Application.Current.MainWindow).MainWindow_Maximize()));
		}

		private void BlockInput(bool e)
		{
			Application.Current.Dispatcher.Invoke(new Action(() => ((MainWindow)Application.Current.MainWindow).BlockInput(e)));
		}

		private void PCSX2ConfigureGame()
		{
			//Start PCSX2 only with --cfgpath
			string _cfgDir = gameConfigs + @"/" + GameName;
			Process.Start(Properties.Settings.Default.emuDir, " --cfgpath=\"" + _cfgDir + "\"");
		}

		private void SpectabisConfig()
		{
			//Title of the last clicked game
			((MainWindow)Application.Current.MainWindow).Open_Settings(true, GameName);
		}

		private void RemoveGame()
		{
			//Reads the game's iso file and adds it to blacklist
			IniFile SpectabisINI = new IniFile(gameConfigs + @"\" + GameName + @"\spectabis.ini");
			AddToBlacklist(SpectabisINI.Read("isoDirectory", "Spectabis"));

			list.Games.Remove(this);

			//Delete profile folder
			if (Directory.Exists(gameConfigs + @"/" + GameName))
			{
				try
				{
					Directory.Delete(gameConfigs + @"/" + GameName, true);

				}
				catch
				{
					//list.PushSnackbar("Failed to delete game files!");
				}

			}

			//Reload game list
			//reloadGames();
		}

		private void AddToBlacklist(string _file)
		{
			//Create a folder and blacklist.text if it doesn't exist
			Directory.CreateDirectory(App.BaseDirectory + @"\resources\logs\");
			if (File.Exists(App.BaseDirectory + @"\resources\logs\blacklist.txt") == false)
			{
				var newFile = File.Create(App.BaseDirectory + @"\resources\logs\blacklist.txt");
				newFile.Close();
			}

			//Add a line to blacklist
			StreamWriter blacklistFile = new StreamWriter(App.BaseDirectory + @"\resources\logs\blacklist.txt", append: true);
			blacklistFile.WriteLine(_file);
			blacklistFile.Close();
		}

		private void RedownloadArt()
		{
			BackgroundWorker worker = new BackgroundWorker();

			worker.DoWork += (object sender, DoWorkEventArgs e) =>
			{
				var scraper = new ScrapeArt(GameName);
				var result = scraper.Result;

				//if (result == null)
				//	PushSnackbar("Couldn't get the game, sorry");

				if (result != null)
					refreshArt();
			};

			worker.RunWorkerAsync();
		}
	}
}
