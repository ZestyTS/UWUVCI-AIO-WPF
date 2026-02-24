using GameBaseClassLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UWUVCI_AIO_WPF.Models;

namespace UWUVCI_AIO_WPF.Classes
{
    class KeyFile
    {
        public static List<TKeys> ReadBasesFromKeyFile(string keyPath)
        {
            List<TKeys> result = new List<TKeys>();

            try
            {
                string resolvedPath = ResolveToAppRootPath(keyPath);
                FileInfo fileInfo = new FileInfo(resolvedPath);
                if (fileInfo.Extension.Contains("vck"))
                {
                    using (FileStream inputConfigStream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read))
                    using (GZipStream decompressedConfigStream = new GZipStream(inputConfigStream, CompressionMode.Decompress))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        result = (List<TKeys>)formatter.Deserialize(decompressedConfigStream);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the error appropriately
                Console.WriteLine($"An error occurred while reading the key file: {ex.Message}");
            }

            return result;
        }

        public static void ExportFile(List<TKeys> keys, GameConsoles console)
        {
            try
            {
                string folderPath = Path.Combine(MainViewModel.AppPaths.AppRoot, "bin", "keys");
                CheckAndFixFolder(folderPath);

                string filePath = Path.Combine(folderPath, $"{console.ToString().ToLower()}.vck");

                using (FileStream createConfigStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (GZipStream compressedStream = new GZipStream(createConfigStream, CompressionMode.Compress))
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(compressedStream, keys);
                }
            }
            catch (Exception ex)
            {
                // Handle or log the error appropriately
                Console.WriteLine($"An error occurred while exporting the key file: {ex.Message}");
            }
        }

        private static string ResolveToAppRootPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(MainViewModel.AppPaths.AppRoot, path);
        }

        private static void CheckAndFixFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
        }
    }
}
