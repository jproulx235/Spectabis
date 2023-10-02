using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spectabis_WPF.Domain
{
	public static class SpectabisFilePath
	{
		public static string ResourcesDirectoryPath => Path.Combine(App.BaseDirectory, "resources");
		public static string ConfigDirectoryPath => Path.Combine(ResourcesDirectoryPath, "configs");
		public static string TempDirectoryPath => Path.Combine(ResourcesDirectoryPath, "_temp");
		public static string InisDirectoryPath => Path.Combine(App.BaseDirectory, "inis");
		public static string GsdxIniFilePath => Path.Combine(InisDirectoryPath, "GHSdx.ini");
		public static string Spu2xIniFilePath => Path.Combine(InisDirectoryPath, "SPU2-X.ini");
		public static string LilyPadIniFilePath => Path.Combine(InisDirectoryPath, "SPU2-X.ini");

		public static string PlaceholderBoxArtFilePath => Path.Combine(App.BaseDirectory, "resources", "_temp", "art.jpg");

		public static string GlobalControllerDirectoryPath => Path.Combine(ConfigDirectoryPath, "#global_controller");
		public static string GlobalControllerLilyPadIniFilePath => Path.Combine(GlobalControllerDirectoryPath, "LilyPad.ini");


		public static string GetGameConfigDirectoryPath(string game)
		{
			return Path.Combine(ConfigDirectoryPath, game);
		}

		public static string GetGameBoxArtFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "art.jpg");
		}

		public static string GetGameSpectabisIniFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "spectabis.ini");
		}

		public static string GetGameGSdxfxFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "GSdx.fx");
		}

		public static string GetGameGSdxIniFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "GSdx.ini");
		}

		public static string GetGameGSdxfxSettingsIniFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "GSdx_FX_Settings.ini");
		}

		public static string GetGameUiSettingsIniFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "PCSX2_ui.ini");
		}

		public static string GetGameVmSettingsIniFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "PCSX2_vm.ini");
		}

		public static string GetGameLilyPadSettingsIniFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "LilyPad.ini");
		}

		public static string GetGameSpu2xSettingsIniFilePath(string game)
		{
			return Path.Combine(GetGameConfigDirectoryPath(game), "SPU2-X.ini");
		}



		public static string DefaultConfigDirectoryPath => Path.Combine(App.BaseDirectory, "resources", "default_config");

		public static string DefaultLilyPadSettingsIniFilePath => Path.Combine(DefaultConfigDirectoryPath, "LilyPad.ini");

		public static string DefaultUiSettingsIniFilePath => Path.Combine(DefaultConfigDirectoryPath, "PCSX2_ui.ini");

		public static string DefaultVmSettingsIniFilePath => Path.Combine(DefaultConfigDirectoryPath, "PCSX2_vm.ini");


		public static string EmulatorGSdxfxFilePath => Path.Combine(Properties.Settings.Default.EmuDir, "shaders", "GSdx.fx");

		public static string EmulatorGSdxfxSettingsIniFilePath => Path.Combine(Properties.Settings.Default.EmuDir, "shaders", "GSdx_FX_Settings.ini");
	}
}
