using Microsoft.VisualStudio.PlatformUI;
using Spectabis_WPF.Domain;
using Spectabis_WPF.Enums;
using Spectabis_WPF.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Spectabis_WPF.ViewModels
{
	public class GameSettingsViewModel : INotifyPropertyChanged
	{
		private string gameName;
		private bool fullscreen;
		private bool nogui;
		private bool fullboot;
		private bool disableSpeedHacks;
		private bool widescreen;
		private decimal zoom;
		private AspectRatio aspectratio;
		private bool customShaders;
		private ICommand configureShadersCommand;
		private ICommand videoSettingsCommand;
		private ICommand audioSettingsCommand;
		private ICommand inputSettingsCommand;
		private ICommand changeArtCommand;
		private ICommand searchWikiCommand;
		private ICommand changeFileCommand;
		private List<AspectRatio> aspectRatioChoices;
		private MainWindowViewModel mainWindow;
		private BitmapImage boxArt;
		private ICommand closeCommand;
		private string isoLocation;

		public string GameName
		{
			get => gameName;
			set
			{
				gameName = value;
				OnPropertyChanged();
			}
		}

		public bool Fullscreen
		{
			get => fullscreen;
			set
			{
				fullscreen = value;
				OnPropertyChanged();
			}
		}

		public bool NoGui
		{
			get => nogui;
			set
			{
				nogui = value;
				OnPropertyChanged();
			}
		}

		public bool FullBoot
		{
			get => fullboot;
			set
			{
				fullboot = value;
				OnPropertyChanged();
			}
		}

		public bool DisableSpeedHacks
		{
			get => disableSpeedHacks;
			set
			{
				disableSpeedHacks = value;
				OnPropertyChanged();
			}
		}

		public bool Widescreen
		{
			get => widescreen;
			set
			{
				widescreen = value;
				OnPropertyChanged();
			}
		}

		public decimal Zoom
		{
			get => zoom;
			set
			{
				zoom = value;
				OnPropertyChanged();
			}
		}

		public AspectRatio AspectRatio
		{
			get => aspectratio;
			set
			{
				aspectratio = value;
				OnPropertyChanged();
			}
		}

		public bool CustomShaders
		{
			get => customShaders;
			set
			{
				customShaders = value;
				OnPropertyChanged();
			}
		}

		public ICommand ConfigureShadersCommand
		{
			get
			{
				if (configureShadersCommand == null)
					configureShadersCommand = new DelegateCommand(() => ConfigureShaders());

				return configureShadersCommand;
			}
		}

		public ICommand VideoSettingsCommand
		{
			get
			{
				if (videoSettingsCommand == null)
					videoSettingsCommand = new DelegateCommand(() => VideoSettings());

				return videoSettingsCommand;
			}
		}

		public ICommand AudioSettingsCommand
		{
			get
			{
				if (audioSettingsCommand == null)
					audioSettingsCommand = new DelegateCommand(() => AudioSettings());

				return audioSettingsCommand;
			}
		}

		public ICommand InputSettingsCommand
		{
			get
			{
				if (inputSettingsCommand == null)
					inputSettingsCommand = new DelegateCommand(() => InputSettings());

				return inputSettingsCommand;
			}
		}

		public ICommand ChangeArtCommand
		{
			get
			{
				if (changeArtCommand == null)
					changeArtCommand = new DelegateCommand(() => ChangeArt());

				return changeArtCommand;
			}
		}

		public ICommand SearchWikiCommand
		{
			get
			{
				if (searchWikiCommand == null)
					searchWikiCommand = new DelegateCommand(() => SearchWiki());

				return searchWikiCommand;
			}
		}

		public ICommand ChangeFileCommand
		{
			get
			{
				if (changeFileCommand == null)
					changeFileCommand = new DelegateCommand(() => ChangeFile());

				return changeFileCommand;
			}
		}

		public List<AspectRatio> AspectRatioChoices
		{
			get => aspectRatioChoices;
			set
			{
				aspectRatioChoices = value;
				OnPropertyChanged();
			}
		}

		public BitmapImage BoxArt
		{
			get => boxArt;
			set
			{
				boxArt = value;
				OnPropertyChanged();
			}
		}

		public ICommand CloseCommand
		{
			get
			{
				if (closeCommand == null)
					closeCommand = new DelegateCommand(() => Close());

				return closeCommand;
			}
		}

		public string IsoLocation
		{
			get => isoLocation;
			set
			{
				isoLocation = value;
				OnPropertyChanged();
			}
		}

		public bool InputSettingsEnabled { get; set; }

		public GameSettingsViewModel(string _name)
		{
			GameName = _name;

			var config = SpectabisConfig.ReadConfig(_name);
			NoGui = config.NoGui;
			Fullscreen = config.Fullscreen;
			FullBoot = config.Fullboot;
			DisableSpeedHacks = config.NoHacks;
			IsoLocation = config.IsoPath;
			Widescreen = config.Widescreen;
			CustomShaders = config.CustomShaders;
			Zoom = config.Zoom;
			AspectRatio = config.AspectRatio;

			AspectRatioChoices = ((AspectRatio[])Enum.GetValues(typeof(AspectRatio))).ToList();

			mainWindow = (Application.Current.MainWindow as MainWindow).DataContext as MainWindowViewModel;

			//Disable Input settings button if "Global Controller Profile" is enabled
			InputSettingsEnabled = Properties.Settings.Default.GlobalController;

			//Show the panel and overlay
			(Application.Current.MainWindow as MainWindow).SlideInPanelAnimation();

			//Creates a bitmap stream
			var artSource = new System.Windows.Media.Imaging.BitmapImage();
			artSource.BeginInit();
			//Fixes the caching issues, where cached copy would just hang around and bother me for two days
			artSource.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.None;
			artSource.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
			artSource.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
			artSource.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
			artSource.UriSource = new Uri(SpectabisFilePath.GetGameBoxArtFilePath(_name), UriKind.RelativeOrAbsolute);
			artSource.EndInit();

			BoxArt = artSource;
		}


		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private void ConfigureShaders()
		{
			if (!File.Exists(SpectabisFilePath.GetGameGSdxfxFilePath(GameName)))
			{
				CopyShaders();
			}

			WriteGSdxFX();

			//open shader config file
			Process.Start(SpectabisFilePath.GetGameGSdxfxSettingsIniFilePath(GameName));
		}

		//Imports GPUconfigure from GSdx plugin
		//All GSdx plugins have same settings, by the looks of it
		//It has no inputs, but writes/reads the ini files where .exe is located at folder /inis/
		[DllImport(@"\plugins\GSdx32-SSE2.dll")]
		static private extern void GSconfigure();

		//Configuration must be closed so .dll is not in use
		[DllImport(@"\plugins\GSdx32-SSE2.dll")]
		static private extern void GSclose();

		private void VideoSettings()
		{
			try
			{
				if (File.Exists(SpectabisFilePath.GetGameGSdxIniFilePath(GameName)))
				{
					//Creates inis folder and copies it from game profile folder
					Directory.CreateDirectory(SpectabisFilePath.InisDirectoryPath);
					File.Copy(SpectabisFilePath.GetGameGSdxIniFilePath(GameName), SpectabisFilePath.GsdxIniFilePath, true);
				}
				else
				{
					Directory.CreateDirectory(SpectabisFilePath.InisDirectoryPath);
					File.Create(SpectabisFilePath.GsdxIniFilePath);
				}
			}
			catch (Exception ex)
			{
				var msg = "Unable to configure GSdx config file";
				Logger.LogException(ex, msg);
				mainWindow.PushSnackbar(msg);
			}

			try
			{
				//GPUConfigure(); - Only software mode was available
				GSconfigure();
				GSclose();

				File.Copy(SpectabisFilePath.GsdxIniFilePath, SpectabisFilePath.GetGameGSdxIniFilePath(GameName), true);
				Directory.Delete(SpectabisFilePath.InisDirectoryPath, true);
			}
			catch (Exception ex)
			{
				var msg = "Unable to configure GSdx plugin";
				Logger.LogException(ex, msg);
				mainWindow.PushSnackbar(msg);
			}
		}

		[DllImport(@"\plugins\Spu2-X.dll")]
		static private extern void SPU2configure();

		[DllImport(@"\plugins\Spu2-X.dll")]
		static private extern void SPU2close();

		private void AudioSettings()
		{
			try
			{
				//Creates inis folder and copies it from game profile folder
				Directory.CreateDirectory(SpectabisFilePath.InisDirectoryPath);

				if (File.Exists(SpectabisFilePath.GetGameSpu2xSettingsIniFilePath(GameName)))
				{
					File.Copy(SpectabisFilePath.GetGameSpu2xSettingsIniFilePath(GameName), SpectabisFilePath.Spu2xIniFilePath, true);
				}
				else
				{
					File.Create(SpectabisFilePath.Spu2xIniFilePath);
				}
			}
			catch (Exception ex)
			{
				var msg = "Could not configure SPU2-X config file";
				Logger.LogException(ex, msg);
				mainWindow.PushSnackbar(msg);
			}

			try
			{
				SPU2configure();
				SPU2close();

				File.Copy(SpectabisFilePath.Spu2xIniFilePath, SpectabisFilePath.GetGameSpu2xSettingsIniFilePath(GameName), true);
				Directory.Delete(SpectabisFilePath.InisDirectoryPath, true);
			}
			catch (Exception ex)
			{
				var msg = "Could not configure SPU2-X plugin";
				Logger.LogException(ex, msg);
				mainWindow.PushSnackbar(msg);
			}
		}

		[DllImport(@"\plugins\LilyPad.dll")]
		static private extern void PADconfigure();

		//Configuration must be closed so .dll is not in use
		[DllImport(@"\plugins\LilyPad.dll")]
		static private extern void PADclose();

		private void InputSettings()
		{
			try
			{
				//Copy the existing .ini file for editing if it exists
				if (File.Exists(SpectabisFilePath.GetGameLilyPadSettingsIniFilePath(GameName)))
				{
					//Creates inis folder and copies it from game profile folder
					Directory.CreateDirectory(SpectabisFilePath.InisDirectoryPath);
					File.Copy(SpectabisFilePath.GetGameLilyPadSettingsIniFilePath(GameName), SpectabisFilePath.LilyPadIniFilePath, true);
				}
				else
				{
					Directory.CreateDirectory(SpectabisFilePath.InisDirectoryPath);
					File.Create(SpectabisFilePath.LilyPadIniFilePath);
				}
			}
			catch (Exception ex)
			{
				var msg = "Could not configure LilyPad config file";
				Logger.LogException(ex, msg);
				mainWindow.PushSnackbar(msg);
			}

			Console.WriteLine("Loading " + SpectabisFilePath.LilyPadIniFilePath);

			try
			{
				//Calls the DLL configuration function
				PADconfigure();

				//Calls the configration close function
				PADclose();

				//Copies the modified file into the game profile & deletes the created folder
				File.Copy(SpectabisFilePath.LilyPadIniFilePath, SpectabisFilePath.GetGameLilyPadSettingsIniFilePath(GameName), true);
				Directory.Delete(SpectabisFilePath.InisDirectoryPath, true);
			}
			catch (Exception ex)
			{
				var msg = "Could not configure LilyPad plugin";
				Logger.LogException(ex, msg);
				mainWindow.PushSnackbar(msg);
			}


			Console.WriteLine("Saving to " + SpectabisFilePath.GetGameLilyPadSettingsIniFilePath(GameName));
		}

		private void ChangeArt()
		{
			Ookii.Dialogs.Wpf.VistaOpenFileDialog artBrowser = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
			artBrowser.Filter = "JPEG Files (*.jpg)|*.jpg|PNG Files (*.png)|*.png";
			artBrowser.Title = "Browse to a new boxart image";

			var browserResult = artBrowser.ShowDialog();
			if (browserResult == true)
			{
				string _file = artBrowser.FileName;

				//Url files don't get filtered, so let's just not break the game profile and stop, if 
				//selected file is indeed a url file
				if (_file.Contains(".url"))
				{
					Console.WriteLine("File was URL, returning.");
					return;
				}

				//Replace the boxart image
				BoxArt = null;
				File.Copy(_file, SpectabisFilePath.GetGameBoxArtFilePath(GameName), true);

				//Reload the image in header
				var artSource = new System.Windows.Media.Imaging.BitmapImage();
				artSource.BeginInit();
				artSource.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.None;
				artSource.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
				artSource.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;

				artSource.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
				artSource.UriSource = new Uri(SpectabisFilePath.GetGameBoxArtFilePath(GameName));
				artSource.EndInit();
				BoxArt = artSource;

				//mainWindow.RefreshGameArt(GameName);
			}
		}

		private void SearchWiki()
		{
			//Take the header title and replace spaces with + sign
			string _query = GameName;
			_query = _query.Replace(" - ", ":+");
			_query = _query.Replace(" ", "+");
			_query = _query.Replace("++", ":+");

			//Open up PCSX2 wiki
			Process.Start(@"https://wiki.pcsx2.net/index.php?search=" + _query);
		}

		private void ChangeFile()
		{

		}

		private void Close()
		{
			var config = new GameConfig
			{
				GameName = GameName,
				IsoPath = IsoLocation,
				NoGui = NoGui,
				Fullscreen = Fullscreen,
				Fullboot = FullBoot,
				NoHacks = DisableSpeedHacks,
				Widescreen = Widescreen,
				CustomShaders = CustomShaders,
				Zoom = Zoom,
				AspectRatio = AspectRatio
			};
			SpectabisConfig.SaveConfig(config);
			
			(Application.Current.MainWindow as MainWindow).SlideOutPanelAnimation();
		}

		private void CopyShaders()
		{
			if (!File.Exists(SpectabisFilePath.GetGameGSdxfxFilePath(GameName)))
			{
				File.Copy(SpectabisFilePath.EmulatorGSdxfxFilePath, SpectabisFilePath.GetGameGSdxfxFilePath(GameName));
				File.Copy(SpectabisFilePath.EmulatorGSdxfxSettingsIniFilePath, SpectabisFilePath.GetGameGSdxfxSettingsIniFilePath(GameName));
			}
		}

		//Write shader file locations to GSdx ini
		private void WriteGSdxFX()
		{
			var GSdx = new IniFile(SpectabisFilePath.GetGameGSdxIniFilePath(GameName));
			GSdx.Write("shaderfx_glsl", SpectabisFilePath.GetGameGSdxfxFilePath(GameName), "Settings");
			GSdx.Write("shaderfx_conf", SpectabisFilePath.GetGameGSdxfxSettingsIniFilePath(GameName), "Settings");
			Console.WriteLine("Shader files written to GSdx.ini");
		}
	}
}
