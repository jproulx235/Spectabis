using MahApps.Metro.Controls;
using Spectabis_WPF.Domain;
using Spectabis_WPF.View_Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// Interaction logic for LibraryListGame.xaml
	/// </summary>
	public partial class LibraryListGame : UserControl
	{
		public LibraryListGameViewModel ViewModel { get; set; }
		private Process PCSX = new Process();
		public LibraryListGame()
		{
			InitializeComponent();
		}

		public LibraryListGame(LibraryListGameViewModel vm)
		{
			this.DataContext = vm;
			ViewModel = vm;

			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Console.WriteLine($"CopyGlobalProfile({ViewModel.GameName})");
			GameProfile.CopyGlobalProfile(ViewModel.GameName);

			PCSX = LaunchPCSX2.CreateGameProcess(ViewModel.GameName);
			PCSX.EnableRaisingEvents = true;
			PCSX.Exited += new EventHandler(PCSX_Exited);

			PCSX.Start();

			//Minimize Window
			this.Invoke(new Action(() => ((MainWindow)Application.Current.MainWindow).MainWindow_Minimize()));

			//Set running game text
			this.Invoke(new Action(() => ((MainWindow)Application.Current.MainWindow).SetRunningGame(ViewModel.GameName)));

			BlockInput(true);
		}

		private void PCSX_Exited(object sender, EventArgs e)
		{
			//Bring Spectabis to front
			BlockInput(false);
			this.Invoke(new Action(() => ((MainWindow)Application.Current.MainWindow).MainWindow_Maximize()));
		}

		private void BlockInput(bool e)
		{
			this.Invoke(new Action(() => ((MainWindow)Application.Current.MainWindow).BlockInput(e)));
		}
	}
}
