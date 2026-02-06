using GameBaseClassLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using UWUVCI_AIO_WPF.Classes;
using UWUVCI_AIO_WPF.Helpers;
using Path = System.IO.Path;

namespace UWUVCI_AIO_WPF.UI.Windows
{
    /// <summary>
    /// Interaktionslogik für ImageCreator.xaml
    /// </summary>  

    public partial class ImageCreator : Window, IDisposable
    {
        private static readonly string tempPath = PathResolver.GetTempPath();
        private static readonly string toolsPath = PathResolver.GetToolsPath();
        BootImage bi = new BootImage();
        Bitmap b;
        string console = "other";
        bool drc = false;
        private string backupcons;
        private bool _disposed;
        private Bitmap _customFrame;
        private bool _useCustomFrame;
        private bool _suppressPresetChange;
        private GameConsoles _consoleEnum;

        public ImageCreator(string name)
        {
            InitializeComponent();
            imageName.Content = name;
            _consoleEnum = GameConsoles.NES;
            
           
            if (name.ToLower().Contains("drc"))
            {
                drc = true;
                imageName_Copy.Content = "DRC IMAGE";
            }
        }
        public ImageCreator(GameConsoles console, string name) : this(name)
        {
            SetTemplate(console);
        }
        public ImageCreator(bool other, GameConsoles consoles, string name) : this(name)
        {
            Bitmap bit;
            _consoleEnum = consoles;
            if (consoles == GameConsoles.TG16)
            {
                bit = new Bitmap(other ? Properties.Resources.TGCD : Properties.Resources.TG16);
                SetPresetItems(new List<string> { other ? "TGCD" : "TG16" }, 0);
            }
            else
            {
                console = "GBC";
                bit = new Bitmap(other ? Properties.Resources.GBC : Properties.Resources.newgameboy);
                SetPresetItems(new List<string> { other ? "GBC" : "GB" }, 0);
            }
            bi.Frame = bit;
        }

        private void SetPresetFrame(Bitmap bit)
        {
            if (_useCustomFrame)
            {
                bit?.Dispose();
                return;
            }
            bi.Frame = bit;
        }

        private void SetTemplate(GameConsoles console)
        {
            Bitmap bit;
            _consoleEnum = console;
            this.console = console.ToString();
            switchs(Visibility.Visible);
            switch (console)
            {
                case GameConsoles.NDS:
                    bit = new Bitmap(Properties.Resources.NDS);

                    this.console = "NDS";
                    SetPresetItems(new List<string> { "NDS" }, 0);
                    break;
                case GameConsoles.N64:
                    bit = new Bitmap(Properties.Resources.N64);

                    SetPresetItems(new List<string> { "N64" }, 0);
                    break;
                case GameConsoles.NES:
                    bit = new Bitmap(Properties.Resources.NES);

                    SetPresetItems(new List<string> { "NES" }, 0);
                    break;
                case GameConsoles.GBA:
                    bit = new Bitmap(Properties.Resources.GBA);
                    this.console = "GBA";
                    SetPresetItems(new List<string> { "GBA" }, 0);
                    break;
                case GameConsoles.WII:
                    bit = new Bitmap(Properties.Resources.WII);
                    this.console = "WII";
                    switchs(Visibility.Collapsed);
                    snesonly.Visibility = Visibility.Hidden;
                    backupcons = "WII";
                    SetPresetItems(new List<string> { "Wii", "WiiWare", "Homebrew", "Alternative - Wii" }, 0);
                    break;
                case GameConsoles.GCN:
                    bit = new Bitmap(Properties.Resources.GCN);
                    SetPresetItems(new List<string> { "GameCube" }, 0);
                    break;
                case GameConsoles.MSX:
                    bit = new Bitmap(Properties.Resources.MSX);
                    SetPresetItems(new List<string> { "MSX" }, 0);
                    break;
                case GameConsoles.SNES:
                    bit = new Bitmap(Properties.Resources.SNES_PAL);
                    snesonly.Visibility = Visibility.Hidden;
                    SetPresetItems(new List<string> { "SNES - PAL", "SNES - NTSC", "Super Famicom" }, 0);
                    break;
                default:
                    bit = null;
                    SetPresetVisibility(false);
                    break;
            }
            SetPresetFrame(bit);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FileSelect_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel mvm = FindResource("mvm") as MainViewModel;
            string file = mvm.GetFilePath(false, false);
            if (!string.IsNullOrEmpty(file))
            {

                string copy = "";
                if (new FileInfo(file).Extension.Contains("tga"))
                {
                    using (Process conv = new Process())
                    {

                        conv.StartInfo.UseShellExecute = false;
                        conv.StartInfo.CreateNoWindow = true;
                        if (Directory.Exists(Path.Combine(tempPath, "image")))
                        {
                            Directory.Delete(Path.Combine(tempPath, "image"), true);
                        }
                        Directory.CreateDirectory(Path.Combine(tempPath, "image"));
                        conv.StartInfo.FileName = Path.Combine(toolsPath, "tga2png.exe");
                        conv.StartInfo.Arguments = $"-i \"{file}\" -o \"{Path.Combine(tempPath, "image")}\"";

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
                    copy = file;
                }
                try
                {
                    bi.TitleScreen = new Bitmap(copy);
                    b = bi.Create(console);
                    Image.Source = BitmapToImageSource(b);
                }
                catch
                {
                    Custom_Message cm = new Custom_Message("Image Issue", "The image you're trying to use will not work, please try a different image.");
                    try
                    {
                        cm.Owner = mvm.mw;
                    }
                    catch (Exception)
                    {
                        //left empty on purpose
                    }
                    cm.ShowDialog();
                }
            }
            enOv_Click(null, null);
        }

        private void FrameSelect_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel mvm = FindResource("mvm") as MainViewModel;
            string file = mvm.GetFilePath(false, false);
            if (string.IsNullOrEmpty(file))
                return;

            try
            {
                using var loaded = LoadBitmapFromFile(file);
                ValidateFrameBitmap(loaded);

                _customFrame?.Dispose();
                _customFrame = new Bitmap(loaded);
                _useCustomFrame = true;
                bi.Frame = new Bitmap(_customFrame);

                b = bi.Create(console);
                Image.Source = BitmapToImageSource(b);
            }
            catch
            {
                Custom_Message cm = new Custom_Message("Frame Issue", "The frame image must be 1280x720 and 24-bit or 32-bit.");
                try
                {
                    cm.Owner = mvm.mw;
                }
                catch (Exception)
                {
                    // left empty on purpose
                }
                cm.ShowDialog();
            }
        }

