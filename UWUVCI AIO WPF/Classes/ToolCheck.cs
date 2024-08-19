using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace UWUVCI_AIO_WPF.Classes
{
    class ToolCheck
    {
        static readonly string FolderName = "bin\\Tools";
        public static readonly string BackupUrl = @"https://github.com/Hotbrawl20/UWUVCI-Tools/raw/master/";
        public static string[] ToolNames =
        {
            "CDecrypt.exe", "CNUSPACKER.exe", "N64Converter.exe", "png2tga.exe", "psb.exe", "RetroInject.exe",
            "tga_verify.exe", "wiiurpxtool.exe", "INICreator.exe", "blank.ini", "FreeImage.dll", "BuildPcePkg.exe",
            "BuildTurboCdPcePkg.exe", "goomba.gba", "nfs2iso2nfs.exe", "nintendont.dol", "nintendont_force.dol",
            "GetExtTypePatcher.exe", "wit.exe", "cygwin1.dll", "cygz.dll", "cyggcc_s-1.dll", "NintendontConfig.exe",
            "BASE.zip", "tga2png.exe", "iconTex.tga", "wii-vmc.exe", "bootTvTex.png", "ConvertToISO.exe", "NKit.dll",
            "SharpCompress.dll", "NKit.dll.config", "sox.exe", "jpg2tga.exe", "bmp2tga.exe", "ConvertToNKit.exe",
            "wglp.exe", "font.otf", "ChangeAspectRatio.exe", "font2.ttf", "forwarder.dol", "gba1.zip", "gba2.zip", "c2w_patcher.exe"
        };

        public static bool DoesToolsFolderExist() => Directory.Exists(FolderName);

        public static async Task<bool> IsToolRightAsync(string name)
        {
            string md5 = string.Empty;
            try
            {
                string md5FilePath = $"{name}.md5";
                using WebClient client = new WebClient();
                await client.DownloadFileTaskAsync($"{BackupUrl}{name}.md5", md5FilePath);

                md5 = File.ReadAllText(md5FilePath);
                File.Delete(md5FilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while verifying tool: {ex.Message}");
                return false;
            }

            return CalculateMD5(name) == md5;
        }

        private static string CalculateMD5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
        }

        public static List<MissingTool> CheckForMissingTools() =>
            new List<MissingTool>(
                ToolNames
                    .Where(tool => !DoesToolExist(Path.Combine(FolderName, tool)))
                    .Select(tool => new MissingTool(tool, Path.Combine(FolderName, tool)))
            );

        private static bool DoesToolExist(string path)
        {
            if (!File.Exists(path)) return false;

            if (path.EndsWith("gba1.zip", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("gba2.zip", StringComparison.OrdinalIgnoreCase))
            {
                ExtractZipIfRequired(path);
            }

            return true;
        }

        private static void ExtractZipIfRequired(string path)
        {
            string[] requiredFiles = { "MArchiveBatchTool.exe", "ucrtbase.dll" };
            if (!requiredFiles.All(file => File.Exists(Path.Combine(FolderName, file))))
            {
                try
                {
                    ZipFile.ExtractToDirectory(path, FolderName);
                }
                catch (Exception)
                {
                    Thread.Sleep(200);
                    DoesToolExist(path); // Retry extraction
                }
            }
        }
    }

    class MissingTool
    {
        public string Name { get; }
        public string Path { get; }

        public MissingTool(string name, string path)
        {
            Name = name;
            Path = new FileInfo(path).FullName;
        }
    }
}
