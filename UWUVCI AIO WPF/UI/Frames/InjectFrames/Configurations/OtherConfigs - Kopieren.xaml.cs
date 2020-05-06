﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UWUVCI_AIO_WPF.Properties;
using UWUVCI_AIO_WPF.UI.Windows;

namespace UWUVCI_AIO_WPF.UI.Frames.InjectFrames.Configurations
{
    /// <summary>
    /// Interaktionslogik für OtherConfigs.xaml
    /// </summary>
    public partial class TurboGrafX : Page, IDisposable
    {
        MainViewModel mvm;
        bool cd = false;
        public TurboGrafX()
        {
            InitializeComponent();
            mvm = FindResource("mvm") as MainViewModel;
            mvm.setThing(this);
            Injection.ToolTip = "Changing the extension of a ROM may result in a faulty inject.\nWe will not give any support in such cases";

        }
        public TurboGrafX(GameConfig c)
        {
            InitializeComponent();
            mvm = FindResource("mvm") as MainViewModel;
            mvm.setThing(this);
            mvm.GameConfiguration = c.Clone(); getInfoFromConfig();
            Injection.ToolTip = "Changing the extension of a ROM may result in a faulty inject.\nWe will not give any support in such cases";

        }
        private void SoundImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mvm.PlaySound();
        }