        private void FrameReset_Click(object sender, RoutedEventArgs e)
        {
            _useCustomFrame = false;
            _customFrame?.Dispose();
            _customFrame = null;
            ApplyCurrentPresetFrame();
            b = bi.Create(console);
            Image.Source = BitmapToImageSource(b);
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(@"bin\createdIMG"))
            {
                Directory.CreateDirectory(@"bin\createdIMG");
            }
            if (File.Exists(Path.Combine(@"bin\createdIMG", imageName.Content + ".png")))
            {
                File.Delete(Path.Combine(@"bin\createdIMG", imageName.Content + ".png"));
            }
            if (drc)
            {
                b = ResizeImage(b, 854, 480);
            }

            b.Save(Path.Combine(@"bin\createdIMG", imageName.Content + ".png"));


            Close();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text

        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void wind_Loaded(object sender, RoutedEventArgs e)
        {
            snesonly.Visibility = Visibility.Hidden;
            b = bi.Create(console);
            Image.Source = BitmapToImageSource(b);
        }

        private Bitmap LoadBitmapFromFile(string path)
        {
            if (!new FileInfo(path).Extension.Contains("tga"))
                return new Bitmap(path);

            using (Process conv = new Process())
            {
                conv.StartInfo.UseShellExecute = false;
                conv.StartInfo.CreateNoWindow = true;

                if (Directory.Exists(Path.Combine(tempPath, "image")))
                    Directory.Delete(Path.Combine(tempPath, "image"), true);

                Directory.CreateDirectory(Path.Combine(tempPath, "image"));
                conv.StartInfo.FileName = Path.Combine(toolsPath, "tga2png.exe");
                conv.StartInfo.Arguments = $"-i \"{path}\" -o \"{Path.Combine(tempPath, "image")}\"";

                conv.Start();
                conv.WaitForExit();

                foreach (string sFile in Directory.GetFiles(Path.Combine(tempPath, "image"), "*.png"))
                    return new Bitmap(sFile);
            }

            return new Bitmap(path);
        }

