using MaterialDesignThemes.Wpf;
using SharpDX.XInput;
using Spectabis_WPF.Domain;
using Spectabis_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Spectabis_WPF.Views
{
    public partial class Library : Page
    {
        private LibraryViewModel vm;

        //Spectabis Variables
        public static string emuDir => Properties.Settings.Default.EmuExePath;

        private string BaseDirectory = App.BaseDirectory;

        //Current xInput controller
        private Controller xController;

        //Controller input listener thread
        private BackgroundWorker xListener = new BackgroundWorker();

        private List<string> LoadedISOs = new List<string>();

        //Make alist of all arguments
        public static List<string> arguments = new List<string>(Environment.GetCommandLineArgs());

        //PCSX2 Process
        public Process PCSX = new Process();

        public List<LibraryGameViewModel> Games { get; set; } = new List<LibraryGameViewModel>();

        public Library(LibraryViewModel vm)
        {
            this.DataContext = this.vm = vm;

            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;
        }

        //Get primary color from current palette
        public SolidColorBrush CurrentPrimary()
        {
            PaletteHelper PaletteQuery = new PaletteHelper();
            Palette currentPalette = PaletteQuery.QueryPalette();
            SolidColorBrush brush = new SolidColorBrush(currentPalette.PrimarySwatch.PrimaryHues.ElementAt(7).Color);
            return brush;
        }

        //Dragging file effect
        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
        }

        //Drag and drop functionality
        private void Grid_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (string file in files)
            {
                //If file is a valid game file
                if (SupportedGames.GameFiles.Any(s => file.EndsWith(s)))
                {
                    //If file supports name scrapping
                    if (SupportedGames.ScrappingFiles.Any(s => file.EndsWith(s)))
                    {
                        if(Properties.Settings.Default.TitleAsFile)
                        {
                            GameProfile.Create(file, Path.GetFileNameWithoutExtension(file));
                        }
                        else
                        {
                            GameProfile.Create(file, GetGameName.GetName(file));
                        }

                        
                    }
                    else
                    {
                        ((MainWindow)Application.Current.MainWindow).PushSnackbar("This filetype doesn't support automatic boxart!");
                        GameProfile.Create(file, Path.GetFileNameWithoutExtension(file));
                    }
                }
                else
                {
                    ((MainWindow)Application.Current.MainWindow).PushSnackbar("Unsupported file!");
                }
            }

            vm.ReloadGames();
        }

        [DllImport(@"\plugins\LilyPad.dll")]
        static private extern void PADconfigure();

        //Configuration must be closed so .dll is not in use
        [DllImport(@"\plugins\LilyPad.dll")]
        static private extern void PADclose();

        //Detect when USB devices change
        private void USBEventArrived(object sender, EventArrivedEventArgs e)
        {
            getCurrentController();
        }

        //Gets the currently connected controller
        public void getCurrentController()
        {
            //Checks, if controller is connected before detecting a new controller
            bool wasConnected = false;
            if (xController != null)
            {
                wasConnected = true;
            }

            //currentXInputDevice.cs
            currentXInputDevice getDevice = new currentXInputDevice();
            xController = getDevice.getActiveController();

            if (File.Exists(BaseDirectory + @"\x360ce.ini"))
            {
                Console.WriteLine("X360CE.ini found, be sure to use xinput1_4.dll 32-bit version");
            }

            //Show controller message, only when appropriate
            if (xController != null)
            {
                if (wasConnected == false)
                {
                    //When new a controller is detected
                    setControllerState(1);

                    Console.WriteLine("Starting xListener thread!");
                    xListener.RunWorkerAsync();
                }
            }
            else
            {
                Console.WriteLine("No controllers detected");
                if (wasConnected == true)
                {
                    //When controller is unplugged
                    setControllerState(2);
                }
            }

        }

        //Sets controller state label text
        private void setControllerState(int i)
        {
            string statusText = null;

            if (i == 1)
            {
                statusText = "Controller Detected";
            }
            else if (i == 2)
            {
                statusText = "Controller Unplugged";
            }

            //Invoke Dispatcher, in case multiple USB devices are added at the same time
            Dispatcher.BeginInvoke(new Action(() =>
            {
                //Set text from status
                ControllerStatus.Content = statusText;

                //Play fade-out animation
                DoubleAnimation da = new DoubleAnimation();
                da.Duration = TimeSpan.FromMilliseconds(1500);
                da.From = 1;
                da.To = 0;

                ControllerStatus.BeginAnimation(Label.OpacityProperty, da);
            }));
        }

        //Controller input listener thread
        private void xListener_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("xListener Started");

            currentXInputDevice xDevice = new currentXInputDevice();
            var previousState = xController.GetState();

            Console.WriteLine(xDevice.getActiveController().ToString());

            while (xController.IsConnected)
            {
                var buttons = xController.GetState().Gamepad.Buttons;

                //Check for buttons here!
                if (xDevice.getPressedButton(buttons) != "None")
                {
                    Console.WriteLine(xDevice.getPressedButton(buttons));
                }

                Thread.Sleep(100);
            }

            Console.WriteLine("Disposing of xListener thread!");
        }

        //Search bar key event
        private void SearchBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                //Cancel search and reload full library
                vm.ReloadGames();
                SearchBar.Text = null;
                MoveFocus(e);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                //Search and load only games with query
                vm.ReloadGames(SearchBar.Text);
                MoveFocus(e);
                e.Handled = true;
            }
        }

        //SearchBar "click"
        private void SearchBar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBar.Text.Length != 0)
            {
                vm.ReloadGames();
                SearchBar.Text = null;
            }
        }

        //Remove focus from textbox
        private void MoveFocus(KeyEventArgs e)
        {
            //http://stackoverflow.com/questions/8203329/moving-to-next-control-on-enter-keypress-in-wpf

            FocusNavigationDirection focusDirection = FocusNavigationDirection.Next;
            TraversalRequest request = new TraversalRequest(focusDirection);
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            // Change keyboard focus.
            if (elementWithFocus != null)
            {
                if (elementWithFocus.MoveFocus(request)) e.Handled = true;
            }
        }

		private void grid_MouseEnter(object sender, MouseEventArgs e)
		{
            Grid grid = sender as Grid;
            
            foreach(var child in grid.Children)
			{
                if(child is Border)
				{
                    var border = child as Border;
                    border.Visibility = Visibility.Visible;
                    return;
				}
			}
		}

		private void grid_MouseLeave(object sender, MouseEventArgs e)
		{
            Grid grid = sender as Grid;

            foreach (var child in grid.Children)
            {
                if (child is Border)
                {
                    var border = child as Border;
                    border.Visibility = Visibility.Collapsed;
                    return;
                }
            }
        }

		private void GlobalController_Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
            if (Properties.Settings.Default.GlobalController)
            {
                string globalProfile = BaseDirectory + @"resources\configs\#global_controller\LilyPad.ini";
                string inisTemp = BaseDirectory + @"inis\LilyPad.ini";

                Console.WriteLine("Opening Global Controller settings");
                Console.WriteLine("globalProfile = " + globalProfile);
                Console.WriteLine("inisTemp = " + inisTemp);

                GameProfile.CreateGlobalController();

                Directory.CreateDirectory(BaseDirectory + "inis");
                File.Copy(globalProfile, inisTemp, true);

                //Calls the DLL configuration function which is already imported in MainWindow
                PADconfigure();

                //Calls the configration close function which is already imported in MainWindow
                PADclose();

                File.Copy(inisTemp, globalProfile, true);
                File.Delete(inisTemp);
                Directory.Delete(BaseDirectory + @"inis\", true);

                Console.WriteLine("Global settings saved to: " + globalProfile);
            }
        }
	}
}
