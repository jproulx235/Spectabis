using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Spectabis_WPF.View_Models
{
	public class LibraryListGameViewModel : INotifyPropertyChanged
	{
		private string gameName;
		public string GameName 
		{ 
			get => gameName;
			set
			{
				gameName = value;
				OnPropertyChanged();
			} 
		}
		public BitmapImage BoxArt { get; set; }
		public int PlaytimeMinutes { get; set; }
		public decimal PlaytimeHours => PlaytimeMinutes / 60;

		public LibraryListGameViewModel(string gameName)
		{
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
	}
}