        private static void ValidateFrameBitmap(Bitmap frame)
        {
            if (frame.Width != 1280 || frame.Height != 720)
                throw new InvalidDataException("Frame size mismatch.");

            int bpp = System.Drawing.Image.GetPixelFormatSize(frame.PixelFormat);
            if (bpp != 24 && bpp != 32)
                throw new InvalidDataException("Frame bit depth mismatch.");
        }
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        void EnableOrDisbale(bool en)
        {
            GameName1.IsEnabled = en;
            GameName2.IsEnabled = en;
            ReleaseYearLabel.IsEnabled = en;
            RLDi.IsEnabled = en;
            RLEn.IsEnabled = en;
            ReleaseYear.IsEnabled = en;
            if (en && RLDi.IsChecked == true)
            {
                ReleaseYear.IsEnabled = false;
            }
            PLDi.IsEnabled = en;
            PLEn.IsEnabled = en;
            Players.IsEnabled = en;
            if (en && PLDi.IsChecked == true)
            {
                Players.IsEnabled = false;
            }
            PlayerLabel.IsEnabled = en;
            if (snesonly.IsVisible == true)
            {
                snesonly.IsEnabled = en;
            }
        }

        private void enOv_Click(object sender, RoutedEventArgs e)
        {
            if ((ovl.IsVisible == true && enOv.IsChecked == true))
            {
                EnableOrDisbale(true);
                b = bi.Create(console);
                Image.Source = BitmapToImageSource(b);
            }
            else
            {
                EnableOrDisbale(false);
                if (bi.TitleScreen != null)
                {
                    b = ResizeImage(bi.TitleScreen, 1280, 720); Image.Source = BitmapToImageSource(b);
                }
                else
                {
                    b = new Bitmap(1280, 720);
                    using (Graphics gfx = Graphics.FromImage(b))
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
                    {
                        gfx.FillRectangle(brush, 0, 0, 1280, 720);
                    }
                    Image.Source = BitmapToImageSource(b);
                }
            }
        }
        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        void DrawImage()
        {
            bi.NameLine1 = GameName1.Text;
            bi.NameLine2 = GameName2.Text;

            bi.Longname = !string.IsNullOrWhiteSpace(GameName2.Text);

            bi.Players = (PLEn.IsChecked == true && !string.IsNullOrWhiteSpace(Players.Text)) ? Convert.ToInt32(Players.Text) : 0;

            bi.Released = (RLEn.IsChecked == true && !string.IsNullOrWhiteSpace(ReleaseYear.Text)) ? Convert.ToInt32(ReleaseYear.Text) : 0;

            b = bi.Create(console);
            Image.Source = BitmapToImageSource(b);
        }

        private void ApplyCurrentPresetFrame()
        {
            if (PresetCombo.Visibility == Visibility.Visible && PresetCombo.SelectedIndex >= 0)
            {
                ApplyPresetSelection(PresetCombo.SelectedIndex);
                return;
            }

            if (console == "WII" && backupcons == "WII")
            {
                if (altwii.IsChecked == true)
                    SetPresetFrame(Properties.Resources.wii3New);
                else if (sfc.IsChecked == true)
                    SetPresetFrame(Properties.Resources.homebrew3);
                else if (sntsc.IsChecked == true)
                    SetPresetFrame(Properties.Resources.WIIWARE);
                else
                    SetPresetFrame(Properties.Resources.WII);
                return;
            }

            if (console == "GBA")
            {
                SetPresetFrame(Properties.Resources.GBA);
                return;
            }

            if (console == "NDS")
            {
                SetPresetFrame(Properties.Resources.NDS);
                return;
            }

            if (combo.IsVisible && combo.SelectedIndex >= 0)
            {
                if (combo.SelectedIndex == 0)
                    SetPresetFrame(Properties.Resources.SNES_PAL);
                else if (combo.SelectedIndex == 1)
                    SetPresetFrame(Properties.Resources.SNES_USA);
                else
                    SetPresetFrame(Properties.Resources.SFAM);
                return;
            }
        }

