using Spectabis_WPF.Enums;
using System;
using System.ComponentModel;
using System.IO;

namespace Spectabis_WPF.Domain
{
    public static class GameProfile
    {
        //Creates a folder and a blank file for Global Controller settings
        public static void CreateGlobalController()
        {
            if (File.Exists(SpectabisFilePath.GlobalControllerLilyPadIniFilePath) == false)
            {
                Directory.CreateDirectory(SpectabisFilePath.GlobalControllerDirectoryPath);
                File.Create(SpectabisFilePath.GlobalControllerLilyPadIniFilePath);
                Console.WriteLine("Created global controller profile file");
            }
        }

        //Copy global controller profile to a game profile
        public static void CopyGlobalProfile(string game)
        {
            if(Properties.Settings.Default.GlobalController == true)
            {
                Console.WriteLine("Global settings copied to " + game);
                File.Copy(SpectabisFilePath.GlobalControllerLilyPadIniFilePath, SpectabisFilePath.GetGameLilyPadSettingsIniFilePath(game), true);
            }
            else
            {
                Console.WriteLine("Global settings are not copied to " + game);
            }
        }

        //Creates a game profile and returns the created title, because of indexation
        public static string Create(string _isoDir, string _title)
        {
            //sanitize game's title for folder creation
            Console.WriteLine("Sanitizing Game Title");
            _title = _title.ToSanitizedString();

            //Create a folder for profile and add an index, if needed
            if (getIndex(SpectabisFilePath.GetGameConfigDirectoryPath(_title)) != 0)
            {
                _title = _title + " (" + getIndex(SpectabisFilePath.GetGameConfigDirectoryPath(_title)) + ")";
            }

            Directory.CreateDirectory(SpectabisFilePath.GetGameConfigDirectoryPath(_title));

            if (Directory.Exists(SpectabisFilePath.DefaultConfigDirectoryPath))
            {
                Console.WriteLine("Copying initial game configuration from default_config");

                string[] files = Directory.GetFiles(SpectabisFilePath.DefaultConfigDirectoryPath);

                foreach(var file in files)
                {
                    string _destinationPath = Path.Combine(SpectabisFilePath.GetGameConfigDirectoryPath(_title), Path.GetFileName(file));
                    File.Copy(file, _destinationPath);
                }
            }
            else  // Legacy game configuration
            {
                Console.WriteLine("Using legacy game configuration");

                if (Directory.Exists(Properties.Settings.Default.EmuDir + @"\inis\"))
                {
                    string[] inisDir = Directory.GetFiles(Properties.Settings.Default.EmuDir + @"\inis\");
                    foreach (string inifile in inisDir)
                    {
                        Console.WriteLine(inifile + " found!");
                        if (File.Exists(Path.Combine(SpectabisFilePath.GetGameConfigDirectoryPath(_title), Path.GetFileName(inifile))) == false)
                        {
                            string _destinationPath = Path.Combine(SpectabisFilePath.GetGameConfigDirectoryPath(_title), Path.GetFileName(inifile));
                            File.Copy(inifile, _destinationPath);
                        }
                    }
                }
                else
                {
                    string[] inisDirDoc = Directory.GetFiles((Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\PCSX2\inis"));
                    foreach (string inifile in inisDirDoc)
                    {
                        if (!File.Exists(Path.Combine(SpectabisFilePath.GetGameConfigDirectoryPath(_title), Path.GetFileName(inifile))))
                        {
                            string _destinationPath = Path.Combine(SpectabisFilePath.GetGameConfigDirectoryPath(_title), Path.GetFileName(inifile));
                            File.Copy(inifile, _destinationPath);
                        }
                    }

                }
            }

            GameConfig config = new GameConfig
            {
                GameName = _title,
                IsoPath = _isoDir,
                NoGui = true,
                Fullscreen = true,
                Fullboot = true,
                NoHacks = true,
                Widescreen = false,
                Zoom = 100,
                AspectRatio = AspectRatio.wide_16_9,
                CustomShaders = false
            };

            SpectabisConfig.SaveConfig(config);

            // Copy the placeholder before downloading the real art
            Properties.Resources.tempArt.Save(SpectabisFilePath.PlaceholderBoxArtFilePath);
            File.Copy(SpectabisFilePath.PlaceholderBoxArtFilePath, SpectabisFilePath.GetGameBoxArtFilePath(_title), true);

            return _title;
        }

        

        public static void Delete(string _title)
        {
            if(Directory.Exists(SpectabisFilePath.GetGameConfigDirectoryPath(_title)))
            {
                Directory.Delete(SpectabisFilePath.GetGameConfigDirectoryPath(_title), true);
            }
        }

        public static string Rename(string _in, string _out)
        {

            string input = SpectabisFilePath.GetGameConfigDirectoryPath(_in);
            string output = SpectabisFilePath.GetGameConfigDirectoryPath(_out);

            if (Directory.Exists(input))
            {
                if (getIndex(output) != 0)
                {
                    output = output + " (" + getIndex(output) + ")";
                    Directory.Move(input, output);
                    return _out + " (" + getIndex(output) + ")";
                }

                Directory.Move(input, output);
                
                return _out;
            }

            return null;
        }

        private static int getIndex(string _dir)
        {
            var index = 0;

            //Enumerate folder index
            a: if (Directory.Exists(_dir))
            {
                index++;
                _dir = _dir + " (" + index + ")";
                goto a;
            }

            return index;
        }

    }
}