        private void sound_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (File.Exists(mvm.BootSound))
                {
                    if (!new FileInfo(mvm.BootSound).Extension.Contains("btsnd"))
                    {
                        SoundImg.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SoundImg.Visibility = Visibility.Hidden;
                    }
                }
            }
            catch (Exception)
            {

            }


        }
        public void Dispose()
        {

        }

        private void Set_Rom_Path(object sender, RoutedEventArgs e)
        {
            string path = string.Empty;
            if (!cd) path = mvm.GetFilePath(true, false);
            else path = mvm.turbocd();

            if (!CheckIfNull(path))
            {
                mvm.RomPath = path;
                mvm.RomSet = true;
                if (mvm.BaseDownloaded)
                {
                    mvm.CanInject = true;

                }
            }

        }

        private void InjectGame(object sender, RoutedEventArgs e)
        {
            if (File.Exists(tv.Text))
            {
                mvm.GameConfiguration.TGATv.ImgPath = tv.Text;
            }
            else if (!tv.Text.Equals("Added via Config") && !tv.Text.Equals("Downloaded from Cucholix Repo"))
            {
                mvm.GameConfiguration.TGATv.ImgPath = null;
            }
            if (File.Exists(ic.Text))
            {
                mvm.GameConfiguration.TGAIco.ImgPath = ic.Text;
            }
            else if (!ic.Text.Equals("Added via Config") && !ic.Text.Equals("Downloaded from Cucholix Repo"))
            {
                mvm.GameConfiguration.TGAIco.ImgPath = null;

            }
            if (File.Exists(log.Text))
            {
                mvm.GameConfiguration.TGALog.ImgPath = log.Text;
            }
            else if (!log.Text.Equals("Added via Config") && !log.Text.Equals("Downloaded from Cucholix Repo"))
            {
                mvm.GameConfiguration.TGALog.ImgPath = null;
            }
            if (File.Exists(drc.Text))
            {
                mvm.GameConfiguration.TGADrc.ImgPath = drc.Text;
            }
            else if (!drc.Text.Equals("Added via Config") && !drc.Text.Equals("Downloaded from Cucholix Repo"))
            {
                mvm.GameConfiguration.TGADrc.ImgPath = null;
            }
            mvm.Inject(false);
        }

        private void Set_TvTex(object sender, RoutedEventArgs e)
        {
            if (!Settings.Default.dont)
            {
                mvm.ImageWarning();
            }
            string path = mvm.GetFilePath(false, false);
            if (!CheckIfNull(path))
            {
                mvm.GameConfiguration.TGATv.ImgPath = path;
                mvm.GameConfiguration.TGATv.extension = new FileInfo(path).Extension;
                tv.Text = path;
                tvIMG.Visibility = Visibility.Visible;
            }
            else
            {
                if (path == "")
                {
                    mvm.GameConfiguration.TGATv.ImgPath = null;
                    tv.Text = "";
                    tvIMG.Visibility = Visibility.Hidden;
                }
            }

        }

        private void Set_DrcTex(object sender, RoutedEventArgs e)
        {
            if (!Settings.Default.dont)
            {
                mvm.ImageWarning();
            }
            string path = mvm.GetFilePath(false, false);
            if (!CheckIfNull(path))
            {
                mvm.GameConfiguration.TGADrc.ImgPath = path;
                mvm.GameConfiguration.TGADrc.extension = new FileInfo(path).Extension;
                drc.Text = path;
                drcIMG.Visibility = Visibility.Visible;
            }
            else
            {
                if (path == "")
                {
                    mvm.GameConfiguration.TGADrc.ImgPath = null;
                    drc.Text = "";
                    drcIMG.Visibility = Visibility.Hidden;
                }
            }

        }

        private void Set_IconTex(object sender, RoutedEventArgs e)
        {
            if (!Settings.Default.dont)
            {
                mvm.ImageWarning();
            }
            string path = mvm.GetFilePath(false, false);
            if (!CheckIfNull(path))
            {
                mvm.GameConfiguration.TGAIco.ImgPath = path;
                mvm.GameConfiguration.TGAIco.extension = new FileInfo(path).Extension;
                ic.Text = path;
                icoIMG.Visibility = Visibility.Visible;
            }
            else
            {
                if (path == "")
                {
                    mvm.GameConfiguration.TGAIco.ImgPath = null;
                    ic.Text = "";
                    icoIMG.Visibility = Visibility.Hidden;
                }
            }
        }

        private void Set_LogoTex(object sender, RoutedEventArgs e)
        {
            if (!Settings.Default.dont)
            {
                mvm.ImageWarning();
            }
            string path = mvm.GetFilePath(false, false);
            if (!CheckIfNull(path))
            {
                mvm.GameConfiguration.TGALog.ImgPath = path;
                mvm.GameConfiguration.TGALog.extension = new FileInfo(path).Extension;
                log.Text = path;
                logIMG.Visibility = Visibility.Visible;
            }
            else
            {
                if (path == "")
                {
                    mvm.GameConfiguration.TGALog.ImgPath = null;
                    log.Text = "";
                    logIMG.Visibility = Visibility.Hidden;
                }
            }


        }
        public void getInfoFromConfig()
        {
            rp.Text = "";
            mvm.RomPath = "";
            mvm.RomSet = false;
            mvm.gc2rom = "";
            tv.Text = mvm.GameConfiguration.TGATv.ImgPath;
            if (tv.Text.Length > 0)
            {
                tvIMG.Visibility = Visibility.Visible;
            }
            ic.Text = mvm.GameConfiguration.TGAIco.ImgPath;
            if (ic.Text.Length > 0)
            {
                icoIMG.Visibility = Visibility.Visible;
            }
            drc.Text = mvm.GameConfiguration.TGADrc.ImgPath;
            if (drc.Text.Length > 0)
            {
                drcIMG.Visibility = Visibility.Visible;
            }
            log.Text = mvm.GameConfiguration.TGALog.ImgPath;
            if (log.Text.Length > 0)
            {
                logIMG.Visibility = Visibility.Visible;
            }
            gn.Text = mvm.GameConfiguration.GameName;

        }
        private bool CheckIfNull(string s)
        {
            if (s == null || s.Equals(string.Empty))
            {
                return true;
            }
            return false;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            mvm.RomPath = null;
            mvm.RomSet = false;
            mvm.CanInject = false;
            if (cd)
            {
                cd = false;
                mvm.mw.tbTitleBar.Text = "UWUVCI AIO - TurboGrafX-16 VC INJECT";
                Injection.Content = "Select File";
            }

            else
            {
                cd = true;
                mvm.mw.tbTitleBar.Text = "UWUVCI AIO - TurboGrafX-CD VC INJECT";
                Injection.Content = "Set Path";
            }
        }

        private void gn_KeyUp(object sender, KeyEventArgs e)
        {

            /*Regex reg = new Regex("[^a-zA-Z0-9 é -]");
            string backup = string.Copy(gn.Text);
            gn.Text = reg.Replace(gn.Text, string.Empty);
            gn.CaretIndex = gn.Text.Length;
            if (gn.Text != backup)
            {
                gn.ScrollToHorizontalOffset(double.MaxValue);
            }*/




        }
        public void reset()
        {

            tv.Text = "";
            drc.Text = "";
            gn.Text = "";
            ic.Text = "";
            log.Text = "";
        }

        private void icoIMG_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void icoIMG_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ICOSHOW ics = new ICOSHOW(ic.Text);
            try
            {
                ics.Owner = mvm.mw;
            }
            catch (Exception)
            {

            }
            ics.ShowDialog();
        }

        private void tvIMG_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TDRSHOW t = new TDRSHOW(tv.Text, true);
            try
            {
                t.Owner = mvm.mw;
            }
            catch (Exception)
            {

            }
            t.ShowDialog();
        }

        private void drcIMG_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TDRSHOW t = new TDRSHOW(drc.Text, true);
            try
            {
                t.Owner = mvm.mw;
            }
            catch (Exception)
            {

            }
            t.ShowDialog();

        }

        private void logIMG_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LOGSHOW t = new LOGSHOW(log.Text);
            try
            {
                t.Owner = mvm.mw;
            }
            catch (Exception)
            {

            }
            t.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string path = mvm.GetFilePath(true, true);
            if (!CheckIfNull(path))
            {
                if (new FileInfo(path).Extension.Contains("wav"))
                {
                    if (mvm.ConfirmRiffWave(path))
                    {
                        mvm.BootSound = path;
                    }
                    else
                    {
                        Custom_Message cm = new Custom_Message("Incompatible WAV file", "Your WAV file needs to be a RIFF WAVE file which is 16 bit stereo and also 48000khz");
                        try
                        {
                            cm.Owner = mvm.mw;
                        }
                        catch (Exception)
                        {

                        }
                        cm.ShowDialog();
                    }
                }
                else
                {

                    mvm.BootSound = path;
                }
            }
            else
            {
                if (path == "")
                {
                    mvm.BootSound = null;
                    sound.Text = "";

                }
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {/*
            try
            {
                TitleKeys webbrowser = new TitleKeys("n64", "UWUVCI AIO - N64 Help");
                try
                {
                    webbrowser.Owner = mvm.mw;
                }
                catch (Exception)
                {

                }
                webbrowser.Show();
                mvm.mw.Hide();
            }
            catch (Exception)
            {
                Custom_Message cm = new Custom_Message("Not Implemented", "The Helppage for N64 is not implemented yet");
                try
                {
                    cm.Owner = mvm.mw;
                }
                catch (Exception)
                {

                }
                cm.Show();
            }*/
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                TitleKeys webbrowser = null;
                if (cd)
                {
                    webbrowser =  new TitleKeys("tgcd", "TurboGrafX Inject Guide");
                }
                else
                {
                    webbrowser = new TitleKeys("tg16", "TurboGrafX Inject Guide");
                }
                
                try
                {
                    webbrowser.Owner = mvm.mw;
                }
                catch (Exception)
                {

                }
                webbrowser.Show();
                mvm.mw.Hide();
            }
            catch (Exception)
            {
                string cns = "TGx16";
                if(cd) cns = "TGxCD";
                Custom_Message cm = new Custom_Message("Not Implemented", $"The Helppage for TurboGrafX is not implemented yet");
                try
                {
                    cm.Owner = mvm.mw;
                }
                catch (Exception)
                {

                }
                cm.Show();
            }
        }
    }
}
