using System;
using System.Linq;
using System.Net.Http;

namespace YS_Downloader.Utils
{
    public class ApiLoader
    {
        /**
         * 获取启动器api
         * @param serverName CN/OS
         */
        public string GetLauncherApi(string serverName)
        {
            string markdownUrl = "";
            string launcherApi = string.Empty;
            switch (serverName)
            {
                case "CN":
                    markdownUrl = "https://raw.githubusercontent.com/MAnggiarMustofa/GI-Download-Library/main/Yuanshen/Client/Latest.md";
                    break;
                case "OS":
                    markdownUrl = "https://raw.githubusercontent.com/MAnggiarMustofa/GI-Download-Library/main/GenshinImpact/Client/Latest.md";
                    break;
                default:
                    markdownUrl = "https://raw.githubusercontent.com/MAnggiarMustofa/GI-Download-Library/main/Yuanshen/Client/Latest.md";
                    break;
            }

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(markdownUrl).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                string markdownContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                string[] lines = markdownContent.Split(Environment.NewLine.ToCharArray()).Where(s => !string.IsNullOrEmpty(s)).ToArray();

                string line = lines[0];
#if DEBUG
                Console.WriteLine(line);
#endif
                if (!string.IsNullOrEmpty(line))
                {
                    int startIndex = line.IndexOf("(") + 1;
                    int endIndex = line.IndexOf(")");
                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        launcherApi = line.Substring(startIndex, endIndex - startIndex);
                    }
                }
            }
            return launcherApi;
        }
        /**
         * 获取最新版本号
         * @param apiUrl 之前获取到的启动器api
         */
        public string GetLatestVersion(string apiUrl)
        {
            string version = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(apiUrl).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                dynamic downloadInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                if (downloadInfo.retcode == 0)
                {
                    version = downloadInfo.data.game.latest.version;
                }
            }
            return version;
        }
        /**
         * 获取解压文件下载api
         * @param apiUrl 之前获取到的启动器api
         */
        public string GetDownloadApi(string apiUrl)
        {
            string downloadApi = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(apiUrl).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                dynamic downloadInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                if (downloadInfo.retcode == 0)
                {
                    downloadApi = downloadInfo.data.game.latest.decompressed_path;
                }
            }
            return downloadApi;
        }
        public string GetFileInfoList(string downloadApi)
        {
            string downloadUrl = downloadApi + "/pkg_version";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(downloadUrl).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }
    }
}