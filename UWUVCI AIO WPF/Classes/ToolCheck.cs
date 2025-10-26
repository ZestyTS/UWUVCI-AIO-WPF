using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UWUVCI_AIO_WPF.Helpers;

namespace UWUVCI_AIO_WPF.Classes
{
    class ToolCheck
    {
        static string FolderName = "bin\\Tools";
        public static string backupulr = @"https://github.com/Hotbrawl20/UWUVCI-Tools/raw/master/";
        public static string[] ToolNames =
        {
            "N64Converter.exe",
            "png2tga.exe",
            "psb.exe",
            "RetroInject.exe",
            "tga_verify.exe",
            "wiiurpxtool.exe",
            "INICreator.exe",
            "blank.ini",
            "FreeImage.dll",
            "BuildPcePkg.exe",
            "BuildTurboCdPcePkg.exe",
            "goomba.gba",
            "nfs2iso2nfs.exe",
            "nintendont.dol",
            "nintendont_force.dol",
            "GetExtTypePatcher.exe",
            "wit.exe",
            "wit-mac",
            "wit-linux",
            "wstrt.exe",
            "wstrt-mac",
            "wstrt-linux",
            "cygwin1.dll",
            "cygz.dll",
            "cyggcc_s-1.dll",
            "BASE.zip",
            "tga2png.exe",
            "iconTex.tga",
            "wii-vmc.exe",
            "bootTvTex.png",
            "ConvertToISO.exe",
            "NKit.dll",
            "SharpCompress.dll",
            "NKit.dll.config",
            "sox.exe",
            "jpg2tga.exe",
            "bmp2tga.exe",
            "ConvertToNKit.exe",
            "wglp.exe",
            "font.otf",
            "ChangeAspectRatio.exe",
            "font2.ttf",
            "forwarder.dol",
            "gba1.zip",
            "gba2.zip",
            "c2w_patcher.exe",
            "DSLayoutScreens.zip",
            "cygcrypto-1.1.dll",
            "cygncursesw-10.dll"
        };

        public static bool DoesToolsFolderExist()
        {
            try
            {
                return Directory.Exists(FolderName);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> IsToolRightAsync(string name)
        {
            try
            {
                string md5Url = backupulr + name + ".md5";
                string expectedHash;

                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(10);
                    expectedHash = (await http.GetStringAsync(md5Url)).Trim();
                }

                string localHash = await CalculateMD5Async(name);

                bool match = string.Equals(expectedHash, localHash, StringComparison.OrdinalIgnoreCase);
                if (!match)
                    Logger.Log($"MD5 mismatch for {name}: expected {expectedHash}, got {localHash}");

                return match;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error verifying MD5 for {name}: {ex.Message}");
                return false;
            }
        }
        public static async Task<string> CalculateMD5Async(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true);
            byte[] hash = await Task.Run(() => md5.ComputeHash(stream)); // compute on threadpool
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static List<MissingTool> CheckForMissingTools()
        {
            List<MissingTool> missingTools = new List<MissingTool>();

            foreach (string toolName in ToolNames)
            {
                string path = Path.Combine(FolderName, toolName);

                // Check if the tool exists and has the right MD5 hash
                if (!DoesToolExist(path))
                    missingTools.Add(new MissingTool(toolName, path));
            }

            return missingTools;
        }


        private static bool DoesToolExist(string path, int retryCount = 0)
        {
            const int MaxRetries = 3;  // Define a maximum number of retries

            if (!File.Exists(path))
                return false;

            if (path.ToLower().Contains("gba1.zip") || path.ToLower().Contains("gba2.zip"))
            {
                if (!File.Exists(Path.Combine(FolderName, "MArchiveBatchTool.exe")) || !File.Exists(Path.Combine(FolderName, "ucrtbase.dll")))
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(path, FolderName);
                    }
                    catch (Exception)
                    {
                        if (retryCount < MaxRetries)
                        {
                            Thread.Sleep(200);
                            return DoesToolExist(path, retryCount + 1);  // Recursively retry
                        }
                        else
                        {
                            Console.WriteLine($"Failed to extract {path} after {MaxRetries} attempts.");
                            return false;
                        }
                    }
                }
            }
            return true;
        }

    }

    public class MissingTool
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public MissingTool(string n, string p)
        {
            Name = n;
            FileInfo f = new FileInfo(p);
            Path = f.FullName;
        }
    }
}
