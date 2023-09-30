using CsQuery.ExtensionMethods;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.PlatformUI;
using Spectabis_WPF.Domain;
using Spectabis_WPF.Domain.Scraping;
using Spectabis_WPF.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Spectabis_WPF.ViewModels
{
	public enum ApiItemMovement
	{
		Up = 0,
		Down = 1
	}

	public class SettingsViewModel : INotifyPropertyChanged
	{
		private bool showTitles;
		private bool darkMode;
		private bool autoScraping;
		private bool showSearchBar;
		private bool tooltips;
		private bool checkUpdates;
		private bool titleAsFile;
		private bool showPlaytime;
		private List<ApiItem> apiItems;
		private string emuDir;
		private ICommand browseCommand;
		private ICommand resetCommand;

		public bool ShowTitles
		{
			get => showTitles;
			set
			{
				showTitles = value;
				Properties.Settings.Default.ShowTitle = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged(nameof(ShowTitles));
			}
		}

		public bool DarkMode
		{
			get => darkMode;
			set
			{
				darkMode = value;
				Properties.Settings.Default.NightMode = value;
				Properties.Settings.Default.Save();
				new PaletteHelper().SetLightDark(value);
				OnPropertyChanged(nameof(DarkMode));
			}
		}

		public bool AutoScraping
		{
			get => autoScraping;
			set
			{
				autoScraping = value;
				Properties.Settings.Default.AutoBoxart = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged(nameof(AutoScraping));
			}
		}

		public bool ShowSearchBar
		{
			get => showSearchBar;
			set
			{
				showSearchBar = value;
				Properties.Settings.Default.Searchbar = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged(nameof(ShowSearchBar));
			}
		}

		public bool ShowTooltips
		{
			get => tooltips;
			set
			{
				tooltips = value;
				Properties.Settings.Default.Tooltips = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged(nameof(ShowTooltips));
			}
		}

		public bool CheckUpdates
		{
			get => checkUpdates;
			set
			{
				checkUpdates = value;
				Properties.Settings.Default.Checkupdates = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged(nameof(CheckUpdates));
			}
		}

		public bool TitleAsFile
		{
			get => titleAsFile;
			set
			{
				titleAsFile = value;
				Properties.Settings.Default.TitleAsFile = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged(nameof(TitleAsFile));
			}
		}

		public bool ShowPlaytime
		{
			get => showPlaytime;
			set
			{
				showPlaytime = value;
				Properties.Settings.Default.Playtime = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged(nameof(ShowPlaytime));
			}
		}

		public List<ApiItem> ApiItems
		{
			get => apiItems;
			set
			{
				apiItems = value;
				OnPropertyChanged(nameof(ApiItems));
			}
		}

		public string EmuDir
		{
			get => emuDir;
			set
			{
				emuDir = value;
				Properties.Settings.Default.EmuExePath = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged(nameof(EmuDir));
			}
		}

		public ICommand BrowseCommand
		{
			get
			{
				if (browseCommand == null)
					browseCommand = new DelegateCommand(() => Browse());

				return browseCommand;
			}
		}

		public ICommand ResetCommand
		{
			get
			{
				if (resetCommand == null)
					resetCommand = new DelegateCommand(() => ResetPriorities());

				return resetCommand;
			}
		}

		public SettingsViewModel()
		{
			ApiItems = new List<ApiItem>();

			// Set the private properrties so we don't trigger the saving
			showTitles = Properties.Settings.Default.ShowTitle;
			darkMode = Properties.Settings.Default.NightMode;
			autoScraping = Properties.Settings.Default.AutoBoxart;
			showSearchBar = Properties.Settings.Default.Searchbar;
			tooltips = Properties.Settings.Default.Tooltips;
			checkUpdates = Properties.Settings.Default.Checkupdates;
			titleAsFile = Properties.Settings.Default.TitleAsFile;
			showPlaytime = Properties.Settings.Default.Playtime;
			emuDir = Properties.Settings.Default.EmuExePath;

			var getProp = new Func<string, PropertyInfo>(p => typeof(Properties.Settings).GetProperty(p));
			var apiList = new List<ApiItem>{
					new ApiItem(this)
					{ 
						ApiName = "Giant Bomb" , 
						PropertyInfo = getProp(nameof(Properties.Settings.APIKey_GiantBomb)) , 
						ScraperApi = ScrapeArt.Scrapers[ScraperType.GiantBomb]
					},
					new ApiItem(this)
					{ 
						ApiName = "TheGamesDB" , 
						PropertyInfo = getProp(nameof(Properties.Settings.APIKey_TheGamesDb)), 
						ScraperApi = ScrapeArt.Scrapers[ScraperType.TheGamesDbApi]
					},
					new ApiItem(this)
					{ 
						ApiName = "IGDB", 
						PropertyInfo = getProp(nameof(Properties.Settings.APIKey_IGDB)), 
						ScraperApi = ScrapeArt.Scrapers[ScraperType.IGDB]
					},
					new ApiItem(this)
					{ 
						ApiName = "Moby Games" , 
						PropertyInfo = getProp(nameof(Properties.Settings.APIKey_MobyGames)), 
						ScraperApi = ScrapeArt.Scrapers[ScraperType.MobyGames] 
					},
					new ApiItem(this)
					{ 
						ApiName = "TheGamesDb Open", 
						IsFreeApi = true, 
						ScraperApi = ScrapeArt.Scrapers[ScraperType.TheGamesDbHtml]
					}
				}
				.Select((p, i) => {
					p.Id = (ScraperType)i;
					return p;
				})
				.ToList();

			var order = new[] {
					Properties.Settings.Default.APIUserSequence,
					Properties.Settings.Default.APIAutoSequence
				}
				.First(p => string.IsNullOrEmpty(p) == false)
				.Split(',')
				.Select(int.Parse)
				.ToList();

			apiList = apiList
				.Select((p, i) => apiList[order[i]])
				.ToList();

			foreach (var item in apiList)
			{
				item.Index = apiList.IndexOf(item);
			}

			ApiItems = apiList.OrderBy(x => x.Index).ToList();
		}

		private void ResetPriorities()
		{
			foreach(var item in ApiItems)
			{
				item.Index = (int)item.Id;
			}

			ApiItems = ApiItems
				.OrderBy(x => x.Index)
				.ToList();

			Properties.Settings.Default.APIUserSequence = string.Join(",", ApiItems.Select(p => (int)p.Id));
			Properties.Settings.Default.Save();
		}

		public void IndexChanged(ScraperType type, ApiItemMovement move)
		{
			var item = ApiItems.FirstOrDefault(x => x.Id == type);

			int oldIndex = item.Index;

			// Moved down
			if(move == ApiItemMovement.Down)
			{
				var other = ApiItems[item.Index + 1];
				item.Index++;
				other.Index--;
			}
			// Moved up
			else
			{
				var other = ApiItems[item.Index - 1];
				item.Index--;
				other.Index++;
			}

			ApiItems = ApiItems
				.OrderBy(x => x.Index)
				.ToList();

			Properties.Settings.Default.APIUserSequence = string.Join(",", ApiItems.Select(p => (int)p.Id));
			Properties.Settings.Default.Save();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private void Browse()
		{
			Ookii.Dialogs.Wpf.VistaFolderBrowserDialog BrowserDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
			BrowserDialog.Description = "Select PCSX2 Directory";
			BrowserDialog.UseDescriptionForTitle = true;

			//Show the dialog and check, if directory contains pcsx2.exe
			var BrowserResult = BrowserDialog.ShowDialog();
			//If OK was clicked...
			if (BrowserResult == true)
			{
				var filesInFolder = Directory.GetFiles(BrowserDialog.SelectedPath);

				if (filesInFolder.Any(p => p.Contains("pcsx2") && p.EndsWith(".exe")) == false)
				{
					//If directory isn't PCSX2's, fall back to beginning
					((MainWindow)Application.Current.MainWindow).PushSnackbar("Invalid Emulator Directory");
					return;
				}

				//Set emudir textbox to location of selected directory
				EmuDir = BrowserDialog.SelectedPath;
			}
		}
	}
}
