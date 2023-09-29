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
        private MainWindow mainWindow;
        private string GameConfigs = App.BaseDirectory + @"\resources\configs\";
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
                        param => mainWindow.Open_AddGame()
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

        public LibraryViewModel()
		{
            Console.WriteLine("Opening Library...");

            mainWindow = Application.Current.MainWindow as MainWindow;

            //Hide searchbar
            if (Properties.Settings.Default.searchbar == false)
            {
                SearchVisibility = Visibility.Collapsed;
            }

            //List all loaded games
            EnumerateISOs();

            ScanGameDirectory();

            //Set appropriate menu icon for global controller settings
            setGlobalControllerIcon(Properties.Settings.Default.globalController);

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
            string[] _gamesdir = Directory.GetDirectories(GameConfigs);

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
            if (Properties.Settings.Default.gameDirectory != null)
            {
                //If game directory doesn't exist, stop and remove it from variable
                if (Directory.Exists(Properties.Settings.Default.gameDirectory) == false)
                {
                    mainWindow.PushSnackbar("Game Directory doesn't exist anymore!");
                    Properties.Settings.Default.gameDirectory = null;
                    Properties.Settings.Default.Save();
                }

                //List of all files that don't contain already loaded files
                if (Properties.Settings.Default.gameDirectory == null)
                    return;

                string[] _fileList;

                try
                {
                    _fileList = Directory.GetFiles(Properties.Settings.Default.gameDirectory, "*.???", SearchOption.AllDirectories);
                    Console.WriteLine(_fileList.Count() + " files found!");
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to enumerate game directory - '{Properties.Settings.Default.gameDirectory}'");
                    ((MainWindow)Application.Current.MainWindow).PushSnackbar("Failed to enumerate game file directory! Try selecting a different folder.");
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
                Properties.Settings.Default.globalController = true;
            }
            else if (GlobalControllerIcon == PackIconKind.XboxController)
            {
                Console.WriteLine("Turning off Global Controller settings");
                setGlobalControllerIcon(false);
                Properties.Settings.Default.globalController = false;
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

            if (Directory.Exists(GameConfigs))
            {
                //Makes a collection of game folders from game config directory
                string[] _gamesdir = Directory.GetDirectories(GameConfigs);

                //Loops through each folder in game config directory
                foreach (string game in _gamesdir)
                {
                    //Loads only games that contain query string
                    if (game.ToLower().Contains(query.ToLower()))

                        if (File.Exists(game + @"\Spectabis.ini"))
                        {
                            //Sets _gameName to name of the folder
                            string _gameName = game.Remove(0, game.LastIndexOf(Path.DirectorySeparatorChar) + 1);

                            Games.Add(new LibraryGameViewModel(_gameName, this));
                            NoGameVisibility = Visibility.Collapsed;
                        }
                }
            }

            Directory.CreateDirectory(GameConfigs);
        }

        public void AddGame(string _img, string _isoDir, string _title)
        {
            Console.WriteLine($"Title: {_title}");
            _title = _title.ToSanitizedString();
            Console.WriteLine($"Sanitized: {_title}");

            //Checks, if the game profile already exists
            if (Directory.Exists(App.BaseDirectory + @"\resources\configs\" + _title))
            {
                if (File.Exists(App.BaseDirectory + @"\resources\configs\" + _title + @"\Spectabis.ini"))
                {
                    mainWindow.PushSnackbar("Game Profile already exists!");
                    return;
                }
            }

            //Create a folder for game profile
            Directory.CreateDirectory(App.BaseDirectory + @"\resources\configs\" + _title);

            //Copies existing ini files from PCSX2
            //looks for inis in pcsx2 directory
            if (Directory.Exists(Properties.Settings.Default.emuDir + @"\inis\"))
            {
                string[] inisDir = Directory.GetFiles(Properties.Settings.Default.emuDir + @"\inis\");
                foreach (string inifile in inisDir)
                {
                    Console.WriteLine(inifile + " found!");
                    if (File.Exists(App.BaseDirectory + @"\resources\configs\" + _title + @"\" + Path.GetFileName(inifile)) == false)
                    {
                        string _destinationPath = Path.Combine(App.BaseDirectory + @"\resources\configs\" + _title + @"\" + Path.GetFileName(inifile));
                        File.Copy(inifile, _destinationPath);
                    }
                }
            }
            else
            {
                Console.WriteLine($"{Properties.Settings.Default.emuDir}\\inis\\ not found!");
                Console.WriteLine("Trying " + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\PCSX2\inis");

                //looks for pcsx2 inis in documents folder
                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\PCSX2\inis"))
                {
                    string[] inisDirDoc = Directory.GetFiles((Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\PCSX2\inis"));
                    foreach (string inifile in inisDirDoc)
                    {
                        if (File.Exists(App.BaseDirectory + @"\resources\configs\" + _title + @"\" + Path.GetFileName(inifile)) == false)
                        {
                            string _destinationPath = Path.Combine(App.BaseDirectory + @"\resources\configs\" + _title + @"\" + Path.GetFileName(inifile));
                            File.Copy(inifile, _destinationPath);
                        }
                    }
                }

                //if no inis are found, warning is shown
                else
                {
                    Console.WriteLine("Cannot find default PCSX2 configuration");
                    mainWindow.PushSnackbar("Cannot find default PCSX2 configuration");
                }

            }

            //Create a blank Spectabis.ini file
            var gameIni = new IniFile(App.BaseDirectory + @"\resources\configs\" + _title + @"\spectabis.ini");
            gameIni.Write("isoDirectory", _isoDir, "Spectabis");
            gameIni.Write("nogui", "1", "Spectabis");
            gameIni.Write("fullscreen", "1", "Spectabis");
            gameIni.Write("fullboot", "1", "Spectabis");
            gameIni.Write("nohacks", "1", "Spectabis");

            //Copy tempart from resources and filestream it to game profile
            Properties.Resources.tempArt.Save(App.BaseDirectory + @"\resources\_temp\art.jpg");

            try
            {
                File.Copy(App.BaseDirectory + @"\resources\_temp\art.jpg", App.BaseDirectory + @"\resources\configs\" + _title + @"\art.jpg", true);
            }
            catch
            {
                Console.WriteLine("Failed to copy temporal art file...");
            }
            finally
            {
                //If game boxart location is null, then try scrapping
                if (_img == null)
                {
                    //Add game title to automatic scrapping tasklist
                    if (Properties.Settings.Default.autoBoxart == true)
                    {
                        Console.WriteLine("Adding " + _title + " to taskQueue!");
                    }
                }

                //Add game to gamePanel
                Games.Add(new LibraryGameViewModel(_title, this));
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
                Properties.Settings.Default.gameDirectory = DirectoryDialog.SelectedPath;
                Properties.Settings.Default.Save();
                ReloadGames();

                Console.WriteLine(DirectoryDialog.SelectedPath + " set as directory!");
            }
            else
            {
                Properties.Settings.Default.gameDirectory = null;
                Properties.Settings.Default.Save();
                mainWindow.PushSnackbar("Game directory folder has been removed!");
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
