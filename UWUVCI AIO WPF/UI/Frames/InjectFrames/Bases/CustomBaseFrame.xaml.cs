using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameBaseClassLibrary;
using System.IO;
using UWUVCI_AIO_WPF.Helpers;
using UWUVCI_AIO_WPF.UI.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using UWUVCI_AIO_WPF.Models;

namespace UWUVCI_AIO_WPF.UI.Frames.InjectFrames.Bases
{
    /// <summary>
    /// Interaktionslogik für CustomBaseFrame.xaml
    /// </summary>
    public partial class CustomBaseFrame : Page
    {
        GameConsoles console;
        GameBases bases;
        bool existing;
        MainViewModel mvm;
        public CustomBaseFrame(GameBases Base, GameConsoles console, bool existing)
        {
            InitializeComponent();
            tbCode.Text = "Code Folder not found";
            tbCode.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            tbContent.Text = "Content Folder not found";
            tbContent.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            tbMeta.Text = "Meta Folder not found";
            tbMeta.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            mvm = (MainViewModel)FindResource("mvm");
            bases = Base;
            mvm.isCkeySet();
            this.existing = existing;
            this.console = console;
            mvm.SetCBASE(this);
            if (mvm.Ckeys)
            {
                CK.Visibility = Visibility.Hidden;
                path.IsEnabled = true;
            }

        }
       


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tbCode.Text = "Code Folder not found";
            tbCode.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            tbContent.Text = "Content Folder not found";
            tbContent.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            tbMeta.Text = "Meta Folder not found";
            tbMeta.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            mvm.BaseDownloaded = false;
            mvm.CBasePath = null;
            //warning if using custom bases program may crash
           Custom_Message cm = new Custom_Message("Information", "If using Custom Bases there will be a chance that the program crashes if adding a wrong base (example: a normal wiiu game instead of a nds vc game).\nA custom base is containing either the code/content/meta folders or Installable files (*.h3, *.app, ...)\nIf you add a wrong base, we will not assist you fixing it, other than telling you to use another base.\nIf you agree to this please hit continue");
            try {
                cm.Owner = mvm.mw;
            }
            catch (Exception)
            {

            }
            cm.ShowDialog();
          
