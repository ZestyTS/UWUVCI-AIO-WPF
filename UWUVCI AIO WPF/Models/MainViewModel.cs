﻿using GameBaseClassLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UWUVCI_AIO_WPF.Classes;
using UWUVCI_AIO_WPF.Properties;
using UWUVCI_AIO_WPF.UI;
using UWUVCI_AIO_WPF.UI.Windows;

namespace UWUVCI_AIO_WPF
{
    class MainViewModel : BaseModel
    {
        //public GameConfig GameConfiguration { get; set; }
        private GameConfig gameConfiguration = new GameConfig();

        
        public GameConfig GameConfiguration
        {
            get { return gameConfiguration; }
            set
            {
                gameConfiguration = value;
                OnPropertyChanged();
            }
        }
        private string romPath;

        public string RomPath
        {
            get { return romPath; }
            set { romPath = value;
                OnPropertyChanged();
            }
        }


        private GameBases gbTemp;

        public GameBases GbTemp
        {
            get { return gbTemp; }
            set { gbTemp = value; }
        }

        private string selectedBaseAsString;

        public string SelectedBaseAsString
        {
            get { return selectedBaseAsString; }
            set { selectedBaseAsString = value; }
        }


        private List<string> lGameBasesString = new List<string>();

        public List<string> LGameBasesString
        {
            get { return lGameBasesString; }
            set
            {
                lGameBasesString = value;
                OnPropertyChanged();
            }
        }
        private bool pathsSet { get; set; } = false;

        public bool PathsSet
        {
            get { return pathsSet; }
            set
            {
                pathsSet = value;
                OnPropertyChanged();
            }
        }

        private string baseStore;

        public string BaseStore
        {
            get { return baseStore; }
            set { baseStore = value;
                OnPropertyChanged();
            }
        }

        private string injectStore;

        public string InjectStore
        {
            get { return injectStore; }
            set { injectStore = value;
                OnPropertyChanged();
            }
        }

        private bool injected = false;

        public bool Injected
        {
            get { return injected; }
            set { injected = value;
                OnPropertyChanged();
            }
        }

        public int OldIndex { get; set; }

        public bool RomSet { get; set; }

        private List<GameBases> lBases = new List<GameBases>();

        public List<GameBases> LBases
        {
            get { return lBases; }
            set { lBases = value;
                OnPropertyChanged();
            }
        }

        #region TKLIST
        private List<GameBases> lNDS = new List<GameBases>();

        public List<GameBases> LNDS
        {
            get { return lNDS; }
            set { lNDS = value; OnPropertyChanged(); }
        }

        private List<GameBases> lN64 = new List<GameBases>();

        public List<GameBases> LN64
        {
            get { return lN64; }
            set { lN64 = value; OnPropertyChanged(); }
        }

        private List<GameBases> lNES = new List<GameBases>();
            
        public List<GameBases> LNES
        {
            get { return lNES; }
            set { lNES = value; OnPropertyChanged(); }
        }
        private List<GameBases> lGBA = new List<GameBases>();

        public List<GameBases> LGBA
        {
            get { return lGBA; }
            set { lGBA = value; OnPropertyChanged(); }
        }

        private List<GameBases> lSNES = new List<GameBases>();



        public List<GameBases> LSNES
        {
            get { return lSNES; }
            set { lSNES = value; OnPropertyChanged(); }
        }

        private List<GameBases> ltemp = new List<GameBases>();

        public List<GameBases> Ltemp
        {
            get { return ltemp; }
            set { ltemp = value; OnPropertyChanged(); }
        }
        #endregion

        public bool BaseDownloaded { get; set; } = false;


        private bool canInject = false;

        public bool CanInject
        {
            get { return canInject; }
            set { canInject = value;
                OnPropertyChanged();
            }
        }

        private MainWindow mw;

