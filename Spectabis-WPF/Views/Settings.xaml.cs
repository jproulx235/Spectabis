using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CsQuery.ExtensionMethods;
using MaterialDesignThemes.Wpf;
using Spectabis_WPF.Domain;
using Spectabis_WPF.Domain.Scraping;
using Button = System.Windows.Controls.Button;
using Spectabis_WPF.ViewModels;
using System.ComponentModel;

namespace Spectabis_WPF.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings(SettingsViewModel vm)
        {
			this.DataContext = vm;

            InitializeComponent();
        }
    }
}