        private void SetPresetVisibility(bool visible)
        {
            PresetLabel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            PresetCombo.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetPresetItems(List<string> items, int selectedIndex)
        {
            _suppressPresetChange = true;
            PresetCombo.ItemsSource = items;
            PresetCombo.SelectedIndex = selectedIndex;
            _suppressPresetChange = false;
            SetPresetVisibility(true);
            ApplyPresetSelection(selectedIndex);
        }

        private void ApplyPresetSelection(int index)
        {
            if (_consoleEnum == GameConsoles.WII)
            {
                backupcons = "WII";
                if (index == 0)
                {
                    console = "WII";
                    switchs(Visibility.Hidden);
                    SetPresetFrame(Properties.Resources.WII);
                }
                else if (index == 1)
                {
                    console = "WII";
                    switchs(Visibility.Hidden);
                    SetPresetFrame(Properties.Resources.WIIWARE);
                }
                else if (index == 2)
                {
                    console = "WII";
                    switchs(Visibility.Hidden);
                    SetPresetFrame(Properties.Resources.homebrew3);
                }
                else
                {
                    console = "other";
                    switchs(Visibility.Visible);
                    SetPresetFrame(Properties.Resources.wii3New);
                }
                return;
            }

            switch (_consoleEnum)
            {
                case GameConsoles.GBA:
                    SetPresetFrame(Properties.Resources.GBA);
                    return;
                case GameConsoles.NDS:
                    SetPresetFrame(Properties.Resources.NDS);
                    return;
                case GameConsoles.N64:
                    SetPresetFrame(Properties.Resources.N64);
                    return;
                case GameConsoles.NES:
                    SetPresetFrame(Properties.Resources.NES);
                    return;
                case GameConsoles.GCN:
                    SetPresetFrame(Properties.Resources.GCN);
                    return;
                case GameConsoles.MSX:
                    SetPresetFrame(Properties.Resources.MSX);
                    return;
            }

            if (index == 0)
                SetPresetFrame(Properties.Resources.SNES_PAL);
            else if (index == 1)
                SetPresetFrame(Properties.Resources.SNES_USA);
            else
                SetPresetFrame(Properties.Resources.SFAM);
        }

        private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressPresetChange)
                return;

            ApplyPresetSelection(PresetCombo.SelectedIndex);
            b = bi.Create(console);
            Image.Source = BitmapToImageSource(b);
        }

        private void Players_TextChanged(object sender, TextChangedEventArgs e)
        {
            DrawImage();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                bi?.Dispose();
                b?.Dispose();
                _customFrame?.Dispose();
            }

            _disposed = true;
        }

        private void combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo.SelectedIndex == 0)
            {
                SetPresetFrame(Properties.Resources.SNES_PAL);

            }
            else if (combo.SelectedIndex == 1)
            {
                SetPresetFrame(Properties.Resources.SNES_USA);
            }
            else
            {
                SetPresetFrame(Properties.Resources.SFAM);
            }
            b = bi.Create(console);
            Image.Source = BitmapToImageSource(b);
        }

        private void pal_Click(object sender, RoutedEventArgs e)
        {
            if (console != "WII" && backupcons != "WII")
            {
                SetPresetFrame(pal.IsChecked == true ? Properties.Resources.SNES_PAL : Properties.Resources.SNES_USA);
            }
            else
            {
                console = "WII";
                switchs(Visibility.Hidden);

                SetPresetFrame(pal.IsChecked == true ? Properties.Resources.WII : Properties.Resources.WIIWARE);
            }

            b = bi.Create(console);
            Image.Source = BitmapToImageSource(b);
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (console != "WII" && backupcons != "WII")
            {
                backupcons = console;
                SetPresetFrame(Properties.Resources.SFAM);
            }
            else
            {
                backupcons = "WII";
                if (altwii.IsChecked == false)
                {
                    console = "WII";
                    switchs(Visibility.Hidden);
                    SetPresetFrame(Properties.Resources.homebrew3);
                }
                else
                {
                    switchs(Visibility.Visible);
                    console = "other";
                    SetPresetFrame(Properties.Resources.wii3New);
                }

            }

            b = bi.Create(console);
            Image.Source = BitmapToImageSource(b);
        }

        private void PLDi_Click(object sender, RoutedEventArgs e)
        {
            Players.IsEnabled = PLEn.IsChecked == true;

            DrawImage();
        }

        private void RLEn_Click(object sender, RoutedEventArgs e)
        {
            ReleaseYearLabel.IsEnabled = RLEn.IsChecked == true;

            DrawImage();
        }

        private void switchs(Visibility v)
        {
            if (v == Visibility.Hidden)
                v = Visibility.Collapsed;
            GameName1.Visibility = v;
            GameName2.Visibility = v;

            ReleaseYearGroup.Visibility = v;
            PlayersGroup.Visibility = v;

            if (v == Visibility.Hidden)
            {
                bi.NameLine1 = "";
                bi.NameLine2 = "";
                bi.Released = 0;
                bi.Players = 0;
            }
            else
            {
                bi.NameLine1 = GameName1.Text;
                bi.NameLine2 = GameName2.Text;
                if (!string.IsNullOrEmpty(ReleaseYear.Text))
                {
                    bi.Released = Convert.ToInt32(ReleaseYear.Text);
                }
                if (!string.IsNullOrEmpty(Players.Text))
                {
                    bi.Players = Convert.ToInt32(Players.Text);
                }

            }

        }
    }
}
