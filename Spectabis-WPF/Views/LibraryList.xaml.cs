using Spectabis_WPF.View_Models;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace Spectabis_WPF.Views
{
	/// <summary>
	/// Interaction logic for LibraryList.xaml
	/// </summary>
	public partial class LibraryList : Page
	{
		public List<LibraryListGameViewModel> Games { get; set; } = new List<LibraryListGameViewModel>();
		public LibraryListGameViewModel Game { get; set; }
		public LibraryList()
		{
			var dir = App.BaseDirectory + @"\resources\configs\";

			string[] _gamesdir = Directory.GetDirectories(dir);

			//Loops through each folder in game config directory
			foreach (string game in _gamesdir)
			{
				string _gameName = game.Remove(0, game.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
				Games.Add(new LibraryListGameViewModel(_gameName));
			}

			Game = Games.First();

			InitializeComponent();
		}
	}
}
