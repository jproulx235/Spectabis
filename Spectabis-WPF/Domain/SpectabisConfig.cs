using Spectabis_WPF.Enums;
using Spectabis_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spectabis_WPF.Domain
{
	public static class SpectabisConfig
	{
		private static string configDir => Path.Combine(App.BaseDirectory, @"resources\configs");

		public static GameConfig ReadConfig(string game)
		{
			var specIni = GetSpectabisIniFile(game);
			var _nogui = specIni.Read("nogui", "Spectabis");
			var _fullscreen = specIni.Read("fullscreen", "Spectabis");
			var _fullboot = specIni.Read("fullboot", "Spectabis");
			var _nohacks = specIni.Read("nohacks", "Spectabis");
			var _isodir = specIni.Read("isoDirectory", "Spectabis");

			var vmIni = GetVmIniFile(game);
			var _widescreen = vmIni.Read("EnableWideScreenPatches", "EmuCore");

			var gsdxIni = GetGsdxIniFile(game);
			var _shaders = gsdxIni.Read("shaderfx", "Settings");

			var uiIni = GetUiIniFile(game);
			var _zoom = uiIni.Read("Zoom", "GSWindow");
			var _aspect = uiIni.Read("AspectRatio", "GSWindow");

			return new GameConfig
			{
				GameName = game,
				IsoPath = _isodir,
				NoGui = Convert.ToBoolean(_nogui),
				Fullscreen = Convert.ToBoolean(_fullscreen),
				Fullboot = Convert.ToBoolean(_fullboot),
				NoHacks = Convert.ToBoolean(_nohacks),
				Widescreen = Convert.ToBoolean(_widescreen),
				CustomShaders = Convert.ToBoolean(_shaders),
				Zoom = Convert.ToDecimal(_zoom),
				AspectRatio = (AspectRatio)Enum.Parse(typeof(AspectRatio), _aspect)
			};
		}

		public static void SaveConfig(GameConfig config)
		{
			var specIni = GetSpectabisIniFile(config.GameName);

			specIni.Write("nogui", config.NoGui.ToString(), "Spectabis");
			specIni.Write("fullscreen", config.Fullscreen.ToString(), "Spectabis");
			specIni.Write("fullboot", config.Fullboot.ToString(), "Spectabis");
			specIni.Write("nohacks", config.NoHacks.ToString(), "Spectabis");
			specIni.Write("isoDirectory", config.IsoPath, "Spectabis");

			var vmIni = GetVmIniFile(config.GameName);

			vmIni.Write("EnableWideScreenPatches", config.Widescreen.ToString(), "EmuCore");

			var gsdxIni = GetGsdxIniFile(config.GameName);

			gsdxIni.Write("shaderfx", config.CustomShaders.ToString(), "Settings");

			if(config.CustomShaders)
			{

			}

			var uiIni = GetUiIniFile(config.GameName);

			uiIni.Write("Zoom", config.Zoom.ToString(), "GSWindow");
			uiIni.Write("AspectRatio", config.AspectRatio.ToString(), "GSWindow");
		}

		private static string CreateSpectabisIniPath(string game)
		{
			return Path.Combine(configDir, game, "spectabis.ini");
		}

		private static IniFile GetSpectabisIniFile(string game)
		{
			var iniPath = CreateSpectabisIniPath(game);
			return new IniFile(iniPath);
		}

		private static string CreateVmIniPath(string game)
		{
			return Path.Combine(configDir, game, "PCSX2_vm.ini");
		}

		private static IniFile GetVmIniFile(string game)
		{
			var iniPath = CreateVmIniPath(game);
			return new IniFile(iniPath);
		}

		private static string CreateGsdxIniPath(string game)
		{
			return Path.Combine(configDir, game, "GSdx.ini");
		}

		private static IniFile GetGsdxIniFile(string game)
		{
			var iniPath = CreateGsdxIniPath(game);
			return new IniFile(iniPath);
		}

		private static string CreateUiIniPath(string game)
		{
			return Path.Combine(configDir, game, "PCSX2_ui.ini");
		}

		private static IniFile GetUiIniFile(string game)
		{
			var iniPath = CreateUiIniPath(game);
			return new IniFile(iniPath);
		}
	}

	public class GameConfig
	{
		public string GameName { get; set; }
		public string IsoPath { get; set; }
		public bool NoGui { get; set; }
		public bool Fullscreen { get; set; }
		public bool Fullboot { get; set; }
		public bool NoHacks { get; set; }

		public bool Widescreen { get; set; }

		public bool CustomShaders { get; set; }

		public decimal Zoom { get; set; }
		public AspectRatio AspectRatio { get; set; }
	}
}
