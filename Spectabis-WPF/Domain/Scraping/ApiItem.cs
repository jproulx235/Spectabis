using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Spectabis_WPF.Properties;
using Spectabis_WPF.ViewModels;

namespace Spectabis_WPF.Domain.Scraping {
	[DebuggerDisplay("{" + nameof(ApiName) + "}")]
	public class ApiItem : INotifyPropertyChanged
	{
		private int index;
		private ICommand upCommand;
		private ICommand downCommand;
		private SettingsViewModel settings;

		public string ApiName { get; set; }
		public string ApiKey
		{
			get => (string)PropertyInfo?.GetValue(Settings.Default);
			set => PropertyInfo?.SetValue(Settings.Default, value);
		}

		public bool IsFreeApi { get; set; }
		public IScraperApi ScraperApi { get; set; }
		public PropertyInfo PropertyInfo { get; set; }
		public ScraperType Id { get; set; }
		public int Index 
		{ 
			get => index; 
			set 
			{ 
				index = value;
				OnPropertyChanged(nameof(Index));
			}
		}
		public bool Enabled { get; set; }

		public ICommand UpCommand
		{
			get
			{
				if (upCommand == null)
					upCommand = new DelegateCommand(() => MoveUp());

				return upCommand;
			}
		}

		public ICommand DownCommand
		{
			get
			{
				if (downCommand == null)
					downCommand = new DelegateCommand(() => MoveDown());

				return downCommand;
			}
		}

		public ApiItem(SettingsViewModel settings)
		{
			this.settings = settings;
		}

		private void MoveUp()
		{
			settings.IndexChanged(Id, ApiItemMovement.Up);
		}

		private void MoveDown()
		{
			settings.IndexChanged(Id, ApiItemMovement.Down);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}