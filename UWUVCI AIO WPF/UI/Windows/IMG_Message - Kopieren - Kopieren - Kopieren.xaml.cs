﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using UWUVCI_AIO_WPF.UI.Frames.InjectFrames.Configurations;
using Path = System.IO.Path;

namespace UWUVCI_AIO_WPF.UI.Windows
{
    /// <summary>
    /// Interaktionslogik für IMG_Message.xaml
    /// </summary>
    public partial class LOGSHOW : Window, IDisposable
    {
        private static readonly string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "temp");
        private static readonly string toolsPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Tools");
        string copy = "";
        string pat = "";
        BitmapImage bitmap = new BitmapImage();
        public LOGSHOW(string path)
        {
            try
            {
                if (Owner?.GetType() != typeof(MainWindow))
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
            catch (Exception)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            pat = string.Copy(path);
            InitializeComponent();
            if (Directory.Exists(Path.Combine(tempPath, "image"))) Directory.Delete(Path.Combine(tempPath, "image"), true);
            Directory.CreateDirectory(Path.Combine(tempPath, "image"));
            if (path == "Added via Config")
            {
                File.WriteAllBytes(Path.Combine(tempPath, "image", "log." + (FindResource("mvm") as MainViewModel).GameConfiguration.TGALog.extension), (FindResource("mvm") as MainViewModel).GameConfiguration.TGALog.ImgBin);
                pat = Path.Combine(tempPath, "image", "log." + (FindResource("mvm") as MainViewModel).GameConfiguration.TGALog.extension);
            }
            if (new FileInfo(pat).Extension.Contains("tga"))
            {
                using (Process conv = new Process())
                {

                    conv.StartInfo.UseShellExecute = false;
                    conv.StartInfo.CreateNoWindow = true;


                    conv.StartInfo.FileName = Path.Combine(toolsPath, "tga2png.exe");
                    conv.StartInfo.Arguments = $"-i \"{pat}\" -o \"{Path.Combine(tempPath, "image")}\"";

                    conv.Start();
                    conv.WaitForExit();

                    foreach (string sFile in Directory.GetFiles(Path.Combine(tempPath, "image"), "*.png"))
                    {
                        copy = sFile;
                    }
                }
            }
            else
            {
                copy = pat;
            }


            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(copy, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            img.Source = image;
            if(path == "Added via Config")
            {
                File.Delete(pat);
            }


        }

        public void Dispose()
        {
           
        }

        private void Canc_Click(object sender, RoutedEventArgs e)
        {
            bitmap.UriSource = null;
            Close();

        }

        private void wind_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (FindResource("mvm") as MainViewModel).mw.Topmost = true;
        }

        private void wind_Closed(object sender, EventArgs e)
        {
            (FindResource("mvm") as MainViewModel).mw.Topmost = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel mvm = FindResource("mvm") as MainViewModel;
            switch (mvm.GameConfiguration.Console)
            {
                case GameBaseClassLibrary.GameConsoles.NDS:
                case GameBaseClassLibrary.GameConsoles.NES:
                case GameBaseClassLibrary.GameConsoles.SNES:
                case GameBaseClassLibrary.GameConsoles.MSX:
                    (mvm.Thing as OtherConfigs).clearImages(3);
                    break;
                case GameBaseClassLibrary.GameConsoles.GBA:
                    (mvm.Thing as GBA).clearImages(3);
                    break;
                case GameBaseClassLibrary.GameConsoles.WII:
                    if(mvm.test == GameBaseClassLibrary.GameConsoles.GCN)
                    {
                        (mvm.Thing as GCConfig).clearImages(3);
                    }
                    else
                    {
                        (mvm.Thing as WiiConfig).clearImages(3);
                    }
                    break;
                case GameBaseClassLibrary.GameConsoles.N64:
                    (mvm.Thing as N64Config).clearImages(3);
                    break;
                case GameBaseClassLibrary.GameConsoles.TG16:
                    (mvm.Thing as TurboGrafX).clearImages(3);
                    break;
            }
            Close();
        }
    }
}
