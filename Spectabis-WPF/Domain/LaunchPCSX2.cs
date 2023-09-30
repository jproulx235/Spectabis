using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Spectabis_WPF.Domain
{
    class LaunchPCSX2
    {
        public static Process CreateGameProcess(string game, bool launchAndTerminate = false)
        {
            GameConfig config = SpectabisConfig.ReadConfig(game);
            
            string _launchargs = "";

            if (config.NoGui)
                _launchargs = "--nogui ";
            if (config.Fullscreen)
                _launchargs += "--fullscreen ";
            if (config.Fullboot)
                _launchargs += "--fullboot ";
            if (config.NoHacks)
                _launchargs += "--nohacks ";

            Console.WriteLine($"{_launchargs} {config.IsoPath} --cfgpath={SpectabisFilePath.GetGameConfigDirectoryPath(game)}");

            Process PCSX = new Process();

            //PCSX2 Process
            if(File.Exists(Properties.Settings.Default.EmuExePath))
            {
                var argument = $"{_launchargs} \"{config.IsoPath}\" --cfgpath=\"{SpectabisFilePath.GetGameConfigDirectoryPath(game)}\"";

                if(config.IsoPath.EndsWith(".ELF") || config.IsoPath.EndsWith(".elf"))
                {
                    argument = $"--elf=\"{config.IsoPath}\" --cfgpath=\"{SpectabisFilePath.GetGameConfigDirectoryPath(game)}\"";
                }

                PCSX.StartInfo.FileName = Properties.Settings.Default.EmuExePath;
                PCSX.StartInfo.Arguments = argument;

                if(launchAndTerminate)
                {
                    PCSX.Start();
                    Application.Current.Shutdown();
                }

            }
            else
            {
                Console.WriteLine(Properties.Settings.Default.EmuExePath + " does not exist!");
            }

            return PCSX;
        }

        public static Process CreateFirstTimeWizard()
        {
            var process = new Process();
            process.StartInfo.FileName = Properties.Settings.Default.EmuExePath;
            process.StartInfo.Arguments = $"--forcewiz --nogui --cfgpath=\"{SpectabisFilePath.DefaultConfigDirectoryPath}\"";

            return process;
        }
    }
}
