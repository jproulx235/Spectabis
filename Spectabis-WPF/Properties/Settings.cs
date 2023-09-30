using Newtonsoft.Json;
using System;
using System.IO;

namespace Spectabis_WPF.Properties {
    public sealed class Settings {
         
        private static Settings DefaultSettings() {
            return new Settings {
                ShowTitle = true,
                DoubleClick = true,
                EmuExePath = null,
                NightMode = false,
                AutoBoxart = true,
                Swatch = "bluegrey",
                GameDirectory = null,
                Searchbar = true,
                Tooltips = false,
                Aprilfooled = false,
                Checkupdates = true,
                TitleAsFile = false,
                GlobalController = false,
                Playtime = true,
                APIKey_GiantBomb = "d6356ce53a374e55b91d8018f5a59d43",
                APIKey_MobyGames = "8830485ffd8241f1a316db413f1806f4",
                APIKey_IGDB = "2cf5bede3f8c4eca9f55958c73b58eb6",
                APIKey_TheGamesDb = "e79660b6dd1245d4876a2e983ea7f5ab",
                APIAutoSequence = "0,1,2,3,4",
                APIUserSequence = ""
            };
        }

        public bool ShowTitle { get; set; }
        public bool DoubleClick { get; set; }
        public string EmuExePath { get; set; }
        public string EmuDir => Path.GetDirectoryName(EmuExePath);
        public bool NightMode { get; set; }
        public bool AutoBoxart { get; set; }
        public string Swatch { get; set; }
        public string GameDirectory { get; set; }
        public bool Searchbar { get; set; }
        public bool Tooltips { get; set; }
        public bool Aprilfooled { get; set; }
        public bool Checkupdates { get; set; }
        public bool TitleAsFile { get; set; }
        public bool GlobalController { get; set; }
        public bool Playtime { get; set; }
        public string APIKey_GiantBomb { get; set; }
        public string APIKey_MobyGames { get; set; }
        public string APIKey_IGDB { get; set; }
        public string APIKey_TheGamesDb { get; set; }
        public string APIAutoSequence { get; set; }
        public string APIUserSequence { get; set; }

        private static string DefaultSaveFile => Path.Combine(App.BaseDirectory, "spectabis.json");
        private static Settings _defaultInstance = null;

        public static Settings Default {
            get {
                if (_defaultInstance != null)
                    return _defaultInstance;

                Settings settings;
                if (File.Exists(DefaultSaveFile) == false)
                    settings = DefaultSettings();
                else {
                    var str = File.ReadAllText(DefaultSaveFile);
                    settings = JsonConvert.DeserializeObject<Settings>(str);
                }
                _defaultInstance = settings;
                return settings;
            }
        }

        // TODO UAC
        public void Save() {
            _defaultInstance = this;
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(DefaultSaveFile, str);
        }
    }
}