            if(mvm.choosefolder)
            {
                mvm.choosefolder = false;        //get folder
                using (var dialog = new CommonOpenFileDialog())
                {
                    dialog.IsFolderPicker = true;
                    if (DialogHelpers.TryShowDialog(dialog, out var selectedPath, mvm.mw, "CustomBase.SelectFolder"))
                    {
                        try
                        {
                            if (mvm.DirectoryIsEmpty(selectedPath))
                            {
                                Custom_Message cm1 =   new Custom_Message("Issue", "The Folder is Empty. Please choose another Folder.");
                                try
                                {
                                    cm.Owner = mvm.mw;
                                }
                                catch (Exception )
                                {

                                }
                                cm1.ShowDialog();
                                
                            }
                            else
                            { 
                               if(Directory.GetDirectories(selectedPath).Length > 3)
                                {
                                    Custom_Message cm1 =  new Custom_Message("Issue", "This Folder has too many subfolders. Please choose another folder");
                                    try
                                    {
                                        cm.Owner = mvm.mw;
                                    }
                                    catch (Exception )
                                    {

                                    }
                                    cm1.ShowDialog();
                                }
                                else
                                {
                                    if(Directory.GetDirectories(selectedPath).Length > 0)
                                    {
                                        //Code Content Meta
                                        if (Directory.Exists(System.IO.Path.Combine(selectedPath, "content")) && Directory.Exists(System.IO.Path.Combine(selectedPath, "code")) && Directory.Exists(System.IO.Path.Combine(selectedPath, "meta")))
                                        {
                                            //create new Game Config
                                            mvm.GameConfiguration.Console = console;
                                            mvm.GameConfiguration.CBasePath = selectedPath;
                                            GameBases gb = new GameBases();
                                            gb.Name = "Custom";
                                            gb.Region = Regions.EU;
                                            gb.Path = mvm.GameConfiguration.CBasePath;
                                            bar.Text = gb.Path;
                                            mvm.GameConfiguration.BaseRom = gb;
                                            tbCode.Text = "Code Folder exists";
                                            tbCode.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
                                            tbContent.Text = "Content Folder exists";
                                            tbContent.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
                                            tbMeta.Text = "Meta Folder exists";
                                            tbMeta.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
                                            mvm.BaseDownloaded = true;
                                        }
                                        else
                                        {
                                           Custom_Message cm1 = new Custom_Message("Issue", "This folder is not in the \"loadiine\" format");
                                            try
                                            {
                                                cm.Owner = mvm.mw;
                                            }
                                            catch (Exception )
                                            {

                                            }
                                            cm1.ShowDialog();
                                        }
                                    }
                                    else
                                    {
                                        //WUP
                                        if (Directory.GetFiles(selectedPath, "*.app").Length > 0 && Directory.GetFiles(selectedPath, "*.h3").Length > 0 && File.Exists(System.IO.Path.Combine(selectedPath, "title.tmd")) && File.Exists(System.IO.Path.Combine(selectedPath, "title.tik")))
                                        {
                                            if (mvm.CBaseConvertInfo())
                                            {
                                                //Convert to LOADIINE => save under bases/custom or custom_x path => create new config
                                                string path = Injection.ExtractBase(selectedPath, console);
                                                mvm.GameConfiguration = new GameConfig();
                                                mvm.GameConfiguration.Console = console;
                                                mvm.GameConfiguration.CBasePath = path;
                                                GameBases gb = new GameBases();
                                                gb.Name = "Custom";
                                                gb.Region = Regions.EU;
                                                gb.Path = mvm.GameConfiguration.CBasePath;
                                                mvm.CBasePath = mvm.GameConfiguration.CBasePath;
                                                mvm.GameConfiguration.BaseRom = gb;
                                                tbCode.Text = "Code Folder exists";
                                                tbCode.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
                                                tbContent.Text = "Content Folder exists";
                                                tbContent.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
                                                tbMeta.Text = "Meta Folder exists";
                                                tbMeta.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
                                                mvm.BaseDownloaded = true;
                                            }
                                        }
                                        else
                                        {
                                           Custom_Message cm1 = new Custom_Message("Issue", "This Folder does not contain needed NUS files");
                                            try
                                            {
                                                cm.Owner = mvm.mw;
                                            }
                                            catch (Exception )
                                            {

                                            }
                                            cm1.ShowDialog();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception )
                        {
                           
                        }
                    }

                }

               

            }

        }
        public void Reset()
        {
            tbCode.Text = "Code Folder not found";
            tbCode.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            tbContent.Text = "Content Folder not found";
            tbContent.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            tbMeta.Text = "Meta Folder not found";
            tbMeta.Foreground = new SolidColorBrush(Color.FromRgb(205, 50, 50));
            mvm = (MainViewModel)FindResource("mvm");
        }

        private void CK_Click(object sender, RoutedEventArgs e)
        {
            mvm.EnterKey(true);
            if (mvm.Ckeys)
            {
                CK.Visibility = Visibility.Hidden;
                path.IsEnabled = true;
            }

        }

        private void AddCustomBase_Click(object sender, RoutedEventArgs e)
        {
            if (mvm == null)
                return;

            string name = (CustomNameBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                SetCustomStatus("Please enter a unique name for the custom base.", isError: true);
                return;
            }

            if (mvm.LBases.Any(b => b != null && string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                SetCustomStatus("A base with this name already exists. Choose a different name.", isError: true);
                return;
            }

            if (!TryNormalizeLength(CustomTitleIdBox.Text, 16, out string titleId))
            {
                SetCustomStatus("Title ID must be 16 characters.", isError: true);
                return;
            }

            if (!TryNormalizeLength(CustomTitleKeyBox.Text, 32, out string titleKey))
            {
                SetCustomStatus("Title Key must be 32 characters.", isError: true);
                return;
            }

            var customBase = new GameBases
            {
                Name = name,
                Region = Regions.EU,
                Tid = titleId,
                KeyHash = -1
            };

            if (!mvm.SaveTitleKeyForBase(customBase, titleKey, console))
            {
                SetCustomStatus("Failed to save the custom base. Please try again.", isError: true);
                return;
            }

            mvm.GetBases(console);

            if (mvm.bcf != null)
                mvm.bcf.RefreshBasesAndSelect(name);

            SetCustomStatus("Custom base saved. Select it from the base list to download.", isError: false);
        }

        private void CustomKeyFromOtp_Click(object sender, RoutedEventArgs e)
        {
            if (mvm == null)
                return;

            CustomTitleKeyBox.Text = mvm.ReadCkeyFromOtp();
            SetCustomStatus("Key loaded from otp.bin (not verified).", isError: false);
        }

        private void SetCustomStatus(string message, bool isError)
        {
            if (CustomBaseStatus == null)
                return;

            CustomBaseStatus.Text = message;
            CustomBaseStatus.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(205, 50, 50))
                : (Brush)FindResource("AppMutedTextBrush");
        }

        private static bool TryNormalizeLength(string input, int length, out string normalized)
        {
            normalized = (input ?? string.Empty).Trim().Replace(" ", "");
            return normalized.Length == length;
        }
    }
}
