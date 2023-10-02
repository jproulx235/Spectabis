using Spectabis_WPF.Domain;
using Spectabis_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Spectabis_WPF.Views
{
	/// <summary>
	/// Interaction logic for GameSettings.xaml
	/// </summary>
	public partial class GameSettings : UserControl
	{
		public GameSettings(GameSettingsViewModel vm)
		{
            this.DataContext = vm;
			InitializeComponent();
		}

        private void TitleEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //Hide the textbox
            TitleEditBox.Visibility = Visibility.Collapsed;

            //Show real label
            Header_title.Visibility = Visibility.Visible;
        }

        private void TitleEditBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            List<string> chars = new List<string>();
            //Remove unsupported characters
            {
                var tempText = TitleEditBox.Text;
                foreach (var illegal in new[] { "/", "\\", ":", "|", "*", "<", ">" })
                    tempText = tempText.Replace(illegal, string.Empty);
                TitleEditBox.Text = tempText;
            }

            Console.WriteLine(e.Key.ToString());

            //This is the only way to save the name
            if (e.Key == Key.Enter)
            {

                //Save old name to a variable
                string _oldName = Header_title.Text;
                string _newName;

                //Hide the textbox
                TitleEditBox.Visibility = Visibility.Collapsed;

                //Pass text to label
                Header_title.Text = TitleEditBox.Text;

                //Show real label
                Header_title.Visibility = Visibility.Visible;

                //Save new name to the variable
                _newName = Header_title.Text;

                //Check, if old directory exists
                if (Directory.Exists(App.BaseDirectory + @"\resources\configs\" + _oldName))
                {
                    try
                    {
                        //Move old folder to new folder
                        GameProfile.Rename(_oldName, _newName);

                        //Rename game tile
                        //renameTile(_oldName, _newName);

                        Console.WriteLine($"Renamed profile '{_oldName}' to '{_newName}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Couldn't rename the folder");
                        Console.WriteLine(ex);
                    }
                }
            }

            if (e.Key == Key.Escape)
            {
                //Hide the textbox
                TitleEditBox.Visibility = Visibility.Collapsed;

                //Show real label
                Header_title.Visibility = Visibility.Visible;
            }
        }


        //Public timer, because it needs to stop itself
        public DispatcherTimer timeTimer = new DispatcherTimer();

        //Because labels don't support Click, i'll have to do it on my own
        private void Header_title_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Console.WriteLine("Clicked on game header!");

            int TimeBetweenClicks = System.Windows.Forms.SystemInformation.DoubleClickTime;

            //If the timer is not yet stopped, that means it's a double click
            //If the timer is not running, it's not a double click
            if (timeTimer.IsEnabled)
            {
                //Hide the real title label
                Header_title.Visibility = Visibility.Collapsed;

                //Set text from label and make the textbox visible
                TitleEditBox.Text = Header_title.Text;
                TitleEditBox.Visibility = Visibility.Visible;

                //Set focus to this textbox
                TitleEditBox.Focus();
                TitleEditBox.CaretIndex = TitleEditBox.Text.Length;
            }
            else
            {
                //Starts the timer, with system double click time as interval (500ms for me)
                timeTimer.Interval = new TimeSpan(0, 0, 0, 0, TimeBetweenClicks);
                timeTimer.Tick += timeTimer_Tick;
                timeTimer.Start();

                Console.WriteLine("Started timer - after ms" + TimeBetweenClicks);
            }
        }

        //Stop double-click timer on first tick
        private void timeTimer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("Tick!");
            timeTimer.Stop();
        }
    }
}