        public MainViewModel()
        {
            toolCheck();
            BaseCheck();
            
            GameConfiguration = new GameConfig();
           if (!ValidatePathsStillExist() && Settings.Default.SetBaseOnce && Settings.Default.SetOutOnce)
            {
                MessageBox.Show("One of your added Paths seems to not exist anymore. Please check the paths in the Path menu!", "Issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            UpdatePathSet();

            GetAllBases();
        }
        public void setMW(MainWindow mwi)
        {
            mw = mwi;
        }
        public void Pack(bool loadiine)
        {
            if (loadiine)
            {
                Injection.Loadiine(GameConfiguration.GameName);

            }
            else
            {
                Injection.Packing(GameConfiguration.GameName);
            }
            mw.load_frame.Content = new Done();
            GameConfiguration = new GameConfig();
            LGameBasesString.Clear();
            CanInject = false;
            BaseDownloaded = false;
            RomSet = false;
            RomPath = null;
            Injected = false;
        }
        public void Inject()
        {
            Injection.Inject(GameConfiguration, RomPath);
        }
        private void BaseCheck()
        {
            if (Directory.Exists(@"bases"))
            {
                var test = GetMissingVCBs();
                if(test.Count > 0)
                {
                    DialogResult res = MessageBox.Show("There are no vcb Base files stored in the bases folder. Would you like to download the missing base files?", "Error 004: \"Missing VCB Bases\"", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if(res == DialogResult.Yes)
                    {
                        foreach(string s in test)
                        {
                            DownloadBase(s);

                        }
                        BaseCheck();
                        
                    }
                    else if(res == DialogResult.No)
                    {
                        MessageBox.Show("The Programm will now terminate.");
                        Environment.Exit(1);
                    }
                }
            }
            else
            {
                DialogResult res = MessageBox.Show("No bases folder found.\nShould a bases folder be created and the missing vcb bases downloaded?", "Error 003: \"Missing VCB Bases Folder\"", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    Directory.CreateDirectory(@"bases");
                    var test = GetMissingVCBs();
                    foreach (string s in test)
                    {
                        DownloadBase(s);
                        
                    }
                    BaseCheck();
                }
                else
                {
                    MessageBox.Show("The Programm will now terminate.");
                    Environment.Exit(1);
                }
            }
            
        }
        public void UpdateBases()
        {
            
            string[] bases = { "bases.vcbnds", "bases.vcbn64", "bases.vcbgba", "bases.vcbsnes", "bases.vcbnes" };
            foreach(string s in bases)
            {
                DownloadBase(s);
                DeleteBase(s);
                CopyBase(s);
            }
            MessageBox.Show("Finished Updating! Restarting UWUVCI AIO");
            System.Diagnostics.Process.Start(System.Windows.Application.ResourceAssembly.Location);
            Environment.Exit(0);
            
        }

        public string GetFilePath(bool ROM, bool INI)
        {
            string ret = string.Empty;
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                if (ROM)
                {
                    switch (GameConfiguration.Console)
                    {
                        case GameConsoles.NDS:
                            dialog.Filter = "Nintendo DS ROM (*.nds; *.srl) | *.nds;*.srl";
                            break;
                        case GameConsoles.N64:
                            dialog.Filter = "Nintendo 64 ROM (*.n64; *.v64; *.z64) | *.n64;*.v64;*.z64";
                            break;
                        case GameConsoles.GBA:
                            dialog.Filter = "GameBoy Advance ROM (*.gba) | *.gba";
                            break;
                        case GameConsoles.NES:
                            dialog.Filter = "Nintendo Entertainment System ROM (*.nes) | *.nes";
                            break;
                        case GameConsoles.SNES:
                            dialog.Filter = "Super Nintendo Entertainment System ROM (*.sfc; *.smc) | *.sfc;*.smc";
                            break;
                    }
                }
                else if(!INI)
                {
                    dialog.Filter = "BootImages (*.png; *.tga) | *.png;*.tga";
                }
                else
                {
                    dialog.Filter = "N64 VC Configuration (*.ini) | *.ini";
                }
                DialogResult res = dialog.ShowDialog();
                if(res == DialogResult.OK)
                {
                    ret = dialog.FileName;
                }
            }
            return ret;
        }

        private static void CopyBase(string console)
        {
            File.Copy(console, $@"bases\{console}");
            File.Delete(console);
        }
        private static void DeleteBase(string console)
        {
            File.Delete($@"bases\{console}");
        }
        public static List<string> GetMissingVCBs()
        {
            List<string> ret = new List<string>();
            string path = @"bases\bases.vcb";
            if (!File.Exists(path + "nds"))
            {
                ret.Add(path + "nds");
            }
            if (!File.Exists(path + "nes"))
            {
                ret.Add(path + "nes");
            }
            if (!File.Exists(path + "n64"))
            {
                ret.Add(path + "n64");
            }
            if (!File.Exists(path + "snes"))
            {
                ret.Add(path + "snes");
            }
            if (!File.Exists(path + "gba"))
            {
                ret.Add(path + "gba");
            }
            return ret;
        }
        public static void DownloadBase(string name)
        {
            try
            {
                string basePath = $@"\bases\{name}";
                using (var client = new WebClient())
                {
                    client.DownloadFile(getDownloadLink(name), name);
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                MessageBox.Show("There was an Error downloading the VCB Base File.\nThe Programm will now terminate.", "Error 005: \"Unable to Download VCB Base\"", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            
        }
        private static string getDownloadLink(string basefile)
        {
            try
            {
                //get download link from uwuvciapi
                WebRequest request = WebRequest.Create("https://uwuvciapi.azurewebsites.net/GetVCBLink?vcb=" + basefile);
                var response = request.GetResponse();
                using (Stream dataStream = response.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.  
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.  
                    string responseFromServer = reader.ReadToEnd();
                    // Display the content.  
                    return responseFromServer;
                }
               
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void toolCheck()
        {
            if (ToolCheck.DoesToolsFolderExist())
            {
                List<MissingTool> missingTools = new List<MissingTool>();
                missingTools = ToolCheck.CheckForMissingTools();
                if(missingTools.Count > 0)
                {
                    string errorMessage = "Error 002:\nFollowing Tools seem to be missing:\n\n";
                    foreach(MissingTool m in missingTools)
                    {
                        errorMessage += $"{m.Name} not found under {m.Path}\n\n";
                    }
                    errorMessage += "Please add listed files to their corresponding Path.\nThe Programm will be terminated";
                    MessageBox.Show(errorMessage, "Error 002: \"Missing Tools\"", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Windows.Application.Current.Shutdown();
                }
            }
            else
            {
                string path = $@"{Directory.GetCurrentDirectory()}\Tools";
                MessageBox.Show($"Error: 001\nThe Tools folder seems to be missing.\nPlease make sure that the Tools Folder exists at this location:\n\n{path} \n\nThe Programm will be terminated.", "Error 001: \"Missing Tools folder\"", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Windows.Application.Current.Shutdown();
            }
        }

        public void UpdatePathSet()
        {
            PathsSet = Settings.Default.PathsSet;
            if(BaseStore != Settings.Default.BasePath)
            {
                BaseStore = Settings.Default.BasePath;
            }
            if (InjectStore != Settings.Default.BasePath)
            {
                InjectStore = Settings.Default.OutPath;
            }
        }

        public bool ValidatePathsStillExist()
        {
            bool ret = false;
            bool basep = false;
            try
            {
                if (Directory.Exists(Settings.Default.BasePath))
                {
                    basep = true;
                }
                else
                {
                    Settings.Default.BasePath = string.Empty;
                    Settings.Default.PathsSet = false;
                    Settings.Default.Save();
                }
                if (Directory.Exists(Settings.Default.OutPath))
                {
                    if (basep)
                    {
                        ret = true;
                    }
                }
                else
                {
                    Settings.Default.OutPath = string.Empty;
                    Settings.Default.PathsSet = false;
                    Settings.Default.Save();
                }
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }

        public void GetBases(GameConsoles Console)
        {
            List<GameBases> lTemp = new List<GameBases>();
            string vcbpath = $@"bases/bases.vcb{Console.ToString().ToLower()}";
            lTemp = VCBTool.ReadBasesFromVCB(vcbpath);
            LBases.Clear();
            GameBases custom = new GameBases();
            custom.Name = "Custom";
            custom.Region = Regions.EU;
            LBases.Add(custom);
            foreach(GameBases gb in lTemp)
            {
                LBases.Add(gb);
            }
            lGameBasesString.Clear();
            foreach(GameBases gb in LBases)
            {
                LGameBasesString.Add($"{gb.Name} {gb.Region}");
            }
        }
        
        public GameBases getBasefromName(string Name)
        {
            string NameWORegion = Name.Remove(Name.Length - 3, 3);
            string Region = Name.Remove(0, Name.Length - 2);
            foreach(GameBases b in LNDS)
            {
                if(b.Name == NameWORegion && b.Region.ToString() == Region)
                {
                    return b;
                }
            }
            foreach (GameBases b in LN64)
            {
                if (b.Name == NameWORegion && b.Region.ToString() == Region)
                {
                    return b;
                }
            }
            foreach (GameBases b in LNES)
            {
                if (b.Name == NameWORegion && b.Region.ToString() == Region)
                {
                    return b;
                }
            }
            foreach (GameBases b in LSNES)
            {
                if (b.Name == NameWORegion && b.Region.ToString() == Region)
                {
                    return b;
                }
            }
            foreach (GameBases b in LGBA)
            {
                if (b.Name == NameWORegion && b.Region.ToString() == Region)
                {
                    return b;
                }
            }
            return null;
        }

        private void GetAllBases()
        {
            LN64.Clear();
            LNDS.Clear();
            LNES.Clear();
            LSNES.Clear();
            LGBA.Clear();
            lNDS = VCBTool.ReadBasesFromVCB($@"bases/bases.vcbnds");
            lNES = VCBTool.ReadBasesFromVCB($@"bases/bases.vcbnes");
            lSNES = VCBTool.ReadBasesFromVCB($@"bases/bases.vcbsnes");
            lN64 = VCBTool.ReadBasesFromVCB($@"bases/bases.vcbn64");
            lGBA = VCBTool.ReadBasesFromVCB($@"bases/bases.vcbgba");
            CreateSettingIfNotExist(lNDS, GameConsoles.NDS);
            CreateSettingIfNotExist(lNES, GameConsoles.NES);
            CreateSettingIfNotExist(lSNES, GameConsoles.SNES);
            CreateSettingIfNotExist(lGBA, GameConsoles.GBA);
            CreateSettingIfNotExist(lN64, GameConsoles.N64);
        }
        private void CreateSettingIfNotExist(List<GameBases> l, GameConsoles console)
        {
            string file = $@"keys\{console.ToString().ToLower()}.vck";
            if (!File.Exists(file))
            {
                List<TKeys> temp = new List<TKeys>();
                foreach(GameBases gb in l)
                {
                    TKeys tempkey = new TKeys();
                    tempkey.Base = gb;
                    temp.Add(tempkey);
                }
                KeyFile.ExportFile(temp, console);
            }
                       
        }
        public void getTempList(GameConsoles console)
        {
            switch (console)
            {
                case GameConsoles.NDS:
                    Ltemp = LNDS;
                    break;
                case GameConsoles.N64:
                    Ltemp = LN64;
                    break;
                case GameConsoles.GBA:
                    Ltemp = LGBA;
                    break;
                case GameConsoles.NES:
                    Ltemp = LNES;
                    break;
                case GameConsoles.SNES:
                    Ltemp = LSNES;
                    break;
            }
        }

        public void EnterKey(bool ck)
        {
            EnterKey ek = new EnterKey(ck);
            ek.ShowDialog();
        }
        public bool checkcKey(string key)
        {
            if (487391367 == key.GetHashCode())
            {
                Settings.Default.Ckey = key;
                Settings.Default.Save();
                
                return true;
            }
            return false;
        }
        public bool isCkeySet()
        {
            if (Settings.Default.Ckey.GetHashCode() == 487391367)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool checkKey(string key)
        {
            if(GbTemp.KeyHash == key.GetHashCode())
            {
                UpdateKeyInFile(key, $@"keys\{GetConsoleOfBase(gbTemp).ToString().ToLower()}.vck", GbTemp, GetConsoleOfBase(gbTemp));
                return true;
            }
            return false;
        }
        public void UpdateKeyInFile(string key, string file, GameBases Base, GameConsoles console)
        {
            if (File.Exists(file))
            {
                var temp = KeyFile.ReadBasesFromKeyFile(file);
                foreach(TKeys  t in temp)
                {
                    if(t.Base.Name == Base.Name && t.Base.Region == Base.Region)
                    {
                        t.Tkey = key;
                    }
                }
                File.Delete(file);
                KeyFile.ExportFile(temp, console);
            }
        }
        public bool isKeySet(GameBases bases)
        {
            var temp = KeyFile.ReadBasesFromKeyFile($@"keys\{GetConsoleOfBase(bases).ToString().ToLower()}.vck");
            foreach(TKeys t in temp)
            {
                if(t.Base.Name == bases.Name && t.Base.Region == bases.Region)
                {
                    if(t.Tkey != null)
                    {
                        return true;
                    }
                }
            }
            return false;

        }
        public TKeys getTkey(GameBases bases)
        {
            var temp = KeyFile.ReadBasesFromKeyFile($@"keys\{GetConsoleOfBase(bases).ToString().ToLower()}.vck");
            foreach (TKeys t in temp)
            {
                if (t.Base.Name == bases.Name && t.Base.Region == bases.Region)
                {
                    if (t.Tkey != null)
                    {
                        return t;
                    }
                }
            }
            return null;

        }
        public void Download()
        {
            Injection.Download(this);
        }
        public GameConsoles GetConsoleOfBase(GameBases gb)
        {
            GameConsoles ret = new GameConsoles();
            bool cont = false;
            foreach(GameBases b in lNDS)
            {
                if(b.Name == gb.Name && b.Region == gb.Region)
                {
                    ret = GameConsoles.NDS;
                    cont = true;
                }
            }
            if (!cont)
            {
                foreach (GameBases b in lN64)
                {
                    if (b.Name == gb.Name && b.Region == gb.Region)
                    {
                        ret = GameConsoles.N64;
                        cont = true;
                    }
                }
            }
            if (!cont)
            {
                foreach (GameBases b in lNES)
                {
                    if (b.Name == gb.Name && b.Region == gb.Region)
                    {
                        ret = GameConsoles.NES;
                        cont = true;
                    }
                }
            }
            if (!cont)
            {
                foreach (GameBases b in lSNES)
                { if(b.Name == gb.Name && b.Region == gb.Region)
                    {
                        ret = GameConsoles.SNES;
                        cont = true;
                    }
                }
            }
            if (!cont)
            {
                foreach (GameBases b in lGBA)
                {
                    if (b.Name == gb.Name && b.Region == gb.Region)
                    {
                        ret = GameConsoles.GBA;
                        cont = true;
                    }
                }
            }
            return ret;
        }
        public List<bool> getInfoOfBase(GameBases gb)
        {
            List<bool> info = new List<bool>();
            if (Directory.Exists($@"{Settings.Default.BasePath}\{gb.Name.Replace(":","")} [{gb.Region}]"))
            {
                info.Add(true);
            }
            else
            {
                info.Add(false);
            }
            if (isKeySet(gb))
            {
                info.Add(true);
            }
            else
            {
                info.Add(false);
            }
            if (isCkeySet())
            {
                info.Add(true);
            }
            else
            {
                info.Add(false);
            }
            return info;
        }

        public void SetInjectPath()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if(result == DialogResult.OK)
                {
                    try
                    {
                        if (DirectoryIsEmpty(dialog.SelectedPath))
                        {
                            Settings.Default.OutPath = dialog.SelectedPath;
                            Settings.Default.SetOutOnce = true;
                            Settings.Default.Save();
                            UpdatePathSet();
                        }
                        else
                        {
                            DialogResult r = MessageBox.Show("Folder contains Files or Subfolders, do you really want to use this folder as the Inject Folder?", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (r == DialogResult.Yes)
                            {
                                Settings.Default.OutPath = dialog.SelectedPath;
                                Settings.Default.SetOutOnce = true;
                                Settings.Default.Save();
                                UpdatePathSet();
                            }
                            else
                            {
                                SetInjectPath();
                            }
                        }
                    }catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                        MessageBox.Show("An Error occured, please try again!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                  
                }
            }
            ArePathsSet();
        }
        public void SetBasePath()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    try
                    {
                        if (DirectoryIsEmpty(dialog.SelectedPath))
                        {
                            Settings.Default.BasePath = dialog.SelectedPath;
                            Settings.Default.SetBaseOnce = true;
                            Settings.Default.Save();
                            UpdatePathSet();
                        }
                        else
                        {
                            DialogResult r = MessageBox.Show("Folder contains Files or Subfolders, do you really want to use this folder as the Base Folder?", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (r == DialogResult.Yes)
                            {
                                Settings.Default.BasePath = dialog.SelectedPath;
                                Settings.Default.SetBaseOnce = true;
                                Settings.Default.Save();
                                UpdatePathSet();
                            }
                            else
                            {
                                SetBasePath();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        MessageBox.Show("An Error occured, please try again!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
            }
            ArePathsSet();
        }
        public void ArePathsSet()
        {
            if (ValidatePathsStillExist())
            {
                Settings.Default.PathsSet = true;
                Settings.Default.Save();
            }
            UpdatePathSet();
        }
        public bool DirectoryIsEmpty(string path)
        {
            int fileCount = Directory.GetFiles(path).Length;
            if (fileCount > 0)
            {
                return false;
            }

            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                if (!DirectoryIsEmpty(dir))
                {
                    return false;
                }
            }

            return true;
        }
    }
}