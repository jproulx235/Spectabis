using MahApps.Metro.Controls;
using Spectabis_WPF.Domain;
using Spectabis_WPF.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Spectabis_WPF.Views
{
	public partial class MainWindow : MetroWindow
    {
        private LibraryViewModel Library { get; set; }
        private MainWindowViewModel vm;

        public static string BaseDirectory = App.BaseDirectory;
    
        //Side panel width value
        public static readonly double PanelWidth = 700;

        public DiscordRPC DiscordRpc;

        public MainWindow()
        {
            this.DataContext = vm = new MainWindowViewModel();

            InitializeComponent();
        }

        public static readonly Duration PanelSlideTime = TimeSpan.FromMilliseconds(120);

        //Side panel sliding in animation, must be triggered after visiblity change
        public void SlideInPanelAnimation()
        {
            DoubleAnimation da = new DoubleAnimation();
            da.From = 0;
            da.To = PanelWidth;
            da.Duration = PanelSlideTime;
            GameSettings.BeginAnimation(System.Windows.Controls.Grid.WidthProperty, da);
            vm.OverlayVisibility = Visibility.Visible;
            vm.GameSettingsVisibility = Visibility.Visible;
        }

        //Side Panel sliding out animation
        public void SlideOutPanelAnimation()
        {
            DoubleAnimation da = new DoubleAnimation();
            da.Completed += new EventHandler(SlideOutPanelAnimation_Finished);
            da.From = PanelWidth;
            da.To = 0;
            da.Duration = PanelSlideTime;
            GameSettings.BeginAnimation(System.Windows.Controls.Grid.WidthProperty, da);
        }

        //When sliding out animated has finished, hide the panel, because things rely on the panel's visiblity
        private void SlideOutPanelAnimation_Finished(object sender, EventArgs e)
        {
            vm.GameSettingsVisibility = Visibility.Collapsed;
            vm.OverlayVisibility = Visibility.Collapsed;
        }

        //When Window closes
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Clear Temp files
            string[] _tempdir = Directory.GetFiles(BaseDirectory + @"\resources\_temp\");
            foreach (var file in _tempdir)
            {
                File.Delete(file);
            }
        }

        //Minimize Spectabis
        public void MainWindow_Minimize()
        {
            this.Invoke(new Action(() => WindowState = WindowState.Minimized));
        }

        //Bring Spectabis to front
        public void MainWindow_Maximize()
        {
            this.Invoke(new Action(() => WindowState = WindowState.Normal));
        }

        private void AprilFools_Button(object sender, RoutedEventArgs e)
        {
            //Fade out
            DoubleAnimation da = new DoubleAnimation();
            da.From = 1;
            da.To = 0;
            da.Duration = TimeSpan.FromSeconds(1.5);
            AprilFools_Grid.BeginAnimation(System.Windows.Controls.Grid.OpacityProperty, da);

            AprilFools_Grid.IsHitTestVisible = false;

            Dispatcher.Invoke(new Action(() => PushSnackbar("Happy April Fools' Day! Pre-order 'Horse Armor DLC' now! ")));

            //Don't show this message again
            Properties.Settings.Default.Aprilfooled = true;
            Properties.Settings.Default.Save();
        }

        public void PushSnackbar(string msg)
        {
            var messageQueue = SnackBar.MessageQueue;

            //the message queue can be called from any thread
            Task.Factory.StartNew(() => messageQueue.Enqueue(msg));
        }

        public void PushSnackbar(object content)
        {
            var messageQueue = SnackBar.MessageQueue;

            //the message queue can be called from any thread
            Task.Factory.StartNew(() => messageQueue.Enqueue(content));
        }

        public void PushDirectoryDialog(string game)
        {
            TextBlock text = new TextBlock();
            text.FontFamily = new FontFamily("Roboto Light");
            text.Text = $"Would you like to add \"{GetGameName.GetName(game)}\" ?";
            text.TextWrapping = TextWrapping.Wrap;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Margin = new Thickness(0, 0, 10, 0);

            Button YesButton = new Button();
            YesButton.Content = "Yes";
            YesButton.Margin = new Thickness(0, 0, 10, 0);
            YesButton.Click += DirectoryDialog_Click;

            Button NoButton = new Button();
            NoButton.Content = "No";
            NoButton.Click += DirectoryDialog_Click;

            //Set file path as tag, so it can be accessed by Dialog_Click
            YesButton.Tag = game;
            NoButton.Tag = game;

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.Children.Add(text);
            panel.Children.Add(YesButton);
            panel.Children.Add(NoButton);

            PushSnackbar(panel);
        }

        //Directory Snackbar notification Yes/No buttons
        private void DirectoryDialog_Click(object sender, EventArgs e)
        {
            //Get the game file from button tag
            Button button = (Button)sender;
            string game = button.Tag.ToString();

            //Hide Snackbar
            SnackBar.IsActive = false;

            //Yes Button
            if (button.Content.ToString() == "Yes")
            {
                Console.WriteLine("Adding " + game);

                if (Properties.Settings.Default.TitleAsFile)
                {
                    GameProfile.Create(game, Path.GetFileNameWithoutExtension(game));
                }
                else
                {
                    GameProfile.Create(game, GetGameName.GetName(game));
                }

                Library.ReloadGames();
            }
        }
    }
}