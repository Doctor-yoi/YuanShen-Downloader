using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

namespace YS_Downloader.Utils
{
    public class Parser
    {
        [Obsolete]
        public static string[] ReadFileFromPath(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            return lines;
        }

        public static string[] ParseContent(string fileContent)
        {
            string[] lines = fileContent.Split("\r\n".ToCharArray()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            return lines;
        }

        public static string[] GetFileUrl(string[] jsonStrings, string url)
        {
            List<string> result = new List<string>();

            foreach (string jsonString in jsonStrings)
            {
                dynamic json = JsonConvert.DeserializeObject(jsonString);
                string remoteName = json.remoteName;
                string completeUrl = $"{url}/{remoteName}";

                result.Add(completeUrl);
            }

            return result.ToArray();
        }
        public static string[] GetFilePath(string[] jsonStrings, string folder)
        {
            List<string> result = new List<string>();

            foreach (string jsonString in jsonStrings)
            {
                dynamic json = JsonConvert.DeserializeObject(jsonString);
                string remoteName = json.remoteName;
                string completePath = $"{folder}/{remoteName}";

                result.Add(completePath);
            }

            return result.ToArray();
        }

        public static string[] GetFileSavePath(string[] FilePathList)
        {
            List<string> result = new List<string>();

            foreach (string FilePath in FilePathList)
            {
                string fileSavePath = Path.GetDirectoryName(FilePath);

                result.Add(fileSavePath);
            }
            return result.ToArray();
        }

        public static string[] GetFileNameList(string[] FilePathList)
        {
            List<string> result = new List<string>();

            foreach (string FilePath in FilePathList)
            {
                string fileName = Path.GetFileName(FilePath);

                result.Add(fileName);
            }
            return result.ToArray();
        }
    }
}
