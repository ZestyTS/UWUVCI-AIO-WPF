﻿using GameBaseClassLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using UWUVCI_AIO_WPF.Classes;
using UWUVCI_AIO_WPF.UI.Windows;
using Newtonsoft.Json;
using MessageBox = System.Windows.MessageBox;
using UWUVCI_AIO_WPF.Models;
using WiiUDownloaderLibrary.Models;
using WiiUDownloaderLibrary;
using Newtonsoft.Json.Linq;
using UWUVCI_AIO_WPF.Helpers;
using System.Diagnostics.Eventing.Reader;

namespace UWUVCI_AIO_WPF
{
    public static class StringExtensions
    {
        public static string ToHex(this string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
                sb.AppendFormat("{0:X2}", (int)c);
            return sb.ToString().Trim();
        }
    }
    internal static class Injection
    {
        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);
        [DllImport("user32.dll")]
        public static extern int SendMessage(
            int hWnd,     // handle to destination window
            uint Msg,      // message
            long wParam,   // first message parameter
            long lParam    // second message parameter
        );
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, int Msg, System.Windows.Forms.Keys wParam, int lParam);
        private static int WM_KEYUP = 0x101;
        private static readonly string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "temp");
        private static readonly string baseRomPath = Path.Combine(tempPath, "baserom");
        private static readonly string imgPath = Path.Combine(tempPath, "img");
        private static readonly string toolsPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Tools");
        static string code = null;
        static MainViewModel mvvm;
        private static bool IsNativeWindows = !MacLinuxHelper.IsRunningUnderWineOrSimilar();

        /*
         * GameConsole: Can either be NDS, N64, GBA, NES, SNES or TG16
         * baseRom = Name of the BaseRom, which is the folder name too (example: Super Metroid EU will be saved at the BaseRom path under the folder SMetroidEU, so the BaseRom is in this case SMetroidEU).
         * customBasePath = Path to the custom Base. Is null if no custom base is used.
         * injectRomPath = Path to the Rom to be injected into the Base Game.
         * bootImages = String array containing the paths for
         *              bootTvTex: PNG or TGA (PNG gets converted to TGA using UPNG). Needs to be in the dimensions 1280x720 and have a bit depth of 24. If null, the original BootImage will be used.
         *              bootDrcTex: PNG or TGA (PNG gets converted to TGA using UPNG). Needs to be in the dimensions 854x480 and have a bit depth of 24. If null, the original BootImage will be used.
         *              iconTex: PNG or TGA (PNG gets converted to TGA using UPNG). Needs to be in the dimensions 128x128 and have a bit depth of 32. If null, the original BootImage will be used.
         *              bootLogoTex: PNG or TGA (PNG gets converted to TGA using UPNG). Needs to be in the dimensions 170x42 and have a bit depth of 32. If null, the original BootImage will be used.
         * gameName = The name of the final game to be entered into the .xml files.
         * iniPath = Only used for N64. Path to the INI configuration. If "blank", a blank ini will be used.
         * darkRemoval = Only used for N64. Indicates whether the dark filter should be removed.
         */
        static List<int> fiind(this byte[] buffer, byte[] pattern, int startIndex)
        {
            List<int> positions = new List<int>();
            int i = Array.IndexOf(buffer, pattern[0], startIndex);
            while (i >= 0 && i <= buffer.Length - pattern.Length)
            {
                byte[] segment = new byte[pattern.Length];
                Buffer.BlockCopy(buffer, i, segment, 0, pattern.Length);
                if (segment.SequenceEqual(pattern))
                    positions.Add(i);
                i = Array.IndexOf(buffer, pattern[0], i + 1);
            }
            return positions;
        }
        static void PokePatch(string rom)
        {
            byte[] search = { 0xD0, 0x88, 0x8D, 0x83, 0x42 };
            byte[] test;
            test = new byte[new FileInfo(rom).Length];
            using (var fs = new FileStream(rom,
                                 FileMode.Open,
                                 FileAccess.ReadWrite))
            {
                try
                {
                    fs.Read(test, 0, test.Length - 1);

                    var l = fiind(test, search, 0);
                    byte[] check = new byte[4];
                    fs.Seek(l[0] + 5, SeekOrigin.Begin);
                    fs.Read(check, 0, 4);

                    fs.Seek(0, SeekOrigin.Begin);
                    if (check[3] != 0x24)
                    {
                        fs.Seek(l[0] + 5, SeekOrigin.Begin);
                        fs.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0, 4);

                    }
                    else
                    {
                        fs.Seek(l[0] + 5, SeekOrigin.Begin);
                        fs.Write(new byte[] { 0x00, 0x00, 0x00 }, 0, 3);

                    }
                    check = new byte[4];
                    fs.Seek(l[1] + 5, SeekOrigin.Begin);
                    fs.Read(check, 0, 4);
                    fs.Seek(0, SeekOrigin.Begin);
                    if (check[3] != 0x24)
                    {
                        fs.Seek(l[1] + 5, SeekOrigin.Begin);
                        fs.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0, 4);

                    }
                    else
                    {
                        fs.Seek(l[1] + 5, SeekOrigin.Begin);
                        fs.Write(new byte[] { 0x00, 0x00, 0x00 }, 0, 3);

                    }
                }
                catch (Exception)
                {

                }


                fs.Close();
            }
        }
        private static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return string.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }
        [STAThread]
        public static bool Inject(GameConfig Configuration, string RomPath, MainViewModel mvm, bool force)
        {

            mvm.failed = false;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += tick;

            Clean();

            long freeSpaceInBytes = 0;
            if (!mvm.saveworkaround)
            {
                try
                {
                    long gamesize = new FileInfo(RomPath).Length;

                    var drive = new DriveInfo(tempPath);

                    done = true;
                    freeSpaceInBytes = drive.AvailableFreeSpace;
                }
                catch (Exception)
                {
                    mvm.saveworkaround = true;
                }

            }
            mvvm = mvm;

            Directory.CreateDirectory(tempPath);

            mvm.msg = "Checking Tools...";
            mvm.InjcttoolCheck();

            mvm.Progress = 5;

            mvm.msg = "Copying Base...";
            try
            {
                if (!mvm.saveworkaround && (Configuration.Console == GameConsoles.WII || Configuration.Console == GameConsoles.GCN))
                {
                    long neededspace = mvm.GC ? 10000000000 : 15000000000;

                    if (freeSpaceInBytes < neededspace)
                        throw new Exception("12G");
                }

                if (Configuration.BaseRom == null || Configuration.BaseRom.Name == null)
                {
                    throw new Exception("BASE");
                }
                if (Configuration.BaseRom.Name != "Custom")
                {
                    //Normal Base functionality here
                    CopyBase($"{Configuration.BaseRom.Name.Replace(":", "")} [{Configuration.BaseRom.Region}]", null);
                }
                else
                {
                    //Custom Base Functionality here
                    CopyBase($"Custom", Configuration.CBasePath);
                }
                if (!Directory.Exists(Path.Combine(baseRomPath, "code")) || !Directory.Exists(Path.Combine(baseRomPath, "content")) || !Directory.Exists(Path.Combine(baseRomPath, "meta")))
                {
                    throw new Exception("MISSINGF");
                }
                mvm.Progress = 10;
                mvm.msg = "Injecting ROM...";

                RunSpecificInjection(Configuration, (mvm.GC ? GameConsoles.GCN : Configuration.Console), RomPath, force, mvm);

                mvm.msg = "Editing XML...";
                EditXML(Configuration.GameName, mvm.Index, code);
                mvm.Progress = 90;
                mvm.msg = "Changing Images...";
                Images(Configuration);
                if (File.Exists(mvm.BootSound))
                {
                    mvm.Progress = 95;
                    mvm.msg = "Adding BootSound...";
                    bootsound(mvm.BootSound);
                }


                mvm.Progress = 100;


                code = null;
                return true;
            } catch (Exception e)
            {
                mvm.Progress = 100;

                code = null;
                if (e.Message == "Failed this shit")
                {
                    Clean();
                    return false;
                }

                var errorMessage = "Injection Failed due to unknown circumstances. Accent marks in the install path for UWUVCI or in the rom path is known to cause issues. Please contact us on the UWUVCI discord if you need any assistance.";

                if (e.Message == "MISSINGF")
                    errorMessage = "Injection Failed because there are base files missing. \nPlease redownload the base, or redump if you used a custom base!";
                else if (e.Message.Contains("Images"))
                    errorMessage = "Injection Failed due to wrong BitDepth, please check if your Files are in a different bitdepth than 32bit or 24bit\n\nIf the image/s that's being used is automatically grabbed for you, then don't use them." +
                        "\nFAQ: #28";
                else if (e.Message.Contains("Size"))
                    errorMessage = "Injection Failed due to Image Issues.Please check if your Images are made using following Information:\n\niconTex: \nDimensions: 128x128\nBitDepth: 32\n\nbootDrcTex: \nDimensions: 854x480\nBitDepth: 24\n\nbootTvTex: \nDimensions: 1280x720\nBitDepth: 24\n\nbootLogoTex: \nDimensions: 170x42\nBitDepth: 32";
                else if (e.Message.Contains("retro"))
                    errorMessage = "The ROM you want to Inject is to big for selected Base!\nPlease try again with different Base";
                else if (e.Message.Contains("BASE"))
                    errorMessage = "If you import a config you NEED to reselect a base";
                else if (e.Message.Contains("WII"))
                    errorMessage = $"{e.Message.Replace("Wii", "")}\nPlease make sure that your ROM isn't flawed and that you have atleast 12 GB of free Storage left.";
                else if (e.Message.Contains("12G"))
                    errorMessage = $"Please make sure to have atleast {FormatBytes(15000000000)} of storage left on the drive where you stored the Injector.";
                else if (e.Message.Contains("nkit"))
                    errorMessage = $"There is an issue with your NKIT.\nPlease try the original ISO, or redump your game and try again with that dump.";
                else if (e.Message.Contains("meta.xml"))
                    errorMessage = "Looks to be your meta.xml file isn't missing from your directory. If you downloaded your base, redownload it, if it's a custom base then the folder selected might be wrong or the layout is messed up.";
                else if (e.Message.Contains("pre.iso"))
                    errorMessage = "Looks to be that there is something about your game that UWUVCI doesn't like, you are most likely injecting with a wbfs or nkit.iso file, this file has data trimmed." +
                        "\nFAQ: #17, #27, #29";
                else if (e.Message.Contains("temp\\temp") || e.Message.Contains("temp/temp"))
                    errorMessage = "The images are most likely the culprit, try changing them around." +
                        "\nFAQ: #28";

                if (MacLinuxHelper.IsRunningInVirtualMachine() || MacLinuxHelper.IsRunningUnderWineOrSimilar())
                    errorMessage += "\n\nYou look to be running this under some form of emulation instead of a native Windows OS. There are external tools that UWUVCI uses which are not managed by the UWUVCI team. These external tools may be causing you issues and we will not be able to resolve your issues.";

                MessageBox.Show(errorMessage + "\n\nDon't forget that there's an FAQ in the ReadMe.txt file and on the UWUVCI Discord\n\nError Message:\n" + e.Message, "Injection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Log(e.Message);
                Clean();
                return false;
            }
            finally
            {

                mvm.Index = -1;
                mvm.LR = false;
                mvm.msg = "";

            }

        }

        private static bool done = false;
        private static void tick(object sender, EventArgs e)
        {
            if (!done)
                mvvm.failed = true;

            throw new Exception("Failed this shit");
        }

        public static void SendKey(IntPtr hWnd, System.Windows.Forms.Keys key)
        {
            PostMessage(hWnd, WM_KEYUP, key, 0);
        }
        static void bootsound(string sound)
        {
            string btsndPath = Path.Combine(baseRomPath, "meta", "bootSound.btsnd");
            FileInfo soundFile = new FileInfo(sound);
            if (soundFile.Extension.Contains("mp3") || soundFile.Extension.Contains("wav"))
            {
                // Convert input file to 6 second .wav
                using (Process sox = new Process())
                {
                    sox.StartInfo.UseShellExecute = false;
                    sox.StartInfo.CreateNoWindow = true;
                    sox.StartInfo.FileName = Path.Combine(toolsPath, "sox.exe");
                    sox.StartInfo.Arguments = $"\"{sound}\" -b 16 \"{Path.Combine(tempPath, "bootSound.wav")}\" channels 2 rate 48k trim 0 6";
                    sox.Start();
                    sox.WaitForExit();
                }
                //convert to btsnd
                wav2btsnd(Path.Combine(tempPath, "bootSound.wav"), btsndPath);
                File.Delete(Path.Combine(tempPath, "bootSound.wav"));
            }
            else
            {
                //Copy BootSound to location
                File.Delete(btsndPath);
                File.Copy(sound, btsndPath);
            }
        }

        private static void wav2btsnd(string inputWav, string outputBtsnd)
        {
            // credits to the original creator of wav2btsnd for the general logic
            byte[] buffer = File.ReadAllBytes(inputWav);
            using FileStream output = new FileStream(outputBtsnd, FileMode.OpenOrCreate);
            using BinaryWriter writer = new BinaryWriter(output);

            writer.Write(new byte[] { 0x0, 0x0, 0x0, 0x2, 0x0, 0x0, 0x0, 0x0 });
            for (int i = 0x2C; i < buffer.Length; i += 2)
                writer.Write(new[] { buffer[i + 1], buffer[i] });
        }

        static void timer_Tick(object sender, EventArgs e)
        {
            if (mvvm.Progress < 50)
                mvvm.Progress += 1;

        }
        private static void RunSpecificInjection(GameConfig cfg, GameConsoles console, string RomPath, bool force, MainViewModel mvm)
        {
            switch (console)
            {
                case GameConsoles.NDS:
                    NDS(RomPath);
                    break;

                case GameConsoles.N64:
                    N64(RomPath, cfg.N64Stuff);
                    break;

                case GameConsoles.GBA:
                    GBA(RomPath, cfg.GBAStuff);
                    break;

                case GameConsoles.NES:
                    NESSNES(RomPath);
                    break;
                case GameConsoles.SNES:
                    NESSNES(RemoveHeader(RomPath));
                    break;
                case GameConsoles.TG16:
                    TG16(RomPath);
                    break;
                case GameConsoles.MSX:
                    MSX(RomPath);
                    break;
                case GameConsoles.WII:
                    if (RomPath.ToLower().EndsWith(".dol"))
                        WiiHomebrew(RomPath, mvm);
                    else if (RomPath.ToLower().EndsWith(".wad"))
                        WiiForwarder(RomPath, mvm);
                    else
                        WII(RomPath, mvm);
                    break;
                case GameConsoles.GCN:
                    GC(RomPath, mvm, force);
                    break;
            }
        }
        private static string ByteArrayToString(byte[] arr)
        {
            ASCIIEncoding enc = new ASCIIEncoding();
            return enc.GetString(arr);
        }
        private static void WiiForwarder(string romPath, MainViewModel mvm)
        {
            string savedir = Directory.GetCurrentDirectory();
            mvvm.msg = "Extracting Forwarder Base...";
            if (Directory.Exists(Path.Combine(tempPath, "TempBase")))
                Directory.Delete(Path.Combine(tempPath, "TempBase"), true);

            Directory.CreateDirectory(Path.Combine(tempPath, "TempBase"));

            var zipLocation = Path.Combine(toolsPath, "BASE.zip");
            ZipFile.ExtractToDirectory(zipLocation, Path.Combine(tempPath));

            DirectoryCopy(Path.Combine(tempPath, "BASE"), Path.Combine(tempPath, "TempBase"), true);
            mvvm.Progress = 20;
            mvvm.msg = "Setting up Forwarder...";
            byte[] test = new byte[4];
            using (FileStream fs = new FileStream(romPath, FileMode.Open))
            {
                fs.Seek(0xC20, SeekOrigin.Begin);
                fs.Read(test, 0, 4);
                fs.Close();
            }

            string[] id = { ByteArrayToString(test) };
            File.WriteAllLines(Path.Combine(tempPath, "TempBase", "files", "title.txt"), id);
            mvm.Progress = 30;
            mvm.msg = "Copying Forwarder...";
            File.Copy(Path.Combine(toolsPath, "forwarder.dol"), Path.Combine(tempPath, "TempBase", "sys", "main.dol"));
            mvm.Progress = 40;
            mvvm.msg = "Creating Injectable file...";
            SharedWitAndNFS2ISO2NFS(savedir, mvm, "WiiForwarder");
        }

        private static void SharedWitAndNFS2ISO2NFS(string savedir, MainViewModel mvm, string functionName)
        {
            if (IsNativeWindows)
            {
                using (Process wit = new Process())
                {
                    if (!mvm.debug)
                        wit.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    wit.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                    wit.StartInfo.Arguments = $"copy \"{Path.Combine(tempPath, "TempBase")}\" --DEST \"{Path.Combine(tempPath, "game.iso")}\" -ovv --links --iso";
                    wit.Start();
                    wit.WaitForExit();
                }

                //Thread.Sleep(6000);
                if (!File.Exists(Path.Combine(tempPath, "game.iso")))
                {
                    Console.Clear();

                    throw new Exception("Wii: An error occured while Creating the ISO");
                }

                mvvm.Progress = 50;

                mvm.msg = "Replacing TIK and TMD...";
                using Process extract = new Process();
                if (!mvm.debug)
                    extract.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                extract.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                extract.StartInfo.Arguments = $"extract \"{Path.Combine(tempPath, "game.iso")}\" --psel data --files +tmd.bin --files +ticket.bin --DEST \"{Path.Combine(tempPath, "TIKTMD")}\" -vv1";
                extract.Start();
                extract.WaitForExit();
            }
            else
            {
                string[] args = {
                    $"copy \"{Path.Combine(tempPath, "TempBase")}\" --DEST \"{Path.Combine(tempPath, "game.iso")}\" -ovv --links --iso",
                    $"extract \"{Path.Combine(tempPath, "game.iso")}\" --psel data --files +tmd.bin --files +ticket.bin --DEST \"{Path.Combine(tempPath, "TIKTMD")}\" -vv1"
                };

                foreach (var arg in args)
                    MacLinuxHelper.WriteFailedStepToJson(functionName, "wit", arg, string.Empty);

                MacLinuxHelper.DisplayMessageBoxAboutTheHelper();

            }

            if (functionName == "GCN")
            {
                //GET ROMCODE and change it
                mvm.msg = "Trying to save rom code...";
                //READ FIRST 4 BYTES
                byte[] chars = new byte[4];
                FileStream fstrm = new FileStream(Path.Combine(tempPath, "game.iso"), FileMode.Open);
                fstrm.Read(chars, 0, 4);
                fstrm.Close();
                string procod = ByteArrayToString(chars);
                string metaXml = Path.Combine(baseRomPath, "meta", "meta.xml");
                XmlDocument doc = new XmlDocument();
                doc.Load(metaXml);
                doc.SelectSingleNode("menu/reserved_flag2").InnerText = procod.ToHex();
                doc.Save(metaXml);
                mvvm.Progress = 55;
            }

            Directory.Delete(Path.Combine(tempPath, "TempBase"), true);

            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "code"), "rvlt.*"))
                File.Delete(sFile);

            File.Copy(Path.Combine(tempPath, "TIKTMD", "tmd.bin"), Path.Combine(baseRomPath, "code", "rvlt.tmd"));
            File.Copy(Path.Combine(tempPath, "TIKTMD", "ticket.bin"), Path.Combine(baseRomPath, "code", "rvlt.tik"));
            Directory.Delete(Path.Combine(tempPath, "TIKTMD"), true);

            mvm.Progress = 60;
            mvm.msg = "Injecting ROM...";
            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "content"), "*.nfs"))
                File.Delete(sFile);

            File.Move(Path.Combine(tempPath, "game.iso"), Path.Combine(baseRomPath, "content", "game.iso"));
            File.Copy(Path.Combine(toolsPath, "nfs2iso2nfs.exe"), Path.Combine(baseRomPath, "content", "nfs2iso2nfs.exe"));
            Directory.SetCurrentDirectory(Path.Combine(baseRomPath, "content"));
            using (Process iso2nfs = new Process())
            {
                if (!mvm.debug)
                    iso2nfs.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                iso2nfs.StartInfo.FileName = "nfs2iso2nfs.exe";

                if (functionName != "GCN")
                {
                    string pass = "-passthrough ";
                    string extra = "";
                    if (mvm.passtrough != true)
                        pass = "";
                    if (mvm.Index == 2)
                        extra = "-horizontal ";
                    if (mvm.Index == 3) 
                        extra = "-wiimote ";
                    if (mvm.Index == 4)
                        extra = "-instantcc ";
                    if (mvm.Index == 5)
                        extra = "-nocc ";
                    if (mvm.LR)
                        extra += "-lrpatch ";

                    iso2nfs.StartInfo.Arguments = $"-enc -homebrew {extra}{pass}-iso game.iso";
                }
                else
                    iso2nfs.StartInfo.Arguments = $"-enc -homebrew -passthrough -iso game.iso";

                iso2nfs.Start();
                iso2nfs.WaitForExit();
                File.Delete("nfs2iso2nfs.exe");
                File.Delete("game.iso");
            }
            Directory.SetCurrentDirectory(savedir);
            mvm.Progress = 80;
        }

        private static void WiiHomebrew(string romPath, MainViewModel mvm)
        {
            string savedir = Directory.GetCurrentDirectory();
            mvvm.msg = "Extracting Homebrew Base...";

            if (Directory.Exists(Path.Combine(tempPath, "TempBase")))
                Directory.Delete(Path.Combine(tempPath, "TempBase"), true);

            Directory.CreateDirectory(Path.Combine(tempPath, "TempBase"));

            ZipFile.ExtractToDirectory(Path.Combine(toolsPath, "BASE.zip"), Path.Combine(tempPath));

            DirectoryCopy(Path.Combine(tempPath, "BASE"), Path.Combine(tempPath, "TempBase"), true);
            mvvm.Progress = 20;
            mvvm.msg = "Injecting DOL...";

            File.Copy(romPath, Path.Combine(tempPath, "TempBase", "sys", "main.dol"));
            mvm.Progress = 30;
            mvvm.msg = "Creating Injectable file...";
            SharedWitAndNFS2ISO2NFS(savedir, mvm, "WiiHomebrew");
        }
        private static void PatchDol(string consoleName, string mainDolPath, MainViewModel mvm)
        {
            var filePaths = mvm.gctPath.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var convertedGctFiles = new List<string>();

            foreach (var path in filePaths)
            {
                string convertedPath = path;

                if (Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var (codes, gameId) = GctCode.ParseOcarinaOrDolphinTxtFile(path);
                        convertedPath = Path.ChangeExtension(path, ".gct");
                        GctCode.WriteGctFile(convertedPath, codes, gameId);
                        Logger.Log($"Converted {path} → {convertedPath} (Game ID: {gameId ?? "None"})");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"ERROR: Failed to convert {path} - {ex.Message}");
                        continue;
                    }
                }

                convertedGctFiles.Add(convertedPath);
            }

            if (convertedGctFiles.Count == 0)
            {
                Logger.Log("ERROR: No valid GCT files available for patching.");
                return;
            }

            if (consoleName == "Wii")
            {
                var stringBuilder = new StringBuilder();

                foreach (var gctFile in convertedGctFiles)
                    stringBuilder.Append($" --add-section \"{gctFile}\"");

                var witArgs = $"patch \"{mainDolPath}\"" + stringBuilder.ToString();

                if (IsNativeWindows)
                {
                    using var unpack = new Process();
                    unpack.StartInfo.FileName = Path.Combine(toolsPath, "wstrt.exe");
                    unpack.StartInfo.Arguments = witArgs;
                    unpack.Start();
                    unpack.WaitForExit();
                }
                else
                {
                    MacLinuxHelper.PrepareAndInformUserOnUWUVCIHelper(consoleName, "wstrt", witArgs, toolsPath);
                }
            }
            else
            {
                var dol = new Dol();
                var allCodes = new List<GctCode>();

                foreach (var filePath in convertedGctFiles)
                    allCodes.AddRange(GctCode.LoadFromFile(filePath));

                dol.PatchDolFile(mainDolPath, allCodes);
            }
        }



        private static void GctPatch(MainViewModel mvm, string consoleName, string isoPath)
        {
            if (string.IsNullOrEmpty(mvm.GctPath))
                return;
;
            var extraction = Path.Combine(tempPath, "temp");

            mvm.msg = "Patching main.dol with gct file";
            mvm.Progress = 27;

            File.Delete(isoPath);
;
            var mainDolPath = Directory.GetFiles(extraction, "main.dol", SearchOption.AllDirectories).FirstOrDefault();

            PatchDol(consoleName, mainDolPath, mvm);
        }

        private static void WII(string romPath, MainViewModel mvm)
        {
            var witArgs = "";
            var dolPatch = mvm.RemoveDeflicker || mvm.RemoveDithering || mvm.HalfVFilter;
            string savedir = Directory.GetCurrentDirectory();
            if (new FileInfo(romPath).Extension.Contains("iso"))
            {
                mvm.msg = "Copying ROM...";
                File.Copy(romPath, Path.Combine(tempPath, "pre.iso"));
                mvm.Progress = 15;
            }
            else if (mvm.NKITFLAG || romPath.Contains("nkit") || new FileInfo(romPath).Extension.Contains("wbfs"))
            {
                witArgs = $"copy --source \"{romPath}\" --dest \"{Path.Combine(tempPath, "pre.iso")}\" -I";
                if (IsNativeWindows)
                {
                    if (mvm.NKITFLAG || romPath.Contains("nkit"))
                        mvm.msg = "Converting NKIT to ISO";
                    else
                        mvm.msg = "Converting WBFS to ISO...";

                    using Process toiso = new Process();

                    if (!mvm.debug)
                        toiso.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    toiso.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                    toiso.StartInfo.Arguments = witArgs;

                    toiso.Start();
                    toiso.WaitForExit();
                }
                else
                    MacLinuxHelper.PrepareAndInformUserOnUWUVCIHelper("Wii", "wit", witArgs, toolsPath);

                if (!new FileInfo(romPath).Extension.Contains("wbfs"))
                    if (!File.Exists(Path.Combine(toolsPath, "pre.iso")))
                        throw new Exception("nkit");

                mvm.Progress = 15;
            }

            //GET ROMCODE and change it
            mvm.msg = "Trying to change the Manual...";
            //READ FIRST 4 BYTES
            byte[] chars = new byte[4];
            FileStream fstrm = new FileStream(Path.Combine(tempPath, "pre.iso"), FileMode.Open);
            fstrm.Read(chars, 0, 4);
            fstrm.Close();
            string procod = ByteArrayToString(chars);
            string neededformanual = procod.ToHex();
            string metaXml = Path.Combine(baseRomPath, "meta", "meta.xml");
            XmlDocument doc = new XmlDocument();
            doc.Load(metaXml);
            doc.SelectSingleNode("menu/reserved_flag2").InnerText = neededformanual;
            doc.Save(metaXml);
            //edit emta.xml
            mvm.Progress = 25;

            if (mvm.regionfrii)
            {
                using FileStream fs = new FileStream(Path.Combine(tempPath, "pre.iso"), FileMode.Open);
                fs.Seek(0x4E003, SeekOrigin.Begin);
                if (mvm.regionfriius)
                {
                    fs.Write(new byte[] { 0x01 }, 0, 1);
                    fs.Seek(0x4E010, SeekOrigin.Begin);
                    fs.Write(new byte[] { 0x80, 0x06, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 }, 0, 16);
                }
                else if (mvm.regionfriijp)
                {
                    fs.Write(new byte[] { 0x00 }, 0, 1);
                    fs.Seek(0x4E010, SeekOrigin.Begin);
                    fs.Write(new byte[] { 0x00, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 }, 0, 16);
                }
                else
                {
                    fs.Write(new byte[] { 0x02 }, 0, 1);
                    fs.Seek(0x4E010, SeekOrigin.Begin);
                    fs.Write(new byte[] { 0x80, 0x80, 0x80, 0x00, 0x03, 0x03, 0x04, 0x03, 0x00, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 }, 0, 16);
                }
                fs.Close();
            }

            var preIso = Path.Combine(tempPath, "pre.iso");

            if (mvm.donttrim)
            {
                witArgs = $"extract \"{preIso}\" --DEST \"{Path.Combine(tempPath, "TEMP")}\" --psel WHOLE -vv1";
                mvm.msg = "Trimming ROM...";
            }
            else
            {
                witArgs = $"extract \"{preIso}\" --DEST \"{Path.Combine(tempPath, "TEMP")}\" --psel data -vv1";
                mvm.msg = "Prepping ROM...";
            }

            if (IsNativeWindows)
            {
                using Process trimm = new Process();
                if (!mvm.debug)
                    trimm.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                trimm.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                trimm.StartInfo.Arguments = witArgs;
                trimm.Start();
                trimm.WaitForExit();
                mvm.Progress = 30;
            }
            else
                MacLinuxHelper.PrepareAndInformUserOnUWUVCIHelper("Wii", "wit", witArgs, toolsPath);

            GctPatch(mvm, "Wii", Path.Combine(tempPath, "pre.iso"));

            if (dolPatch)
            {
                mvm.msg = "Patching main.dol file";
                mvm.Progress = 33;

                var extractionFolder = Path.Combine(tempPath, "TEMP");
                var mainDolPath = Directory.GetFiles(extractionFolder, "main.dol", SearchOption.AllDirectories).FirstOrDefault();
                var output = Path.Combine(Path.GetDirectoryName(mainDolPath), "patched.dol");

                DeflickerDitheringRemover.ProcessFile(mainDolPath, output, mvm.RemoveDeflicker, mvm.RemoveDithering, mvm.HalfVFilter);

                File.Delete(mainDolPath);
                File.Move(output, mainDolPath);
            }

            if (mvm.Index == 4)
            {
                mvvm.msg = "Patching ROM (Force CC)...";
                Console.WriteLine("Patching the ROM to force Classic Controller input");
                using Process tik = new Process();
                tik.StartInfo.FileName = Path.Combine(toolsPath, "GetExtTypePatcher.exe");
                tik.StartInfo.Arguments = $"\"{Path.Combine(tempPath, "TEMP", "DATA", "sys", "main.dol")}\" -nc";
                tik.StartInfo.UseShellExecute = false;
                tik.StartInfo.CreateNoWindow = true;
                tik.StartInfo.RedirectStandardOutput = true;
                tik.StartInfo.RedirectStandardInput = true;
                tik.Start();
                Thread.Sleep(2000);
                tik.StandardInput.WriteLine();
                tik.WaitForExit();
                mvm.Progress = 35;
            }

            if (mvm.Patch)
            {
                mvm.msg = "Video Patching ROM...";
                using Process vmc = new Process();

                File.Copy(Path.Combine(toolsPath, "wii-vmc.exe"), Path.Combine(tempPath, "TEMP", "DATA", "sys", "wii-vmc.exe"));
                Directory.SetCurrentDirectory(Path.Combine(tempPath, "TEMP", "DATA", "sys"));

                vmc.StartInfo.FileName = "wii-vmc.exe";
                vmc.StartInfo.Arguments = "main.dol";
                vmc.StartInfo.UseShellExecute = false;
                vmc.StartInfo.CreateNoWindow = true;
                vmc.StartInfo.RedirectStandardOutput = true;
                vmc.StartInfo.RedirectStandardInput = true;

                vmc.Start();
                Thread.Sleep(1000);
                vmc.StandardInput.WriteLine("a");
                Thread.Sleep(2000);

                if (mvm.toPal)
                    vmc.StandardInput.WriteLine("1");
                else
                    vmc.StandardInput.WriteLine("2");

                Thread.Sleep(2000);
                vmc.StandardInput.WriteLine();
                vmc.WaitForExit();
                File.Delete("wii-vmc.exe");

                Directory.SetCurrentDirectory(savedir);
                mvm.Progress = 40;
            }

            var tempFolder = Path.Combine(tempPath, "TEMP");

            if (mvm.donttrim)
            {
                mvm.msg = "Creating ISO from patched ROM...";
                witArgs = $"copy \"{tempFolder}\" --DEST \"{Path.Combine(tempPath, "game.iso")}\" -ovv --psel WHOLE --iso";
            }
            else
            {
                mvm.msg = "Creating ISO from trimmed ROM...";
                witArgs = $"copy \"{tempFolder}\" --DEST \"{Path.Combine(tempPath, "game.iso")}\" -ovv --links --iso";
            }

            if (IsNativeWindows)
            {
                using Process repack = new Process();
                if (!mvm.debug)
                    repack.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                repack.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                repack.StartInfo.Arguments = witArgs;
                repack.Start();
                repack.WaitForExit();
            }
            else
                MacLinuxHelper.PrepareAndInformUserOnUWUVCIHelper("Wii", "wit", witArgs, toolsPath);


            Directory.Delete(Path.Combine(tempPath, "TEMP"), true);
            File.Delete(Path.Combine(tempPath, "pre.iso"));


            mvm.Progress = 50;
            mvm.msg = "Replacing TIK and TMD...";
            var gameIso = Path.Combine(tempPath, "game.iso");
            witArgs = $"extract \"{gameIso}\" --psel data --files +tmd.bin --files +ticket.bin --DEST \"{Path.Combine(tempPath, "TIKTMD")}\" -vv1";

            if (IsNativeWindows)
            {
                using Process extract = new Process();
                if (!mvm.debug)
                    extract.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                extract.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                extract.StartInfo.Arguments = witArgs;
                extract.Start();
                extract.WaitForExit();
            }
            else
                MacLinuxHelper.PrepareAndInformUserOnUWUVCIHelper("Wii", "wit", witArgs, toolsPath);

            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "code"), "rvlt.*"))
                File.Delete(sFile);

            File.Copy(Path.Combine(tempPath, "TIKTMD", "tmd.bin"), Path.Combine(baseRomPath, "code", "rvlt.tmd"));
            File.Copy(Path.Combine(tempPath, "TIKTMD", "ticket.bin"), Path.Combine(baseRomPath, "code", "rvlt.tik"));
            Directory.Delete(Path.Combine(tempPath, "TIKTMD"), true);

            mvm.Progress = 60;
            mvm.msg = "Injecting ROM...";

            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "content"), "*.nfs"))
                File.Delete(sFile);

            File.Move(Path.Combine(tempPath, "game.iso"), Path.Combine(baseRomPath, "content", "game.iso"));
            File.Copy(Path.Combine(toolsPath, "nfs2iso2nfs.exe"), Path.Combine(baseRomPath, "content", "nfs2iso2nfs.exe"));
            Directory.SetCurrentDirectory(Path.Combine(baseRomPath, "content"));
            using (Process iso2nfs = new Process())
            {
                if (!mvm.debug)
                    iso2nfs.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                iso2nfs.StartInfo.FileName = "nfs2iso2nfs.exe";
                string extra = "";
                if (mvm.Index == 2) { extra = "-horizontal "; }
                if (mvm.Index == 3) { extra = "-wiimote "; }
                if (mvm.Index == 4) { extra = "-instantcc "; }
                if (mvm.Index == 5) { extra = "-nocc "; }
                if (mvm.LR) { extra += "-lrpatch "; }
                iso2nfs.StartInfo.Arguments = $"-enc {extra}-iso game.iso";
                iso2nfs.Start();
                iso2nfs.WaitForExit();
                File.Delete("nfs2iso2nfs.exe");
                File.Delete("game.iso");
            }
            Directory.SetCurrentDirectory(savedir);
            mvm.Progress = 80;
        }
        private static void ConvertToIso(string sourcePath, string outputFileName, bool debugMode)
        {
            using Process process = new Process();
            if (!debugMode)
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            process.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToIso.exe");
            process.StartInfo.Arguments = $"\"{sourcePath}\"";
            process.Start();
            process.WaitForExit();

            string isoPath = Path.Combine(toolsPath, outputFileName);
            if (!File.Exists(isoPath))
                throw new Exception("ISO conversion failed.");

            File.Move(isoPath, Path.Combine(tempPath, "TempBase", "files", "game.iso"));
        }

        // Function to handle NKit conversion
        private static void ConvertToNKit(string sourcePath, string outputFileName, bool debugMode)
        {
            using Process process = new Process();
            if (!debugMode)
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            process.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToNKit.exe");
            process.StartInfo.Arguments = $"\"{sourcePath}\"";
            process.Start();
            process.WaitForExit();

            string nkitIsoPath = Path.Combine(toolsPath, outputFileName);
            if (!File.Exists(nkitIsoPath))
                throw new Exception("NKit conversion failed.");

            File.Move(nkitIsoPath, Path.Combine(tempPath, "TempBase", "files", outputFileName));
        }

        private static void GC(string romPath, MainViewModel mvm, bool force)
        {
            string savedir = Directory.GetCurrentDirectory();
            mvvm.msg = "Extracting Nintendont Base...";

            if (Directory.Exists(Path.Combine(tempPath, "TempBase"))) 
                Directory.Delete(Path.Combine(tempPath, "TempBase"), true);

            Directory.CreateDirectory(Path.Combine(tempPath, "TempBase"));
            ZipFile.ExtractToDirectory(Path.Combine(toolsPath, "BASE.zip"), Path.Combine(tempPath));

            DirectoryCopy(Path.Combine(tempPath, "BASE"), Path.Combine(tempPath, "TempBase"), true);
            mvvm.Progress = 20;
            mvvm.msg = "Applying Nintendont";
            if (force)
            {
                mvvm.msg += " force 4:3...";
                File.Copy(Path.Combine(toolsPath, "nintendont_force.dol"), Path.Combine(tempPath, "TempBase", "sys", "main.dol"));
            }
            else
            {
                mvvm.msg += "...";
                File.Copy(Path.Combine(toolsPath, "nintendont.dol"), Path.Combine(tempPath, "TempBase", "sys", "main.dol"));
            }
            mvm.Progress = 40;
            mvvm.msg = "Injecting GameCube Game into NintendontBase...";
            if (mvm.donttrim)
            {
                if (romPath.ToLower().Contains("nkit.iso"))
                {
                    using (Process wit = new Process())
                    {
                        if (!mvm.debug)
                        {

                            wit.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        }
                        wit.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToIso.exe");
                        wit.StartInfo.Arguments = $"\"{romPath}\"";
                        wit.Start();
                        wit.WaitForExit();
                        if (!File.Exists(Path.Combine(toolsPath, "out.iso")))
                        {
                            throw new Exception("nkit");
                        }
                        File.Move(Path.Combine(toolsPath, "out.iso"), Path.Combine(tempPath, "TempBase", "files", "game.iso"));

                    }
                }
                else
                {
                    if (romPath.ToLower().Contains("gcz"))
                    {
                        //Convert to nkit.iso
                        using (Process wit = new Process())
                        {
                            if (!mvm.debug)
                            {

                                wit.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            }
                            wit.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToIso.exe");
                            wit.StartInfo.Arguments = $"\"{romPath}\"";
                            wit.Start();
                            wit.WaitForExit();
                            if (!File.Exists(Path.Combine(toolsPath, "out.iso")))
                            {
                                throw new Exception("nkit");
                            }
                            File.Move(Path.Combine(toolsPath, "out.iso"), Path.Combine(tempPath, "TempBase", "files", "game.iso"));

                        }
                    }
                    else
                    {
                        File.Copy(romPath, Path.Combine(tempPath, "TempBase", "files", "game.iso"));
                    }
                   
                }
            }
            else
            {
                if (romPath.ToLower().Contains("iso") || romPath.ToLower().Contains("gcm"))
                {
                    //convert to nkit
                    using (Process wit = new Process())
                    {
                        if (!mvm.debug)
                        {

                            wit.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        }
                        wit.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToNKit.exe");
                        wit.StartInfo.Arguments = $"\"{romPath}\"";
                        wit.Start();
                        wit.WaitForExit();
                        if (!File.Exists(Path.Combine(toolsPath, "out.nkit.iso")))
                        {
                            throw new Exception("nkit");
                        }
                        File.Move(Path.Combine(toolsPath, "out.nkit.iso"), Path.Combine(tempPath, "TempBase", "files", "game.iso"));

                    }
                    
                }
                else
                {
                    if (romPath.ToLower().Contains("gcz"))
                    {
                        //Convert to nkit.iso
                        using (Process wit = new Process())
                        {
                            if (!mvm.debug)
                            {

                                wit.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            }
                            wit.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToNKit.exe");
                            wit.StartInfo.Arguments = $"\"{romPath}\"";
                            wit.Start();
                            wit.WaitForExit();
                            if (!File.Exists(Path.Combine(toolsPath, "out.nkit.iso")))
                            {
                                throw new Exception("nkit");
                            }
                            File.Move(Path.Combine(toolsPath, "out.nkit.iso"), Path.Combine(tempPath, "TempBase", "files", "game.iso"));

                        }
                    }
                    else
                    {
                        File.Copy(romPath, Path.Combine(tempPath, "TempBase", "files", "game.iso"));
                    }
                    
                }

            }

            if (mvm.gc2rom != "" && File.Exists(mvm.gc2rom))
            {
                if (mvm.donttrim)
                {
                    if (mvm.gc2rom.Contains("nkit"))
                     {
                         using (Process wit = new Process())
                         {
                             if (!mvm.debug)
                             {

                                 wit.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                             }
                             wit.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToIso.exe");
                             wit.StartInfo.Arguments = $"\"{mvm.gc2rom}\"";
                             wit.Start();
                             wit.WaitForExit();
                             if (!File.Exists(Path.Combine(toolsPath, "out(Disc 1).iso")))
                             {
                                 throw new Exception("nkit");
                             }
                             File.Move(Path.Combine(toolsPath, "out(Disc 1).iso"), Path.Combine(tempPath, "TempBase", "files", "disc2.iso"));

                         }
                     }
                     else
                     {
                        
                        
                            File.Copy(mvm.gc2rom, Path.Combine(tempPath, "TempBase", "files", "disc2.iso"));
                        
                        
                    }
                }
                else{
                    if (mvm.gc2rom.ToLower().Contains("iso") || mvm.gc2rom.ToLower().Contains("gcm"))
                    {
                        //convert to nkit
                        using (Process wit = new Process())
                        {
                            if (!mvm.debug)
                            {

                                wit.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            }
                            wit.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToNKit.exe");
                            wit.StartInfo.Arguments = $"\"{mvm.gc2rom}\"";
                            wit.Start();
                            wit.WaitForExit();
                            if (!File.Exists(Path.Combine(toolsPath, "out(Disc 1).nkit.iso")))
                            {
                                throw new Exception("nkit");
                            }
                            File.Move(Path.Combine(toolsPath, "out(Disc 1).nkit.iso"), Path.Combine(tempPath, "TempBase", "files", "disc2.iso"));

                        }
                    }
                    else
                    {
                        if (romPath.ToLower().Contains("gcz"))
                        {
                            //Convert to nkit.iso
                            using (Process wit = new Process())
                            {
                                if (!mvm.debug)
                                {

                                    wit.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                }
                                wit.StartInfo.FileName = Path.Combine(toolsPath, "ConvertToNKit.exe");
                                wit.StartInfo.Arguments = $"\"{romPath}\"";
                                wit.Start();
                                wit.WaitForExit();
                                if (!File.Exists(Path.Combine(toolsPath, "out(Disc 1).nkit.iso")))
                                {
                                    throw new Exception("nkit");
                                }
                                File.Move(Path.Combine(toolsPath, "out(Disc 1).nkit.iso"), Path.Combine(tempPath, "TempBase", "files", "disc2.iso"));

                            }
                        }
                        else
                        {
                            File.Copy(romPath, Path.Combine(tempPath, "TempBase", "files", "disc2.iso"));
                        }
                    }
                    
                }
            }
            var args = $"copy \"{Path.Combine(tempPath, "TempBase")}\" --DEST \"{Path.Combine(tempPath, "game.iso")}\" -ovv --links --iso";
            if (IsNativeWindows)
            {
                using (Process wit = new Process())
                {
                    if (!mvm.debug)
                    {

                        wit.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    }
                    wit.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                    wit.StartInfo.Arguments = args;
                    wit.Start();
                    wit.WaitForExit();
                }
            }
            else
            {
                MacLinuxHelper.WriteFailedStepToJson("GCN","wit", args, string.Empty);
                MacLinuxHelper.DisplayMessageBoxAboutTheHelper();
            }

            //Thread.Sleep(6000);
            if (!File.Exists(Path.Combine(tempPath, "game.iso")))
            {
                Console.Clear();

                throw new Exception("WII: An error occured while Creating the ISO");
            }
            //Directory.Delete(Path.Combine(tempPath, "TempBase"), true);
            romPath = Path.Combine(tempPath, "game.iso");
            mvvm.Progress = 50;

            //GET ROMCODE and change it
            mvm.msg = "Trying to save rom code...";
            //READ FIRST 4 BYTES
            byte[] chars = new byte[4];
            FileStream fstrm = new FileStream(Path.Combine(tempPath, "TempBase", "files", "game.iso"), FileMode.Open);
            fstrm.Read(chars, 0, 4);
            fstrm.Close();
            string procod = ByteArrayToString(chars);
            string metaXml = Path.Combine(baseRomPath, "meta", "meta.xml");
            XmlDocument doc = new XmlDocument();
            doc.Load(metaXml);
            doc.SelectSingleNode("menu/reserved_flag2").InnerText = procod.ToHex();
            doc.Save(metaXml);
            //edit emta.xml
            Directory.Delete(Path.Combine(tempPath, "TempBase"), true);
            mvvm.Progress = 55;

            mvm.msg = "Replacing TIK and TMD...";

            args = $"extract \"{Path.Combine(tempPath, "game.iso")}\" --psel data --files +tmd.bin --files +ticket.bin --DEST \"{Path.Combine(tempPath, "TIKTMD")}\" -vv1";
            if (IsNativeWindows)
            {
                using (Process extract = new Process())
                {
                    if (!mvm.debug)
                    {
                        extract.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    }
                    extract.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                    extract.StartInfo.Arguments = args;
                    extract.Start();
                    extract.WaitForExit();
                }
            }
            else
            {
                MacLinuxHelper.WriteFailedStepToJson("GCN", "wit", args, string.Empty);
                MacLinuxHelper.DisplayMessageBoxAboutTheHelper();
            }
            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "code"), "rvlt.*"))
            {
                File.Delete(sFile);
            }
            File.Copy(Path.Combine(tempPath, "TIKTMD", "tmd.bin"), Path.Combine(baseRomPath, "code", "rvlt.tmd"));
            File.Copy(Path.Combine(tempPath, "TIKTMD", "ticket.bin"), Path.Combine(baseRomPath, "code", "rvlt.tik"));
            Directory.Delete(Path.Combine(tempPath, "TIKTMD"), true);
            mvm.Progress = 60;
            mvm.msg = "Injecting ROM...";
            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "content"), "*.nfs"))
            {
                File.Delete(sFile);
            }
            File.Move(Path.Combine(tempPath, "game.iso"), Path.Combine(baseRomPath, "content", "game.iso"));
            File.Copy(Path.Combine(toolsPath, "nfs2iso2nfs.exe"), Path.Combine(baseRomPath, "content", "nfs2iso2nfs.exe"));
            Directory.SetCurrentDirectory(Path.Combine(baseRomPath, "content"));
            using (Process iso2nfs = new Process())
            {
                if (!mvm.debug)
                {
                   
                    iso2nfs.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                }
                iso2nfs.StartInfo.FileName = "nfs2iso2nfs.exe";
                iso2nfs.StartInfo.Arguments = $"-enc -homebrew -passthrough -iso game.iso";
                iso2nfs.Start();
                iso2nfs.WaitForExit();
                File.Delete("nfs2iso2nfs.exe");
                File.Delete("game.iso");
            }
            Directory.SetCurrentDirectory(savedir);
            mvm.Progress = 80;
            
        }

        private static void Zesty_GC(string romPath, MainViewModel mvm, bool force)
        {
            string savedir = Directory.GetCurrentDirectory();
            mvvm.msg = "Extracting Nintendont Base...";

            if (Directory.Exists(Path.Combine(tempPath, "TempBase"))) 
                Directory.Delete(Path.Combine(tempPath, "TempBase"), true);

            Directory.CreateDirectory(Path.Combine(tempPath, "TempBase"));
            ZipFile.ExtractToDirectory(Path.Combine(toolsPath, "BASE.zip"), Path.Combine(tempPath));

            DirectoryCopy(Path.Combine(tempPath, "BASE"), Path.Combine(tempPath, "TempBase"), true);
            mvvm.Progress = 20;
            mvvm.msg = "Applying Nintendont";
            if (force)
            {
                mvvm.msg += " force 4:3...";
                File.Copy(Path.Combine(toolsPath, "nintendont_force.dol"), Path.Combine(tempPath, "TempBase", "sys", "main.dol"));
            }
            else
            {
                mvvm.msg += "...";
                File.Copy(Path.Combine(toolsPath, "nintendont.dol"), Path.Combine(tempPath, "TempBase", "sys", "main.dol"));
            }
            mvm.Progress = 25;
            mvvm.msg = "Injecting GameCube Game into NintendontBase...";

            var isoPath = Path.Combine(tempPath, "TempBase", "files");
            if (mvm.donttrim)
            {
                if (romPath.ToLower().Contains("nkit.iso") || romPath.ToLower().Contains("gcz"))
                    ConvertToIso(romPath, "out.iso", mvm.debug);
                else
                    File.Copy(romPath, Path.Combine(tempPath, "TempBase", "files", "game.iso"));
            }
            else
            {
                if (romPath.ToLower().Contains("iso") || romPath.ToLower().Contains("gcm") || romPath.ToLower().Contains("gcz"))
                    ConvertToNKit(romPath, "out.nkit.iso", mvm.debug);
                else
                    File.Copy(romPath, Path.Combine(tempPath, "TempBase", "files", "game.iso"));
            }

            // Handle the second game (disc2.iso)
            if (!string.IsNullOrEmpty(mvm.gc2rom) && File.Exists(mvm.gc2rom))
            {
                if (mvm.donttrim)
                {
                    if (mvm.gc2rom.Contains("nkit"))
                        ConvertToIso(mvm.gc2rom, "out(Disc 1).iso", mvm.debug);
                    else
                        File.Copy(mvm.gc2rom, Path.Combine(tempPath, "TempBase", "files", "disc2.iso"));
                }
                else
                {
                    if (mvm.gc2rom.ToLower().Contains("iso") || mvm.gc2rom.ToLower().Contains("gcm") || romPath.ToLower().Contains("gcz"))
                        ConvertToNKit(mvm.gc2rom, "out(Disc 1).nkit.iso", mvm.debug);
                    else
                        File.Copy(romPath, Path.Combine(tempPath, "TempBase", "files", "disc2.iso"));
                }
            }
            SharedWitAndNFS2ISO2NFS(savedir, mvm, "GCN");
        }
       
        public static void MSX(string injectRomPath)
        {
            mvvm.msg = "Reading Header from Base...";
            byte[] test = new byte[0x580B3];
            using (var fs = new FileStream(Path.Combine(baseRomPath, "content" , "msx", "msx.pkg"),
                                 FileMode.Open,
                                 FileAccess.ReadWrite))
            {


                fs.Read(test, 0, 0x580B3);
                fs.Close();
                File.Delete(Path.Combine(baseRomPath, "content", "msx", "msx.pkg"));
            }
            mvvm.Progress = 20;
            mvvm.msg = "Creating new PKG with read Header...";
            using (var fs = new FileStream(Path.Combine(baseRomPath, "content", "msx", "msx.pkg"),
                                 FileMode.OpenOrCreate,
                                 FileAccess.ReadWrite))
            {


                fs.Write(test, 0, 0x580B3);
                fs.Close();

            }
            mvvm.Progress = 30;
            mvvm.msg = "Reading ROM content...";
            using (var fs = new FileStream(injectRomPath,
                                 FileMode.OpenOrCreate,
                                 FileAccess.ReadWrite))
            {


                test = new byte[fs.Length];
                fs.Read(test, 0, test.Length - 1);

            }
            mvvm.Progress = 50;
            mvvm.msg = "Injecting ROM into new PKG...";
            using (var fs = new FileStream(Path.Combine(baseRomPath, "content", "msx", "msx.pkg"),
                                FileMode.Append))
            {

                fs.Write(test, 0, test.Length);

            }
            mvvm.Progress = 80;
        }
        public static void DeleteDirectory(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
                DeleteDirectory(directory);

            try
            {
                Thread.Sleep(0);
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }
        public static void Clean()
        {
            if (Directory.Exists(tempPath))
                DeleteDirectory(tempPath);
        }
        [STAThread]
        public static void Loadiine(string gameName, string gameConsole)
        {
            if (gameName == null || gameName == string.Empty) gameName = "NoName";
            gameName = gameName.Replace("|", " ");
            Regex reg = new Regex("[^a-zA-Z0-9 é -]");
            //string outputPath = Path.Combine(JsonSettingsManager.Settings.InjectionPath, gameName);
            string outputPath = Path.Combine(JsonSettingsManager.Settings.OutPath, $"[LOADIINE][{gameConsole}] {reg.Replace(gameName,"")} [{mvvm.prodcode}]");
            mvvm.foldername = $"[LOADIINE][{gameConsole}] {reg.Replace(gameName, "")} [{mvvm.prodcode}]";
            int i = 0;
            while (Directory.Exists(outputPath))
            {
                outputPath = Path.Combine(JsonSettingsManager.Settings.OutPath, $"[LOADIINE][{gameConsole}] {reg.Replace(gameName, "")} [{mvvm.prodcode}]_{i}");
                mvvm.foldername = $"[LOADIINE][{gameConsole}] {reg.Replace(gameName, "")} [{mvvm.prodcode}]_{i}";
                i++;
            }
            
            DirectoryCopy(baseRomPath,outputPath, true);

            Custom_Message cm = new Custom_Message("Injection Complete", $"To Open the Location of the Inject press Open Folder.\nIf you want the inject to be put on your SD now, press Copy to SD.", JsonSettingsManager.Settings.OutPath);
            try
            {
                cm.Owner = mvvm.mw;
            }catch(Exception )
            {

            }
            cm.ShowDialog();
            Clean();
        }
        [STAThread]
        public static void Packing(string gameName, string gameConsole, MainViewModel mvm)
        {
            mvm.msg = "Checking Tools...";
            mvm.InjcttoolCheck();
            mvm.Progress = 20;
            mvm.msg = "Creating Outputfolder...";
            Regex reg = new Regex("[^a-zA-Z0-9 -]");
            if (gameName == null || gameName == string.Empty) gameName = "NoName";
           
            //string outputPath = Path.Combine(JsonSettingsManager.Settings.InjectionPath, gameName);
            string outputPath = Path.Combine(JsonSettingsManager.Settings.OutPath, $"[WUP][{gameConsole}] {reg.Replace(gameName,"").Replace("|", " ")}");
            outputPath = outputPath.Replace("|", " ");
            mvvm.foldername = $"[WUP][{gameConsole}] {reg.Replace(gameName, "").Replace("|"," ")}";
            int i = 0;
            while (Directory.Exists(outputPath))
            {
                outputPath = Path.Combine(JsonSettingsManager.Settings.OutPath, $"[WUP][{gameConsole}] {reg.Replace(gameName,"").Replace("|", " ")}_{i}");
                mvvm.foldername = $"[WUP][{gameConsole}] {reg.Replace(gameName, "").Replace("|", " ")}_{i}";
                i++;
            }
            var oldpath = Directory.GetCurrentDirectory();
            mvm.Progress = 40;
            mvm.msg = "Packing...";
            try
            {
                Directory.Delete(Environment.GetEnvironmentVariable("LocalAppData") + @"\temp\.net\CNUSPACKER", true);
            }
            catch { }
            try
            {
                var cmdLine = $"-in \"{baseRomPath}\" -out \"{outputPath}\" -encryptKeyWith {JsonSettingsManager.Settings.Ckey}";
                var regex = new Regex(@"(\"".+?\"")|(\S+)", RegexOptions.Compiled);
                var args = new List<string>();

                foreach (Match match in regex.Matches(cmdLine))
                    args.Add(match.Value.Trim('\"'));

                CNUSPACKER.Program.Main(args.ToArray());

                Directory.SetCurrentDirectory(oldpath);
            } catch(Exception ex )
            {
                Logger.Log(ex.Message);
                throw ex;
            }
            mvm.Progress = 90;
            mvm.msg = "Cleaning...";
            Clean();
            mvm.Progress = 100;
            
            mvm.msg = "";
        }
        
        public static void Download(MainViewModel mvm)
        {
            var curdir = Directory.GetCurrentDirectory();
            mvm.InjcttoolCheck();
            GameBases b = mvm.getBasefromName(mvm.SelectedBaseAsString);

            //GetKeyOfBase
            TKeys key = mvm.getTkey(b);
            if (mvm.GameConfiguration.Console == GameConsoles.WII || mvm.GameConfiguration.Console == GameConsoles.GCN)
            {
                if (Directory.Exists(tempPath)) 
                    Directory.Delete(tempPath, true);

                Directory.CreateDirectory(tempPath);

                // Call the download method with progress reporting
                var downloader = new Downloader(null, null);
                downloader.DownloadAsync(new TitleData(b.Tid, key.Tkey), Path.Combine(tempPath, "download")).GetAwaiter().GetResult();

                CSharpDecrypt.CSharpDecrypt.Decrypt(new string[] { JsonSettingsManager.Settings.Ckey, Path.Combine(tempPath, "download", b.Tid), Path.Combine(JsonSettingsManager.Settings.BasePath, $"{b.Name.Replace(":", "")} [{b.Region}]") });
                mvm.Progress = 99;
                foreach (string sFile in Directory.GetFiles(Path.Combine(JsonSettingsManager.Settings.BasePath, $"{b.Name.Replace(":", "")} [{b.Region}]", "content"), "*.nfs"))
                    File.Delete(sFile);

                mvm.Progress = 100;
            }
            else
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                Directory.CreateDirectory(tempPath);

                // Call the download method with progress reporting
                var downloader = new Downloader(null, null);
                downloader.DownloadAsync(new TitleData(b.Tid, key.Tkey), Path.Combine(tempPath, "download")).GetAwaiter().GetResult();

                mvm.Progress = 75;
                CSharpDecrypt.CSharpDecrypt.Decrypt(new string[] { JsonSettingsManager.Settings.Ckey, Path.Combine(tempPath, "download", b.Tid), Path.Combine(JsonSettingsManager.Settings.BasePath, $"{b.Name.Replace(":", "")} [{b.Region}]") });
                mvm.Progress = 100;
            }
            Directory.SetCurrentDirectory(curdir);
        }

        public static string ExtractBase(string path, GameConsoles console)
        {
            if(!Directory.Exists(Path.Combine(JsonSettingsManager.Settings.BasePath, "CustomBases")))
                Directory.CreateDirectory(Path.Combine(JsonSettingsManager.Settings.BasePath, "CustomBases"));

            string outputPath = Path.Combine(JsonSettingsManager.Settings.BasePath, "CustomBases", $"[{console}] Custom");
            int i = 0;
            while (Directory.Exists(outputPath))
            {
                outputPath = Path.Combine(JsonSettingsManager.Settings.BasePath, $"[{console}] Custom_{i}");
                i++;
            }
            CSharpDecrypt.CSharpDecrypt.Decrypt(new string[] { JsonSettingsManager.Settings.Ckey, path, outputPath });
            return outputPath;
        }
        // This function changes TitleID, ProductCode and GameName in app.xml (ID) and meta.xml (ID, ProductCode, Name)
        private static void EditXML(string gameNameOr, int index, string code)
        {
            string gameName = string.Empty;
            if(gameNameOr != null || !string.IsNullOrWhiteSpace(gameNameOr))
            {
                gameName = gameNameOr;
                if (gameName.Contains('|'))
                {
                    var split = gameName.Split('|');
                    gameName = split[0] + "," + split[1];
                }
            }

            string metaXml = Path.Combine(baseRomPath, "meta", "meta.xml");
            string appXml = Path.Combine(baseRomPath, "code", "app.xml");
            Random random = new Random();
            string ID = $"{random.Next(0x3000, 0x10000):X4}{random.Next(0x3000, 0x10000):X4}";

            string ID2 = $"{random.Next(0x3000, 0x10000):X4}";
            mvvm.prodcode = ID2;
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(metaXml);
                if (gameName != null && gameName != string.Empty)
                {
                    doc.SelectSingleNode("menu/longname_ja").InnerText = gameName.Replace(",", "\n" );
                    doc.SelectSingleNode("menu/longname_en").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_fr").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_de").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_it").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_es").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_zhs").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_ko").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_nl").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_pt").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_ru").InnerText = gameName.Replace(",", "\n");
                    doc.SelectSingleNode("menu/longname_zht").InnerText = gameName.Replace(",", "\n");
                }

                /* if(code != null)
                {
                    doc.SelectSingleNode("menu/product_code").InnerText = $"WUP-N-{code}";
                }
                else
                {*/
                    doc.SelectSingleNode("menu/product_code").InnerText = $"WUP-N-{ID2}";
                //}
                if (index > 0)
                {
                    doc.SelectSingleNode("menu/drc_use").InnerText = "65537";
                }
                doc.SelectSingleNode("menu/title_id").InnerText = $"00050002{ID}";
                doc.SelectSingleNode("menu/group_id").InnerText = $"0000{ID2}";
                if (gameName != null && gameName != string.Empty)
                {
                    doc.SelectSingleNode("menu/shortname_ja").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_fr").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_de").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_en").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_it").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_es").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_zhs").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_ko").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_nl").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_pt").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_ru").InnerText = gameName.Split(',')[0];
                    doc.SelectSingleNode("menu/shortname_zht").InnerText = gameName.Split(',')[0];
                }

                doc.Save(metaXml);
            }
            catch (NullReferenceException)
            {
                   
            }

            try
            {
                doc.Load(appXml);
                doc.SelectSingleNode("app/title_id").InnerText = $"00050002{ID}";
            //doc.SelectSingleNode("app/title_id").InnerText = $"0005000247414645";
                
                doc.SelectSingleNode("app/group_id").InnerText = $"0000{ID2}";
                doc.Save(appXml);
            }
            catch (NullReferenceException)
            {
                  
            }
        }

        //This function copies the custom or normal Base to the working directory
        private static void CopyBase(string baserom, string customPath)
        {
            if (Directory.Exists(baseRomPath)) // sanity check
                Directory.Delete(baseRomPath, true);

            if (baserom == "Custom")
                DirectoryCopy(customPath, baseRomPath, true);
            else
                DirectoryCopy(Path.Combine(JsonSettingsManager.Settings.BasePath, baserom), baseRomPath, true);
        }

        private static void TG16(string injectRomPath)
        {
            //checking if folder
            if (Directory.Exists(injectRomPath))
            {
                DirectoryCopy(injectRomPath, "test", true);
                //TurboGrafCD
                using (Process TurboInject = new Process())
                {
                    mvvm.msg = "Creating TurboCD Pkg...";
                    TurboInject.StartInfo.UseShellExecute = false;
                    TurboInject.StartInfo.CreateNoWindow = true;
                    TurboInject.StartInfo.FileName = Path.Combine(toolsPath, "BuildTurboCDPcePkg.exe");
                    TurboInject.StartInfo.Arguments = $"test";
                    TurboInject.Start();
                    TurboInject.WaitForExit();
                    mvvm.Progress = 70;
                }
                Directory.Delete("test", true);
            }
            else
            {
                //creating pkg file including the TG16 rom
                using Process TurboInject = new Process();
                mvvm.msg = "Creating Turbo16 Pkg...";
                TurboInject.StartInfo.UseShellExecute = false;
                TurboInject.StartInfo.CreateNoWindow = true;
                TurboInject.StartInfo.FileName = Path.Combine(toolsPath, "BuildPcePkg.exe");
                TurboInject.StartInfo.Arguments = $"\"{injectRomPath}\"";
                TurboInject.Start();
                TurboInject.WaitForExit();
                mvvm.Progress = 70;
            }
            mvvm.msg = "Injecting ROM...";
            //replacing tg16 rom
            File.Delete(Path.Combine(baseRomPath, "content", "pceemu", "pce.pkg"));
            File.Copy("pce.pkg", Path.Combine(baseRomPath, "content", "pceemu", "pce.pkg"));
            File.Delete("pce.pkg");
            mvvm.Progress = 80;
        }

        private static void NESSNES(string injectRomPath)
        {
            string rpxFile = Directory.GetFiles(Path.Combine(baseRomPath, "code"), "*.rpx")[0]; //To get the RPX path where the NES/SNES rom needs to be Injected in
            mvvm.msg = "Decompressing RPX...";
            RPXCompOrDecomp(rpxFile, false); //Decompresses the RPX to be able to write the game into it
            mvvm.Progress = 20;
            if (mvvm.pixelperfect)
            {
                using Process retroinject = new Process();
                mvvm.msg = "Applying Pixel Perfect Patches...";
                retroinject.StartInfo.UseShellExecute = false;
                retroinject.StartInfo.CreateNoWindow = true;
                retroinject.StartInfo.RedirectStandardOutput = true;
                retroinject.StartInfo.RedirectStandardError = true;
                retroinject.StartInfo.FileName = Path.Combine(toolsPath, "ChangeAspectRatio.exe");
                retroinject.StartInfo.Arguments = $"\"{rpxFile}\"";

                retroinject.Start();
                retroinject.WaitForExit();
                mvvm.Progress = 30;
            }
            using (Process retroinject = new Process())
            {
                mvvm.msg = "Injecting ROM...";
                retroinject.StartInfo.UseShellExecute = false;
                retroinject.StartInfo.CreateNoWindow = true;
                retroinject.StartInfo.RedirectStandardOutput = true;
                retroinject.StartInfo.RedirectStandardError = true;
                retroinject.StartInfo.FileName = Path.Combine(toolsPath, "retroinject.exe");
                retroinject.StartInfo.Arguments = $"\"{rpxFile}\" \"{injectRomPath}\" \"{rpxFile}\"";

                retroinject.Start();
                retroinject.WaitForExit();
                mvvm.Progress = 70;
                var s = retroinject.StandardOutput.ReadToEnd();
                var e = retroinject.StandardError.ReadToEnd();
                if (e.Contains("is too large") || s.Contains("is too large"))
                {
                    mvvm.Progress = 100;
                    throw new Exception("retro");
                }

            }
            mvvm.msg = "Compressing RPX...";
            RPXCompOrDecomp(rpxFile, true); //Compresses the RPX
            mvvm.Progress = 80;
        }

        private static void GBA(string injectRomPath, N64Conf config)
        {
            bool delete = false;
            if (!new FileInfo(injectRomPath).Extension.Contains("gba"))
            {
                mvvm.msg = "Injecting GB/GBC ROM into goomba...";

                // Concatenate goomba.gba and the ROM into goombamenu.gba
                string goombaGbaPath = Path.Combine(toolsPath, "goomba.gba");
                string goombaMenuPath = Path.Combine(toolsPath, "goombamenu.gba");

                // Read both files and concatenate them
                using (FileStream output = new FileStream(goombaMenuPath, FileMode.Create))
                {
                    // Copy goomba.gba into goombamenu.gba
                    using (FileStream goombaGbaStream = new FileStream(goombaGbaPath, FileMode.Open))
                        goombaGbaStream.CopyTo(output);

                    // Append the injectRomPath (GB/GBC ROM) to goombamenu.gba
                    using FileStream injectRomStream = new FileStream(injectRomPath, FileMode.Open);
                    injectRomStream.CopyTo(output);
                }

                mvvm.Progress = 20;

                mvvm.msg = "Padding goomba ROM...";

                // Padding to 32MB (33554432 bytes)
                byte[] rom = new byte[33554432];
                FileStream fs = new FileStream(goombaMenuPath, FileMode.Open);
                fs.Read(rom, 0, (int)fs.Length);
                fs.Close();

                // Write the padded ROM to goombaPadded.gba
                string goombaPaddedPath = Path.Combine(toolsPath, "goombaPadded.gba");
                File.WriteAllBytes(goombaPaddedPath, rom);

                injectRomPath = goombaPaddedPath; // Set the injectRomPath to the padded ROM
                delete = true;
                mvvm.Progress = 40;
            }

            if (mvvm.PokePatch)
            {
                mvvm.msg = "Applying PokePatch";
                File.Copy(injectRomPath, Path.Combine(tempPath, "rom.gba"));
                injectRomPath = Path.Combine(tempPath, "rom.gba");
                PokePatch(injectRomPath);
                delete = true;
                mvvm.PokePatch = false;
                mvvm.Progress = 50;
            }

            using (Process psb = new Process())
            {
                mvvm.msg = "Injecting ROM...";
                psb.StartInfo.UseShellExecute = false;
                psb.StartInfo.CreateNoWindow = true;
                psb.StartInfo.FileName = Path.Combine(toolsPath, "psb.exe");
                psb.StartInfo.Arguments = $"\"{Path.Combine(baseRomPath, "content", "alldata.psb.m")}\" \"{injectRomPath}\" \"{Path.Combine(baseRomPath, "content", "alldata.psb.m")}\"";
                //psb.StartInfo.RedirectStandardError = true;
                //psb.StartInfo.RedirectStandardOutput = true;
                psb.Start();

                //var error = psb.StandardError.ReadToEndAsync();
                //var output = psb.StandardOutput.ReadToEndAsync();

                psb.WaitForExit();

                //if (!string.IsNullOrEmpty(error.Result))
                //throw new Exception(error.Result + "\nFile:" + new StackFrame(0, true).GetFileName() + "\nLine: " + new StackFrame(0, true).GetFileLineNumber());

                mvvm.Progress = 50;
            }

            if (config.DarkFilter == false)
            {
                var mArchiveExePath = Path.Combine(toolsPath, "MArchiveBatchTool.exe");
                var allDataPath = Path.Combine(baseRomPath, "content", "alldata.psb.m");

                // Step 1: Extract all data (longer wait time)
                RunProcess(mArchiveExePath, $"archive extract \"{allDataPath}\" --codec zlib --seed MX8wgGEJ2+M47 --keyLength 80", true);
                mvvm.Progress += 5;

                var lastModDirect = new DirectoryInfo(Path.Combine(baseRomPath, "content", "alldata.psb.m_extracted"))
                    .GetDirectories()
                    .OrderByDescending(d => d.LastWriteTimeUtc)
                    .LastOrDefault();

                var titleprofPsbM = Path.Combine(lastModDirect.FullName, "config", "title_prof.psb.m");

                // Step 2: Unpack title_prof.psb.m
                RunProcess(mArchiveExePath, $"m unpack \"{titleprofPsbM}\" zlib MX8wgGEJ2+M47 80", true);
                mvvm.Progress += 5;

                var titleprofPsb = Path.Combine(lastModDirect.FullName, "config", "title_prof.psb");

                // Step 3: Deserialize title_prof.psb
                RunProcess(mArchiveExePath, $"psb deserialize \"{titleprofPsb}\"", true);
                mvvm.Progress += 5;

                var titleprofPsbJson = Path.Combine(lastModDirect.FullName, "config", "title_prof.psb.json");
                var titleprofPsbJson_Modified = Path.Combine(lastModDirect.FullName, "config", "modified_title_prof.psb.json");

                // Step 4: Modify the JSON
                using (StreamReader sr = File.OpenText(titleprofPsbJson))
                {
                    var json = sr.ReadToEnd();
                    dynamic jsonObj = JsonConvert.DeserializeObject(json);
                    jsonObj["root"]["m2epi"]["brightness"] = 1;

                    json = JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(titleprofPsbJson_Modified, json);
                    sr.Close();
                }
                File.Delete(titleprofPsbJson);
                File.Move(titleprofPsbJson_Modified, titleprofPsbJson);

                // Step 5: Serialize the JSON back to PSB
                RunProcess(mArchiveExePath, $"psb serialize \"{titleprofPsbJson}\"", true);
                mvvm.Progress += 5;

                // Step 6: Pack the modified PSB back to a file
                RunProcess(mArchiveExePath, $"m pack \"{titleprofPsb}\" zlib MX8wgGEJ2+M47 80", true);
                mvvm.Progress += 5;

                File.Delete(titleprofPsbJson);

                // Step 7: Rebuild the archive (longer wait time)
                RunProcess(mArchiveExePath, $"archive build --codec zlib --seed MX8wgGEJ2+M47 --keyLength 80 \"{Path.Combine(baseRomPath, "content", "alldata.psb.m_extracted")}\" \"{Path.Combine(baseRomPath, "content", "alldata")}\"", true);
                mvvm.Progress += 15;

                // Clean up extracted data
                Directory.Delete(Path.Combine(baseRomPath, "content", "alldata.psb.m_extracted"), true);
                File.Delete(Path.Combine(baseRomPath, "content", "alldata.psb"));
            }

            if (delete)
            {
                File.Delete(injectRomPath);
                if (File.Exists(Path.Combine(toolsPath, "goombamenu.gba"))) File.Delete(Path.Combine(toolsPath, "goombamenu.gba"));
            }
        }

        // Stupid me complaining, function to create and run the process
        private static void RunProcess(string fileName, string arguments, bool indefiniteWait = false, int waitTime = 3000)
        {
            using var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = arguments,
                FileName = fileName
            };

            process.StartInfo = startInfo;
            process.Start();

            if (indefiniteWait)
                process.WaitForExit();
            else
                process.WaitForExit(waitTime);
        }

        private static void NDS(string injectRomPath)
        {
            try
            {
                string romName = GetRomNameFromZip();
                mvvm.msg = "Removing BaseRom...";
                ReplaceRomWithInjected(romName, injectRomPath);

                if (mvvm.DSLayout) 
                {
                    mvvm.msg = "Adding additional DS layout screens...";

                    using (var zip = ZipFile.Open(Path.Combine(toolsPath, "DSLayoutScreens.zip"), ZipArchiveMode.Read))
                        zip.ExtractToDirectory(Path.Combine(tempPath, "DSLayoutScreens"));

                    // Yes this is a typo but it's becuase I fucked up when I uploaded the original file.
                    DirectoryCopy(Path.Combine(tempPath, "DSLayoutScreens", (mvvm.STLayout ? "Phatnom Hourglass" : "All")), baseRomPath, true);
                }
                if (mvvm.RendererScale || mvvm.Brightness != 80 || mvvm.PixelArtUpscaler != 0)
                {
                    mvvm.msg = "Updating configuration_cafe.json...";
                    UpdateConfigurationCafeJson();
                }

                RecompressRom(romName);
                mvvm.Progress = 80;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"An error occurred in NDS method: {ex.Message}");
                throw;
            }
        }

        private static void UpdateConfigurationCafeJson()
        {
            var configurationCafe = Path.Combine(baseRomPath, "content", "0010", "configuration_cafe.json");

            // Load the JSON file
            string jsonContent = File.ReadAllText(configurationCafe);

            // Parse the JSON content
            var jsonObject = JObject.Parse(jsonContent);

            // Update the values
            jsonObject["configuration"]["3DRendering"]["RenderScale"] = (mvvm.RendererScale ? 0 : 1);
            jsonObject["configuration"]["Display"]["Brightness"] = mvvm.Brightness;
            jsonObject["configuration"]["Display"]["PixelArtUpscaler"] = mvvm.PixelArtUpscaler;

            // Write the updated JSON back to the file
            File.WriteAllText(configurationCafe, jsonObject.ToString());
        }

        private static string GetRomNameFromZip()
        {
            mvvm.msg = "Getting BaseRom Name...";
            string zipLocation = Path.Combine(baseRomPath, "content", "0010", "rom.zip");
            string romName = string.Empty;

            using (var zip = ZipFile.Open(zipLocation, ZipArchiveMode.Read))
            {
                var entry = zip.Entries.FirstOrDefault(file => file.Name.Contains("WUP"));
                if (entry != null)
                    romName = entry.Name;
            }
            mvvm.Progress = 15;

            if (string.IsNullOrEmpty(romName))
                throw new InvalidOperationException("ROM name not found in the zip archive.");

            return romName;
        }

        private static void ReplaceRomWithInjected(string romName, string injectRomPath)
        {
            string romPath = Path.Combine(Directory.GetCurrentDirectory(), romName);

            if (File.Exists(romPath))
                File.Delete(romPath);

            string zipLocation = Path.Combine(baseRomPath, "content", "0010", "rom.zip");

            if (File.Exists(zipLocation))
                File.Delete(zipLocation);

            File.Copy(injectRomPath, romPath);
        }

        private static void RecompressRom(string romName)
        {
            string zipLocation = Path.Combine(baseRomPath, "content", "0010", "rom.zip");
            string romPath = Path.Combine(Directory.GetCurrentDirectory(), romName);

            using (var stream = new FileStream(zipLocation, FileMode.Create))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
                    archive.CreateEntryFromFile(romPath, Path.GetFileName(romPath));

            File.Delete(romPath);
        }


        private static void N64(string injectRomPath, N64Conf config)
        {
            try
            {
                InjectRom(injectRomPath);
                if (config.WideScreen || config.DarkFilter)
                    ApplyCustomSettings(config);

                ApplyIniSettings(config);
                mvvm.Progress = 80;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"An error occurred in N64 method: {ex.Message}");
                throw;
            }
        }

        private static void InjectRom(string injectRomPath)
        {
            string mainRomPath = Directory.GetFiles(Path.Combine(baseRomPath, "content", "rom"))[0];

            using Process n64convert = new Process();
            mvvm.msg = "Injecting ROM...";
            n64convert.StartInfo.UseShellExecute = false;
            n64convert.StartInfo.CreateNoWindow = true;
            n64convert.StartInfo.FileName = Path.Combine(toolsPath, "N64Converter.exe");
            n64convert.StartInfo.Arguments = $"\"{injectRomPath}\" \"{mainRomPath}\"";
            n64convert.Start();
            n64convert.WaitForExit();
            mvvm.Progress = 60;
        }

        private static void ApplyCustomSettings(N64Conf config)
        {
            string frameLayoutPath = Path.Combine(baseRomPath, "content", "FrameLayout.arc");

            using (var fileStream = File.Open(frameLayoutPath, FileMode.Open))
            {
                uint offset = 0;
                uint size = 0;
                byte[] offsetB = new byte[4];
                byte[] sizeB = new byte[4];
                byte[] nameB = new byte[0x18];
                var header = new byte[4];

                byte[] oneOut = BitConverter.GetBytes((float)1);
                byte[] zeroOut = BitConverter.GetBytes((float)0);

                byte darkFilter = (byte)(config.DarkFilter ? 0 : 1);
                byte[] wideScreen = config.WideScreen ? new byte[] { 0x44, 0xF0, 0, 0 } : new byte[] { 0x44, 0xB4, 0, 0 };

                fileStream.Read(header, 0, 4);

                if (header.SequenceEqual(new byte[] { (byte)'S', (byte)'A', (byte)'R', (byte)'C' }))
                {
                    fileStream.Position = 0x0C;
                    fileStream.Read(offsetB, 0, 4);
                    offset = (uint)(offsetB[0] << 24 | offsetB[1] << 16 | offsetB[2] << 8 | offsetB[3]);

                    fileStream.Position = 0x38;
                    fileStream.Read(offsetB, 0, 4);
                    offset += (uint)(offsetB[0] << 24 | offsetB[1] << 16 | offsetB[2] << 8 | offsetB[3]);

                    fileStream.Position = offset;
                    fileStream.Read(header, 0, 4);

                    if (header.SequenceEqual(new byte[] { (byte)'F', (byte)'L', (byte)'Y', (byte)'T' }))
                    {
                        fileStream.Position = offset + 0x04;
                        fileStream.Read(offsetB, 0, 4);

                        offsetB[0] = 0;
                        offsetB[1] = 0;

                        offset += (uint)(offsetB[0] << 24 | offsetB[1] << 16 | offsetB[2] << 8 | offsetB[3]);

                        fileStream.Position = offset;

                        while (offset < fileStream.Length)
                        {
                            fileStream.Read(header, 0, 4);
                            fileStream.Read(sizeB, 0, 4);
                            size = (uint)(sizeB[0] << 24 | sizeB[1] << 16 | sizeB[2] << 8 | sizeB[3]);

                            if (header[0] == 'p' && header[1] == 'i' && header[2] == 'c' && header[3] == '1')
                            {
                                fileStream.Position = offset + 0x0C;
                                fileStream.Read(nameB, 0, 0x18);

                                int count = Array.IndexOf(nameB, (byte)0);
                                string name = Encoding.ASCII.GetString(nameB, 0, count);

                                if (name == "frame")
                                    WriteFrameData(fileStream, offset, zeroOut, oneOut, wideScreen);
                                else if (name == "frame_mask")
                                    WriteDarkFilterData(fileStream, offset, darkFilter);
                                else if (name == "power_save_bg")
                                    break; // End the loop as the required modifications are done

                            }

                            offset += size;
                            fileStream.Position = offset;
                        }
                    }
                }
            }
            mvvm.Progress = 70;
        }

        private static void WriteFrameData(FileStream fileStream, uint offset, byte[] zeroOut, byte[] oneOut, byte[] wideScreen)
        {
            fileStream.Position = offset + 0x2C;
            fileStream.Write(zeroOut, 0, zeroOut.Length);

            fileStream.Position = offset + 0x30; // TranslationX
            fileStream.Write(zeroOut, 0, zeroOut.Length);

            fileStream.Position = offset + 0x44; // ScaleX
            fileStream.Write(oneOut, 0, oneOut.Length);

            fileStream.Position = offset + 0x48; // ScaleY
            fileStream.Write(oneOut, 0, oneOut.Length);

            fileStream.Position = offset + 0x4C; // Widescreen
            fileStream.Write(wideScreen, 0, wideScreen.Length);
        }

        private static void WriteDarkFilterData(FileStream fileStream, uint offset, byte darkFilter)
        {
            fileStream.Position = offset + 0x08; // Dark filter
            fileStream.WriteByte(darkFilter);
        }

        private static void ApplyIniSettings(N64Conf config)
        {
            mvvm.msg = "Copying INI...";
            string mainRomPath = Directory.GetFiles(Path.Combine(baseRomPath, "content", "rom"))[0];
            string mainIni = Path.Combine(baseRomPath, "content", "config", $"{Path.GetFileName(mainRomPath)}.ini");

            if (config.INIBin == null)
            {
                if (config.INIPath == null)
                {
                    File.Delete(mainIni);
                    File.Copy(Path.Combine(toolsPath, "blank.ini"), mainIni);
                }
                else
                {
                    File.Delete(mainIni);
                    File.Copy(config.INIPath, mainIni);
                }
            }
            else
            {
                ReadFileFromBin(config.INIBin, "custom.ini");
                File.Delete(mainIni);
                File.Move("custom.ini", mainIni);
            }
        }

        private static void RPXCompOrDecomp(string rpxpath, bool comp)
        {
            var prefix = comp ? "-c" : "-d";
            using Process rpxtool = new Process();
            rpxtool.StartInfo.UseShellExecute = false;
            rpxtool.StartInfo.CreateNoWindow = true;
            rpxtool.StartInfo.FileName = Path.Combine(toolsPath, "wiiurpxtool.exe");
            rpxtool.StartInfo.Arguments = $"{prefix} \"{rpxpath}\"";

            rpxtool.Start();
            rpxtool.WaitForExit();
        }

        private static void ReadFileFromBin(byte[] bin, string output)
        {
            File.WriteAllBytes(output, bin);
        }
        private static void Images(GameConfig config)
        {
            bool usetemp = false;
            bool readbin = false;
            try
            {
                //is an image embedded? yes => export them and check for issues
                //no => using path
                if (Directory.Exists(imgPath)) // sanity check
                    Directory.Delete(imgPath, true);

                Directory.CreateDirectory(imgPath);
                //ICON
                List<bool> Images = new List<bool>();
                if (config.TGAIco.ImgBin == null)
                {
                    //use path
                    if (config.TGAIco.ImgPath != null)
                    {
                        Images.Add(true);
                        CopyAndConvertImage(config.TGAIco.ImgPath, Path.Combine(imgPath), false, 128, 128, 32, "iconTex.tga");
                    }
                    else
                    {
                        if (File.Exists(Path.Combine(toolsPath, "iconTex.tga")))
                        {
                            CopyAndConvertImage(Path.Combine(toolsPath, "iconTex.tga"), Path.Combine(imgPath), false, 128, 128, 32, "iconTex.tga");
                            Images.Add(true);
                        }
                        else
                            Images.Add(false);

                    }
                }
                else
                {
                    ReadFileFromBin(config.TGAIco.ImgBin, $"iconTex.{config.TGAIco.extension}");
                    CopyAndConvertImage($"iconTex.{config.TGAIco.extension}", Path.Combine(imgPath), true, 128, 128, 32, "iconTex.tga");
                    Images.Add(true);
                }
                if (config.TGATv.ImgBin == null)
                {
                    //use path
                    if (config.TGATv.ImgPath != null)
                    {
                        Images.Add(true);
                        CopyAndConvertImage(config.TGATv.ImgPath, Path.Combine(imgPath), false, 1280, 720, 24, "bootTvTex.tga");
                        config.TGATv.ImgPath = Path.Combine(imgPath, "bootTvTex.tga");
                    }
                    else
                    {
                        if (File.Exists(Path.Combine(toolsPath, "bootTvTex.png")))
                        {
                            CopyAndConvertImage(Path.Combine(toolsPath, "bootTvTex.png"), Path.Combine(imgPath), false, 1280, 720, 24, "bootTvTex.tga");
                            usetemp = true;
                            Images.Add(true);

                        }
                        else
                        {
                            Images.Add(false);
                        }
                    }
                }
                else
                {
                    ReadFileFromBin(config.TGATv.ImgBin, $"bootTvTex.{config.TGATv.extension}");
                    CopyAndConvertImage($"bootTvTex.{config.TGATv.extension}", Path.Combine(imgPath), true, 1280, 720, 24, "bootTvTex.tga");
                    config.TGATv.ImgPath = Path.Combine(imgPath, "bootTvTex.tga");
                    Images.Add(true);
                    readbin = true;
                }

                //Drc
                if (config.TGADrc.ImgBin == null)
                {
                    //use path
                    if (config.TGADrc.ImgPath != null)
                    {
                        Images.Add(true);
                        CopyAndConvertImage(config.TGADrc.ImgPath, Path.Combine(imgPath), false, 854, 480, 24, "bootDrcTex.tga");
                    }
                    else
                    {
                        if (Images[1])
                        {
                            using Process conv = new Process();

                            if (!mvvm.debug)
                            {
                                conv.StartInfo.UseShellExecute = false;
                                conv.StartInfo.CreateNoWindow = true;
                            }
                            if (usetemp)
                                File.Copy(Path.Combine(toolsPath, "bootTvTex.png"), Path.Combine(tempPath, "bootDrcTex.png"));
                            else
                            {
                                conv.StartInfo.FileName = Path.Combine(toolsPath, "tga2png.exe");
                                if (!readbin)
                                    conv.StartInfo.Arguments = $"-i \"{config.TGATv.ImgPath}\" -o \"{Path.Combine(tempPath)}\"";
                                else
                                {
                                    if (config.TGATv.extension.Contains("tga"))
                                    {
                                        ReadFileFromBin(config.TGATv.ImgBin, $"bootTvTex.{config.TGATv.extension}");
                                        conv.StartInfo.Arguments = $"-i \"bootTvTex.{config.TGATv.extension}\" -o \"{Path.Combine(tempPath)}\"";
                                    }
                                    else
                                        ReadFileFromBin(config.TGATv.ImgBin, Path.Combine(tempPath, "bootTvTex.png"));
                                }

                                if (!readbin || config.TGATv.extension.Contains("tga"))
                                {
                                    conv.Start();
                                    conv.WaitForExit();
                                }

                                File.Copy(Path.Combine(tempPath, "bootTvTex.png"), Path.Combine(tempPath, "bootDrcTex.png"));

                                if (File.Exists(Path.Combine(tempPath, "bootTvTex.png")))
                                    File.Delete(Path.Combine(tempPath, "bootTvTex.png"));

                                if (File.Exists($"bootTvTex.{config.TGATv.extension}"))
                                    File.Delete($"bootTvTex.{config.TGATv.extension}");
                            }

                            CopyAndConvertImage(Path.Combine(tempPath, "bootDrcTex.png"), Path.Combine(imgPath), false, 854, 480, 24, "bootDrcTex.tga");
                            Images.Add(true);
                        }
                        else
                            Images.Add(false);
                    }
                }
                else
                {
                    ReadFileFromBin(config.TGADrc.ImgBin, $"bootDrcTex.{config.TGADrc.extension}");
                    CopyAndConvertImage($"bootDrcTex.{config.TGADrc.extension}", Path.Combine(imgPath), true, 854, 480, 24, "bootDrcTex.tga");
                    Images.Add(true);
                }

                //tv



                //logo
                if (config.TGALog.ImgBin == null)
                    //use path
                    if (config.TGALog.ImgPath != null)
                    {
                        Images.Add(true);
                        CopyAndConvertImage(config.TGALog.ImgPath, Path.Combine(imgPath), false, 170, 42, 32, "bootLogoTex.tga");
                    }
                    else
                        Images.Add(false);
                else
                {
                    ReadFileFromBin(config.TGALog.ImgBin, $"bootLogoTex.{config.TGALog.extension}");
                    CopyAndConvertImage($"bootLogoTex.{config.TGALog.extension}", Path.Combine(imgPath), true, 170, 42, 32, "bootLogoTex.tga");
                    Images.Add(true);
                }

                //Fixing Images + Injecting them
                if (Images[0] || Images[1] || Images[2] || Images[3])
                {
                    using (Process checkIfIssue = new Process())
                    {
                        checkIfIssue.StartInfo.UseShellExecute = false;
                        checkIfIssue.StartInfo.CreateNoWindow = false;
                        checkIfIssue.StartInfo.RedirectStandardOutput = true;
                        checkIfIssue.StartInfo.RedirectStandardError = true;
                        checkIfIssue.StartInfo.FileName = $"{Path.Combine(toolsPath, "tga_verify.exe")}";
                        Console.WriteLine(Directory.GetCurrentDirectory());
                        checkIfIssue.StartInfo.Arguments = $"\"{imgPath}\"";
                        checkIfIssue.Start();
                        checkIfIssue.WaitForExit();
                        var s = checkIfIssue.StandardOutput.ReadToEnd();

                        if (s.Contains("width") || s.Contains("height") || s.Contains("depth"))
                            throw new Exception("Size");

                        var e = checkIfIssue.StandardError.ReadToEnd();

                        if (e.Contains("width") || e.Contains("height") || e.Contains("depth"))
                            throw new Exception("Size");

                        if (e.Contains("TRUEVISION") || s.Contains("TRUEVISION"))
                        {
                            checkIfIssue.StartInfo.UseShellExecute = false;
                            checkIfIssue.StartInfo.CreateNoWindow = false;
                            checkIfIssue.StartInfo.RedirectStandardOutput = true;
                            checkIfIssue.StartInfo.RedirectStandardError = true;
                            checkIfIssue.StartInfo.FileName = $"{Path.Combine(toolsPath, "tga_verify.exe")}";
                            Console.WriteLine(Directory.GetCurrentDirectory());
                            checkIfIssue.StartInfo.Arguments = $"--fixup \"{imgPath}\"";
                            checkIfIssue.Start();
                            checkIfIssue.WaitForExit();
                        }
                        // Console.ReadLine();
                    }

                    if (Images[1])
                    {
                        File.Delete(Path.Combine(baseRomPath, "meta", "bootTvTex.tga"));
                        File.Move(Path.Combine(imgPath, "bootTvTex.tga"), Path.Combine(baseRomPath, "meta", "bootTvTex.tga"));
                    }
                    if (Images[2])
                    {
                        File.Delete(Path.Combine(baseRomPath, "meta", "bootDrcTex.tga"));
                        File.Move(Path.Combine(imgPath, "bootDrcTex.tga"), Path.Combine(baseRomPath, "meta", "bootDrcTex.tga"));
                    }
                    if (Images[0])
                    {
                        File.Delete(Path.Combine(baseRomPath, "meta", "iconTex.tga"));
                        File.Move(Path.Combine(imgPath, "iconTex.tga"), Path.Combine(baseRomPath, "meta", "iconTex.tga"));
                    }
                    if (Images[3])
                    {
                        File.Delete(Path.Combine(baseRomPath, "meta", "bootLogoTex.tga"));
                        File.Move(Path.Combine(imgPath, "bootLogoTex.tga"), Path.Combine(baseRomPath, "meta", "bootLogoTex.tga"));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);

                if (e.Message.Contains("Size"))
                    throw e;

                throw new Exception("Images");
            }
        }

        private static void PrepareImageDirectory()
        {
            if (Directory.Exists(imgPath))
                Directory.Delete(imgPath, true);

            Directory.CreateDirectory(imgPath);
        }

        private static bool HandleImage(PNGTGA imgConfig, string fileName, string outputDir, int width, int height, int bitDepth)
        {
            if (imgConfig.ImgBin == null)
            {
                if (!string.IsNullOrEmpty(imgConfig.ImgPath))
                {
                    CopyAndConvertImage(imgConfig.ImgPath, outputDir, false, width, height, bitDepth, $"{fileName}.tga");
                    return true;
                }
                else if (File.Exists(Path.Combine(toolsPath, $"{fileName}.tga")))
                {
                    CopyAndConvertImage(Path.Combine(toolsPath, $"{fileName}.tga"), outputDir, false, width, height, bitDepth, $"{fileName}.tga");
                    return true;
                }
            }
            else
            {
                ReadFileFromBin(imgConfig.ImgBin, $"{fileName}.{imgConfig.extension}");
                CopyAndConvertImage($"{fileName}.{imgConfig.extension}", outputDir, true, width, height, bitDepth, $"{fileName}.tga");
                return true;
            }

            // When tf would this ever return false?
            return false;
        }

        private static bool HandleDrcImage(GameConfig config, bool hasTvImage)
        {
            if (config.TGADrc.ImgBin == null)
            {
                if (!string.IsNullOrEmpty(config.TGADrc.ImgPath))
                {
                    CopyAndConvertImage(config.TGADrc.ImgPath, imgPath, false, 854, 480, 24, "bootDrcTex.tga");
                    return true;
                }
                else if (hasTvImage)
                {
                    ConvertTvImageToDrc(config);
                    return true;
                }
            }
            else
            {
                ReadFileFromBin(config.TGADrc.ImgBin, $"bootDrcTex.{config.TGADrc.extension}");
                CopyAndConvertImage($"bootDrcTex.{config.TGADrc.extension}", imgPath, true, 854, 480, 24, "bootDrcTex.tga");
                return true;
            }
            return false;
        }

        private static void ConvertTvImageToDrc(GameConfig config)
        {        
            string tempFilePath = Path.Combine(tempPath, "bootDrcTex.png");

            if (config.TGATv.extension.Contains("tga"))
                using (var process = new Process())
                {
                    if (!mvvm.debug)
                    {
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                    }

                    ReadFileFromBin(config.TGATv.ImgBin, $"bootTvTex.{config.TGATv.extension}");
                    process.StartInfo.Arguments = $"-i \"bootTvTex.{config.TGATv.extension}\" -o \"{tempFilePath}\"";

                    process.Start();
                    process.WaitForExit();
                }
            else
                ReadFileFromBin(config.TGATv.ImgBin, Path.Combine(tempPath, "bootTvTex.png"));

            CopyAndConvertImage(tempFilePath, imgPath, false, 854, 480, 24, "bootDrcTex.tga");
        }

        private static void VerifyAndInjectImages(bool hasIconImage, bool hasTvImage, bool hasDrcImage, bool hasLogoImage)
        {
            using (Process checkIfIssue = new Process())
            {
                checkIfIssue.StartInfo.UseShellExecute = false;
                checkIfIssue.StartInfo.CreateNoWindow = true;
                checkIfIssue.StartInfo.RedirectStandardOutput = true;
                checkIfIssue.StartInfo.RedirectStandardError = true;
                checkIfIssue.StartInfo.FileName = $"{Path.Combine(toolsPath, "tga_verify.exe")}";
                checkIfIssue.StartInfo.Arguments = $"\"{imgPath}\"";
                checkIfIssue.Start();
                checkIfIssue.WaitForExit();

                string output = checkIfIssue.StandardOutput.ReadToEnd();
                string error = checkIfIssue.StandardError.ReadToEnd();

                if (output.Contains("width") || output.Contains("height") || output.Contains("depth") ||
                    error.Contains("width") || error.Contains("height") || error.Contains("depth"))
                {
                    throw new Exception("Size");
                }

                if (output.Contains("TRUEVISION") || error.Contains("TRUEVISION"))
                {
                    checkIfIssue.StartInfo.Arguments = $"--fixup \"{imgPath}\"";
                    checkIfIssue.Start();
                    checkIfIssue.WaitForExit();
                }
            }

            MoveProcessedImages(hasIconImage, hasTvImage, hasDrcImage, hasLogoImage);
        }

        private static void MoveProcessedImages(bool hasIconImage, bool hasTvImage, bool hasDrcImage, bool hasLogoImage)
        {
            if (hasTvImage)
            {
                string destPath = Path.Combine(baseRomPath, "meta", "bootTvTex.tga");

                if (File.Exists(destPath))
                    File.Delete(destPath);

                File.Move(Path.Combine(imgPath, "bootTvTex.tga"), destPath);
            }

            if (hasDrcImage)
            {
                string destPath = Path.Combine(baseRomPath, "meta", "bootDrcTex.tga");

                if (File.Exists(destPath))
                    File.Delete(destPath);

                File.Move(Path.Combine(imgPath, "bootDrcTex.tga"), destPath);
            }

            if (hasIconImage)
            {
                string destPath = Path.Combine(baseRomPath, "meta", "iconTex.tga");

                if (File.Exists(destPath))
                    File.Delete(destPath);

                File.Move(Path.Combine(imgPath, "iconTex.tga"), destPath);
            }

            if (hasLogoImage)
            {
                string destPath = Path.Combine(baseRomPath, "meta", "bootLogoTex.tga");

                if (File.Exists(destPath))
                    File.Delete(destPath);

                File.Move(Path.Combine(imgPath, "bootLogoTex.tga"), destPath);
            }
        }

        private static void CopyAndConvertImage(string inputPath, string outputPath, bool delete, int widht, int height, int bit, string newname)
        {
            if (inputPath.EndsWith(".tga"))
                File.Copy(inputPath, Path.Combine(outputPath,newname));
            else
            {
                using (Process png2tga = new Process())
                {
                    png2tga.StartInfo.UseShellExecute = false;
                    png2tga.StartInfo.CreateNoWindow = true;
                    var extension = new FileInfo(inputPath).Extension;

                    if (extension.Contains("png"))
                        png2tga.StartInfo.FileName = Path.Combine(toolsPath, "png2tga.exe");
                    else if (extension.Contains("jpg") || extension.Contains("jpeg"))
                        png2tga.StartInfo.FileName = Path.Combine(toolsPath, "jpg2tga.exe");
                    else if (extension.Contains("bmp"))
                        png2tga.StartInfo.FileName = Path.Combine(toolsPath, "bmp2tga.exe");
                    
                    png2tga.StartInfo.Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" --width={widht} --height={height} --tga-bpp={bit} --tga-compression=none";

                    png2tga.Start();
                    png2tga.WaitForExit();
                }
                string name = Path.GetFileNameWithoutExtension(inputPath);

                if(File.Exists(Path.Combine(outputPath , name + ".tga")))
                    File.Move(Path.Combine(outputPath, name + ".tga"), Path.Combine(outputPath, newname));
            }
            if (delete)
                File.Delete(inputPath);
        }

        private static string RemoveHeader(string filePath)
        {
            // logic taken from snesROMUtil
            using FileStream inStream = new FileStream(filePath, FileMode.Open);
            byte[] header = new byte[512];
            inStream.Read(header, 0, 512);
            string string1 = BitConverter.ToString(header, 8, 3);
            string string2 = Encoding.ASCII.GetString(header, 0, 11);
            string string3 = BitConverter.ToString(header, 30, 16);

            if (string1 != "AA-BB-04" && string2 != "GAME DOCTOR" && string3 != "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00")
                return filePath;

            string newFilePath = Path.Combine(tempPath, Path.GetFileName(filePath));
            using (FileStream outStream = new FileStream(newFilePath, FileMode.OpenOrCreate))
            {
                inStream.CopyTo(outStream);
            }

            return newFilePath;
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                Logger.Log($"Source directory does not exist or could not be found: {sourceDirName}");
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            foreach (FileInfo file in dir.EnumerateFiles())
                file.CopyTo(Path.Combine(destDirName, file.Name), true);

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
                foreach (DirectoryInfo subdir in dir.EnumerateDirectories())
                    DirectoryCopy(subdir.FullName,  Path.Combine(destDirName, subdir.Name), copySubDirs);
        }
    }
}
