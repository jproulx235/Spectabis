using Microsoft.VisualStudio.PlatformUI;
using Spectabis_WPF.Domain;
using Spectabis_WPF.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		private LibraryList list;

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
		public BitmapImage BoxArt { get; set; }
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

		public LibraryListGameViewModel(string gameName, LibraryList list)
		{
			this.list = list;
			GameName = gameName;

			if (Properties.Settings.Default.playtime == true)
			{
				IniFile spectabis = new IniFile($"{App.BaseDirectory + @"\resources\configs\"}//{gameName}//spectabis.ini");
				string minutes = spectabis.Read("playtime", "Spectabis");

				PlaytimeMinutes = minutes != "" ? Convert.ToInt32(minutes) : 0;
			}

			BoxArt = new System.Windows.Media.Imaging.BitmapImage();

			BoxArt.BeginInit();

			//Fixes the caching issues, where cached copy would just hang around and bother me for two days
			BoxArt.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.None;
			BoxArt.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
			BoxArt.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;

			BoxArt.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
			BoxArt.UriSource = new Uri(@"resources\configs\" + gameName + @"\art.jpg", UriKind.RelativeOrAbsolute);

			BoxArt.EndInit();
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
	}
}
