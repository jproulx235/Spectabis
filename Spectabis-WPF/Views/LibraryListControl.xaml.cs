using Spectabis_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// Interaction logic for LibraryListControl.xaml
	/// </summary>
	public partial class LibraryListControl : UserControl
	{
		public List<LibraryGameViewModel> Games { get; set; } = new List<LibraryGameViewModel>();
		public Process PCSX = new Process();
		public LibraryListControl()
		{
			InitializeComponent();

			this.DataContext = this;

			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			var dir = App.BaseDirectory + @"\resources\configs\";

			string[] _gamesdir = Directory.GetDirectories(dir);

			//Loops through each folder in game config directory
			foreach (string game in _gamesdir)
			{
				string _gameName = game.Remove(0, game.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);

				if (File.Exists(game + @"\Spectabis.ini"))
					Games.Add(new LibraryGameViewModel(_gameName, this));
			}
		}
	}
}
