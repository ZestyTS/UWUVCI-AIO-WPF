using GameBaseClassLibrary;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UWUVCI_AIO_WPF.UI.Windows;


namespace UWUVCI_AIO_WPF.UI.Frames.InjectFrames.Bases
{
    /// <summary>
    /// Interaktionslogik für NonCustomBaseFrame.xaml
    /// </summary>
    public partial class NonCustomBaseFrame : Page
    {
        MainViewModel mvm;
        GameBases Base;
        BaseContainerFrame bc;
        bool ex;
        GameConsoles consoles;
        public NonCustomBaseFrame(GameBases Base, GameConsoles console, bool existing, BaseContainerFrame bcf)
        {
            InitializeComponent();
            mvm = (MainViewModel)FindResource("mvm");
            bc = bcf;
            this.Base = Base;
            ex = existing;
            consoles = console;
            createConfig(Base, console);
            checkStuff(mvm.getInfoOfBase(Base, consoles));
        }
       
        public NonCustomBaseFrame()
        {
            InitializeComponent();
            mvm = (MainViewModel)FindResource("mvm");
            
        }
        private void createConfig(GameBases Base, GameConsoles console)
        {
            
            mvm.GameConfiguration.BaseRom = Base;
            mvm.GameConfiguration.Console = console;
           
        }
        private void checkStuff(List<bool> info)
        {
            mvm.CanInject = false;
            mvm.Injected = false;
            if (info[0])
            {
                tbDWNL.Text = "Base Downloaded";
                tbDWNL.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
                btnDwnlnd.Content = "Redownload";
            }
            if (info[1])
            {
                TK.Visibility = Visibility.Hidden;
                tbTK.Text = "TitleKey Entered";
                tbTK.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
            }
            else
            {
                TK.Visibility = Visibility.Visible;
            }
            if (info[2])
            {
                CK.Visibility = Visibility.Hidden;
                tbCK.Text = "CommonKey Entered";
                tbCK.Foreground = new SolidColorBrush(Color.FromRgb(50, 205, 50));
            }
            else
            {
                CK.Visibility = Visibility.Visible;
            }

            if(info[1] && info[2])
            {
                btnDwnlnd.IsEnabled = true;
                if (info[0])
                {
                    mvm.BaseDownloaded = true;
                    if (mvm.RomSet) mvm.CanInject = true;
                }
                else
                {
                    mvm.BaseDownloaded = false;
                   
                }
            }

            btnDeleteCustom.Visibility = MainViewModel.IsCustomDownloadBase(Base)
                ? Visibility.Visible
                : Visibility.Collapsed;
            
        }

        private void btnDwnlnd_Click(object sender, RoutedEventArgs e)
        {
            
                mvm.Download();
            Thread.Sleep(500);
                checkStuff(mvm.getInfoOfBase(Base, consoles));
         
           
            
        }

        private void btnDwnlnd_Copy_Click(object sender, RoutedEventArgs e)
        {
            mvm.GbTemp = Base;
            mvm.EnterKey(false, MainViewModel.IsCustomDownloadBase(Base));
            checkStuff(mvm.getInfoOfBase(Base, consoles));
        }

        private void btnDwnlnd_Copy1_Click(object sender, RoutedEventArgs e)
        {
            mvm.EnterKey(true);
            checkStuff(mvm.getInfoOfBase(Base, consoles));
        }

        private void btnDeleteCustom_Click(object sender, RoutedEventArgs e)
        {
            if (!MainViewModel.IsCustomDownloadBase(Base))
                return;

            var result = UWUVCI_MessageBox.Show(
                "Delete Custom Base",
                $"Delete \"{Base.Name}\" from your custom base list?",
                UWUVCI_MessageBoxType.YesNo,
                UWUVCI_MessageBoxIcon.Warning,
                mvm.mw,
                true);

            if (result != UWUVCI_MessageBoxResult.Yes)
                return;

            if (!mvm.RemoveCustomBase(Base, consoles))
            {
                UWUVCI_MessageBox.Show(
                    "Delete Failed",
                    "Failed to delete this custom base. Please try again.",
                    UWUVCI_MessageBoxType.Ok,
                    UWUVCI_MessageBoxIcon.Error,
                    mvm.mw,
                    true);
                return;
            }

            mvm.GetBases(consoles);
            if (bc != null)
                bc.RefreshBasesAndSelect("Custom");
        }
    }
}

