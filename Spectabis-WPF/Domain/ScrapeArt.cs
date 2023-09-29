using Spectabis_WPF.Domain.Scraping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Diagnostics;
using Spectabis_WPF.Properties;

namespace Spectabis_WPF.Domain {

    public enum ScraperType
	{
        GiantBomb = 0,
        TheGamesDbApi = 1,
        IGDB = 2,
        TheGamesDbHtml = 3,
        MobyGames = 4
	}

    public class ScrapeArt {
        private static readonly string BaseDirectory = App.BaseDirectory;

        public GameInfoModel Result;

        public static Dictionary<ScraperType, IScraperApi> Scrapers = new Dictionary<ScraperType, IScraperApi>{
			{ ScraperType.GiantBomb, new Scraping.Api.GiantBombApi() },
            { ScraperType.TheGamesDbApi, new Scraping.Api.TheGamesDbApi() },
            { ScraperType.IGDB, new Scraping.Api.IGDBApi() },
            { ScraperType.TheGamesDbHtml, new Scraping.Api.TheGamesDbHtml() },
			{ ScraperType.MobyGames, new Scraping.Api.MobyGamesApi() }
		};

	    private class PerformanceStat {
		    public int Failures { get; set; }
		    public List<float> Milliseconds { get; } = new List<float>();
		    public float AverageMs => Milliseconds.Average();
	    }
		private static readonly Dictionary<ScraperType, PerformanceStat> ApiPerformanceStats = new Dictionary<ScraperType, PerformanceStat>();

        public ScrapeArt(string title) {
            var order = new[] {
			        Settings.Default.APIUserSequence,
			        Settings.Default.APIAutoSequence
		        }
				.First(p=>string.IsNullOrEmpty(p) == false)
				.Split(',')
		        .Select(int.Parse)
                .Select(x => (ScraperType)x)
		        .ToList();

            foreach(var i in order)
			{
                var scraper = Scrapers[i];

                if (ApiPerformanceStats.ContainsKey(i) == false)
                    ApiPerformanceStats[i] = new PerformanceStat();

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    Result = scraper.GetDataFromApi(title);
                }
                catch (Exception e)
                {
                    File.AppendAllText(Path.Combine(BaseDirectory, "resources", "logs", "ScrapeError.log"),
                        "[" + DateTime.Now + "] " + scraper.GetType().FullName + "\r\nError: " +
                        e.Message + "\r\n" + e.StackTrace + "\r\n\r\n");
                    Result = null;
                }
                if (Result?.ThumbnailUrl == null)
                {
                    stopwatch.Stop();
                    ApiPerformanceStats[i].Milliseconds.Add(stopwatch.ElapsedMilliseconds);
                    ApiPerformanceStats[i].Failures++;
                    continue;
                }

                var downloadSuccess = SaveImageFromUrl(title, Result.ThumbnailUrl);
                if (downloadSuccess == false)
                {
                    stopwatch.Stop();
                    ApiPerformanceStats[i].Milliseconds.Add(stopwatch.ElapsedMilliseconds);
                    ApiPerformanceStats[i].Failures++;
                    continue;
                }
                stopwatch.Stop();
                ApiPerformanceStats[i].Milliseconds.Add(stopwatch.ElapsedMilliseconds);
                return;
            }

			Console.WriteLine("All APIs failed");
        }

        public static string WebClient(string url, Func<WebClient, string> f) {
            using (var req = new WebClient()) {
                req.BaseAddress = url;
                try {
                    return f(req);
                }
                catch {
                    if (Debugger.IsAttached)
                        throw;
                }
            }
            return null;
        }

        public static T WebClient<T>(string url, Func<WebClient, string> f) {
            var resp = WebClient(url, f);
            if (resp == null)
                return default(T);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(resp);
        }

        //Download art to game profile from an image link
        public static bool SaveImageFromUrl(string fileName, string imgUrl) {
            using (new SecureTLSDumbfuckery())
            using (var client = new WebClient()) {
                client.Headers.Add("user-agent", "PCSX2 Spectabis frontend");

                try {
                    //Download image to temp folder and copy to profile folder
                    client.DownloadFile(imgUrl, Path.Combine(BaseDirectory, "resources", "_temp", fileName + ".jpg"));
                    File.Copy(
                        Path.Combine(BaseDirectory, "resources", "_temp", fileName + ".jpg"),
                        Path.Combine(BaseDirectory, "resources", "configs", fileName, "art.jpg"),
                        true
                    );
                    return true;
                }
                catch {
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    Console.WriteLine("Failed to download boxart.");
                    return false;
                }
            }
        }

        public static string EncryptApiKey(string plain) {
            var random = new Random(38946897);
            var s1 = new string(plain.OrderBy(p=>random.Next()).ToArray());
            var s2 = System.Text.Encoding.UTF8.GetBytes(s1);
            var s3 = System.Convert.ToBase64String(s2);
            return s3;
        }

        public static string DecryptApiKey(string encrypted) {
            var random = new Random(38946897);
            var s3 = System.Convert.FromBase64String(encrypted);
            var s2 = System.Text.Encoding.UTF8.GetString(s3);
            var seq = Enumerable.Range(0, s2.Length)
                .OrderBy(p=>random.Next())
                .ToArray();
            var s1 = new char[s2.Length];
            for (var i = 0; i < s1.Length; i++)
                s1[seq[i]] = s2[i];
            return new string(s1);
        }
    }
}
