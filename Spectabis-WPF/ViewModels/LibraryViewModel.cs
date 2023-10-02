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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Spectabis_WPF.ViewModels
{
	public class LibraryViewModel : INotifyPropertyChanged
	{
        private MainWindowViewModel mainWindow;
        private List<string> LoadedISOs = new List<string>();
        private List<LibraryGameViewModel> games;
        private PackIconKind controllerIcon;
        private string controllerTooltip;
        private Visibility gameListVisiblity;
        private Visibility gameGridVisiblity;
        private bool gridButtonEnabled;
        private bool listButtonEnabled;
        private Visibility noGameVisibility;


        public Process PCSX = new Process();

        public List<LibraryGameViewModel> Games 
        { 
            get => games;
			set
			{
                games = value;
                OnPropertyChanged(nameof(Games));
			} 
        }

        public PackIconKind GlobalControllerIcon
		{
            get => controllerIcon;
			set
			{
                controllerIcon = value;
                OnPropertyChanged(nameof(GlobalControllerIcon));
			}
		}

        public string GlobalControllerTooltip
		{
            get => controllerTooltip;
            set
			{
                controllerTooltip = value;
                OnPropertyChanged(nameof(GlobalControllerTooltip));
			}
		}

        public Visibility SearchVisibility { get; set; }

        public Visibility GameListVisibility
		{
            get => gameListVisiblity;
			set
			{
                gameListVisiblity = value;
                OnPropertyChanged(nameof(GameListVisibility));
			}
		}

        public Visibility GameGridVisibility
        {
            get => gameGridVisiblity;
            set
            {
                gameGridVisiblity = value;
                OnPropertyChanged(nameof(GameGridVisibility));
            }
        }

        public bool GridButtonEnabled
		{
            get => gridButtonEnabled;
            set
			{
                gridButtonEnabled = value;
                OnPropertyChanged(nameof(GridButtonEnabled));
			}
		}

        public bool ListButtonEnabled
        {
            get => listButtonEnabled;
            set
            {
                listButtonEnabled = value;
                OnPropertyChanged(nameof(ListButtonEnabled));
            }
        }

        public Visibility NoGameVisibility
		{
            get => noGameVisibility;
			set
			{
                noGameVisibility = value;
                OnPropertyChanged(nameof(NoGameVisibility));
			}
		}

        private ICommand _directoryCommand;

        public ICommand DirectoryCommand
        {
            get
            {
                if (_directoryCommand == null)
                {
                    _directoryCommand = new DelegateCommand(
                        param => AddDirectory()
                    );
                }
                return _directoryCommand;
            }
        }

        private ICommand _controllerCommand;

        public ICommand ControllerCommand
        {
            get
            {
                if (_controllerCommand == null)
                {
                    _controllerCommand = new DelegateCommand(
                        param => GlobalController_Toggle()
                    );
                }
                return _controllerCommand;
            }
        }

        private ICommand _addGameCommand;

        public ICommand AddGameCommand
        {
            get
            {
                if (_addGameCommand == null)
                {
                    _addGameCommand = new DelegateCommand(
                        param => mainWindow.OpenAddGame()
                    );
                }
                return _addGameCommand;
            }
        }

        private ICommand _gridViewCommand;

        public ICommand GridViewCommand
        {
            get
            {
                if (_gridViewCommand == null)
                {
                    _gridViewCommand = new DelegateCommand(
                        param => ShowGridView()
                    );
                }
                return _gridViewCommand;
            }
        }

        private ICommand _listViewCommand;

        public ICommand ListViewCommand
        {
            get
            {
                if (_listViewCommand == null)
                {
                    _listViewCommand = new DelegateCommand(
                        param => ShowListView()
                    );
                }
                return _listViewCommand;
            }
        }

        public LibraryViewModel(MainWindowViewModel mainWindow)
		{
            Console.WriteLine("Opening Library...");

            this.mainWindow = mainWindow;

            //Hide searchbar
            if (Properties.Settings.Default.Searchbar == false)
            {
                SearchVisibility = Visibility.Collapsed;
            }

            //List all loaded games
            EnumerateISOs();

            ScanGameDirectory();

            //Set appropriate menu icon for global controller settings
            setGlobalControllerIcon(Properties.Settings.Default.GlobalController);

            GameListVisibility = Visibility.Visible;
            GameGridVisibility = Visibility.Collapsed;

            GridButtonEnabled = true;
            ListButtonEnabled = false;

            NoGameVisibility = Visibility.Visible;

            ReloadGames();
        }

		public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void EnumerateISOs()
        {
            List<string> gameList = new List<string>();

            //Get all directories in Spectabis config folder
            string[] _gamesdir = Directory.GetDirectories(SpectabisFilePath.ConfigDirectoryPath);

            //Scan each game for Spectabis.ini
            foreach (var game in _gamesdir)
            {
                if (File.Exists(game + @"\spectabis.ini"))
                {
                    IniFile SpectabisIni = new IniFile(game + @"\spectabis.ini");
                    var isoDir = SpectabisIni.Read("isoDirectory", "Spectabis");
                    gameList.Add(isoDir);
                }
            }

            LoadedISOs = gameList;
        }

        private void ScanGameDirectory()
        {
            if (Properties.Settings.Default.GameDirectory != null)
            {
                //If game directory doesn't exist, stop and remove it from variable
                if (Directory.Exists(Properties.Settings.Default.GameDirectory) == false)
                {
                    //mainWindow.PushSnackbar("Game Directory doesn't exist anymore!");
                    Properties.Settings.Default.GameDirectory = null;
                    Properties.Settings.Default.Save();
                }

                //List of all files that don't contain already loaded files
                if (Properties.Settings.Default.GameDirectory == null)
                    return;

                string[] _fileList;

                try
                {
                    _fileList = Directory.GetFiles(Properties.Settings.Default.GameDirectory, "*.???", SearchOption.AllDirectories);
                    Console.WriteLine(_fileList.Count() + " files found!");
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to enumerate game directory - '{Properties.Settings.Default.GameDirectory}'");
                    mainWindow.PushSnackbar("Failed to enumerate game file directory! Try selecting a different folder.");
                    return;
                }

                int _count = _fileList.Except(LoadedISOs).ToList().Count;

                //Count after which games will be moved to "Game Discovery" page
                int TooManyFiles = 3;

                if (_count > TooManyFiles)
                {
                    mainWindow.PushMultipleDirectoryDialog(_count, _fileList.Except(LoadedISOs).ToList());
                }
                else
                {
                    //Go through each file
                    foreach (var file in _fileList)
                    {
                        //Check, if file type is in supported file types
                        if (SupportedGames.GameFiles.Any(s => file.EndsWith(s)))
                        {
                            //Check if file is already loaded in Spectabis
                            List<string> IsoList = LoadedISOs;
                            if (IsoList.Contains(file) == false)
                            {
                                Console.WriteLine(file + " is not loaded, prompting to add!");

                                //Checks, if file is in blacklist file
                                if (IsGameBlacklisted(file) == false)
                                {
                                    //Show a Yes/No message box
                                    //If "Yes" then add the game, if not, add it to blacklist
                                    Application.Current.Dispatcher.Invoke(new Action(() => mainWindow.PushDirectoryDialog(file)));
                                }
                            }
                            else
                            {
                                Console.WriteLine(file + " is already loaded, skipping.");
                            }
                        }
                    }
                }
            }
        }

        private bool IsGameBlacklisted(string _file)
        {
            //Create a folder and blacklist.text if it doesn't exist
            Directory.CreateDirectory(App.BaseDirectory + @"\resources\logs\");
            if (File.Exists(App.BaseDirectory + @"\resources\logs\blacklist.txt") == false)
            {
                var newFile = File.Create(App.BaseDirectory + @"\resources\logs\blacklist.txt");
                newFile.Close();
            }

            StreamReader blacklistFile = new StreamReader(App.BaseDirectory + @"\resources\logs\blacklist.txt");
            if (blacklistFile.ReadToEnd().Contains(_file))
            {
                blacklistFile.Close();
                return true;
            }
            else
            {
                blacklistFile.Close();
                return false;
            }
        }

        private void GlobalController_Toggle()
        {
            //Icon is off, click to turn on
            if (GlobalControllerIcon == PackIconKind.XboxControllerOff)
            {
                Console.WriteLine("Turning on Global Controller settings");
                GameProfile.CreateGlobalController();
                setGlobalControllerIcon(true);
                Properties.Settings.Default.GlobalController = true;
            }
            else if (GlobalControllerIcon == PackIconKind.XboxController)
            {
                Console.WriteLine("Turning off Global Controller settings");
                setGlobalControllerIcon(false);
                Properties.Settings.Default.GlobalController = false;
            }

            Properties.Settings.Default.Save();
            Console.WriteLine("Settings saved!");
        }

        private void setGlobalControllerIcon(bool e)
        {
            if (e == true)
            {
                GlobalControllerIcon = PackIconKind.XboxController;
                GlobalControllerTooltip = "Disable Global Controller Profile" + System.Environment.NewLine + "Right click to configure";
            }
            else
            {
                GlobalControllerIcon = PackIconKind.XboxControllerOff;
                GlobalControllerTooltip = "Enable Global Controller Profile";
            }
        }

        public void ReloadGames(string query = "")
        {
            Games = new List<LibraryGameViewModel>();

            if (Directory.Exists(SpectabisFilePath.ConfigDirectoryPath))
            {
                //Makes a collection of game folders from game config directory
                string[] _gamesdir = Directory.GetDirectories(SpectabisFilePath.ConfigDirectoryPath);

                //Loops through each folder in game config directory
                foreach (string game in _gamesdir)
                {
                    //Loads only games that contain query string
                    if (game.ToLower().Contains(query.ToLower()))

                    if (File.Exists(SpectabisFilePath.GetGameSpectabisIniFilePath(game)))
                    {
                        //Sets _gameName to name of the folder
                        string _gameName = game.Remove(0, game.LastIndexOf(Path.DirectorySeparatorChar) + 1);

                        Games.Add(new LibraryGameViewModel(_gameName, this));
                        NoGameVisibility = Visibility.Collapsed;
                    }
                }
            }
            else
			{
                Directory.CreateDirectory(SpectabisFilePath.ConfigDirectoryPath);
            }
        }

        public void AddToBlacklist(string _file)
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

			var targetGame = Games.FirstOrDefault(x => x.GameName == Path.GetFileNameWithoutExtension(_file));
			if (targetGame != null)
				targetGame.RemoveCommand.Execute(null);
		}

        private void ClearBlacklist()
        {
            if (File.Exists(App.BaseDirectory + @"\resources\logs\blacklist.txt"))
            {
                File.Delete(App.BaseDirectory + @"\resources\logs\blacklist.txt");
            }
            Directory.CreateDirectory($"{App.BaseDirectory}\\resources\\logs\\");
            File.Create($"{App.BaseDirectory}\\resources\\logs\\blacklist.txt");
        }

        public void ForceStop()
        {
            try
            {
                PCSX.Kill();
                mainWindow.BlockInput(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void AddDirectory()
        {
            var DirectoryDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            DirectoryDialog.Description = "Add Game Directory";
            DirectoryDialog.UseDescriptionForTitle = true;

            var DialogResult = DirectoryDialog.ShowDialog();

            if (DialogResult.Value == true)
            {
                Properties.Settings.Default.GameDirectory = DirectoryDialog.SelectedPath;
                Properties.Settings.Default.Save();
                ReloadGames();

                Console.WriteLine(DirectoryDialog.SelectedPath + " set as directory!");
            }
            else
            {
                //Properties.Settings.Default.GameDirectory = null;
                //Properties.Settings.Default.Save();
                //mainWindow.PushSnackbar("Game directory folder has been removed!");
            }

            ClearBlacklist();
        }

        private void ShowGridView()
        {
            GameListVisibility = Visibility.Collapsed;
            GameGridVisibility = Visibility.Visible;

            GridButtonEnabled = false;
            ListButtonEnabled = true;
        }

        private void ShowListView()
        {
            GameListVisibility = Visibility.Visible;
            GameGridVisibility = Visibility.Collapsed;

            GridButtonEnabled = true;
            ListButtonEnabled = false;
        }
    }
}
