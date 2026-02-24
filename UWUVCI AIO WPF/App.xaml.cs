using GameBaseClassLibrary;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using UWUVCI_AIO_WPF.Classes;
using UWUVCI_AIO_WPF.Helpers;
using UWUVCI_AIO_WPF.Services;
using UWUVCI_AIO_WPF.UI.Windows;

namespace UWUVCI_AIO_WPF
{
    public partial class App : Application
    {
        System.Timers.Timer t = new System.Timers.Timer(5000);
        private StartupEventArgs _startupArgs;
        public static bool IsUnofficialBuild { get; private set; }
        public static string UnofficialBuildReason { get; private set; }
        private static string AppDataPath = ResolveWritableAppDataPath();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ConfigureGlobalExceptionHandlers();
            Logger.EnsureInitialized();
            Logger.Log("Application startup begin.");

            try
            {
                RuntimeEnvironment.ApplyWineCompatibilityDefaults();

                if (!Directory.Exists(AppDataPath))
                    Directory.CreateDirectory(AppDataPath);

                Console.SetOut(new ConsoleLoggerWriter());

                if (IsRunningFromOneDrive())
                {
                    UWUVCI_MessageBox.Show(
                        "Error: OneDrive Detected",
                        "ZestyTS' UWUVCI cannot be run from a OneDrive folder due to compatibility issues.\n\n" +
                        "Please move it to another location (e.g., C:\\Programs or C:\\Users\\YourName\\UWUVCI_AIO) before launching.",
                        UWUVCI_MessageBoxType.Ok,
                        UWUVCI_MessageBoxIcon.Error
                    );
                    Environment.Exit(1);
                }

                EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewDragOverEvent, new DragEventHandler(GlobalTextBox_PreviewDragOver));
                EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewDropEvent, new DragEventHandler(GlobalTextBox_PreviewDrop));

                _startupArgs = e;

                JsonSettingsManager.LoadSettings();
                ThemeManager.ApplyTheme(JsonSettingsManager.Settings.Theme);

                var installCheck = StartupValidation.CheckLocalInstall();
                if (!installCheck.Success)
                {
                    UWUVCI_MessageBox.Show(
                        "License Verification Failed",
                        "This copy of ZestyTS' UWUVCI V3 appears to be invalid or was copied from another system.\n\n" +
                        "Please download a legitimate copy from the official source.",
                        UWUVCI_MessageBoxType.Ok,
                        UWUVCI_MessageBoxIcon.Error
                    );
                    Environment.Exit(1);
                    return;
                }

                var integrityCheck = StartupValidation.CheckReleaseIntegrity();
                if (!integrityCheck.Success)
                {
                    IsUnofficialBuild = true;
                    UnofficialBuildReason = integrityCheck.Reason;
                    Logger.Log($"Unofficial build detected: {integrityCheck.Reason}");
                }

                RuntimeEnvironment.ApplyInvariantCulture();

                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                string currentVersionString = currentVersion?.ToString() ?? "0.0.0.0";
                string lastVersionSeen = JsonSettingsManager.Settings.LastVersionSeen ?? "0.0.0.0";

                bool shouldShowTutorial = false;

                if (JsonSettingsManager.Settings.IsFirstLaunch)
                {
                    MarkTutorialRequired(ref shouldShowTutorial);
                }
                else if (Version.TryParse(lastVersionSeen, out var lastVersion) && currentVersion > lastVersion)
                {
                    MarkTutorialRequired(ref shouldShowTutorial);
                }
                else if (JsonSettingsManager.Settings.ForceTutorialOnNextLaunch)
                {
                    MarkTutorialRequired(ref shouldShowTutorial);
                }
                else if (!JsonSettingsManager.Settings.HasAcknowledgedTutorial)
                {
                    MarkTutorialRequired(ref shouldShowTutorial);
                }

                if (shouldShowTutorial)
                {
                    new TutorialWizard().ShowDialog();

                    JsonSettingsManager.Settings.IsFirstLaunch = false;
                    JsonSettingsManager.Settings.LastVersionSeen = currentVersionString;
                    JsonSettingsManager.Settings.ForceTutorialOnNextLaunch = false;
                    JsonSettingsManager.SaveSettings();
                }
                else
                {
                    LaunchMainApplication(e);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Fatal startup exception: " + ex);
                ShowFatalStartupDialog(ex);
                Shutdown(-1);
            }
        }

        private static void GlobalTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private bool IsRunningFromOneDrive()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            return exePath.ToLower().Contains("onedrive");
        }

        private static void GlobalTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    TextBox txtBox = sender as TextBox;
                    if (txtBox != null)
                    {
                        if (Current.MainWindow is MainWindow mainWindow &&
                            mainWindow.Content is Grid grid &&
                            grid.DataContext is MainViewModel mvm)
                        {
                            if (mvm.GameConfiguration.Console == GameConsoles.WII || mvm.GameConfiguration.Console == GameConsoles.GCN)
                                return;

                            string filePath = files[0];

                            txtBox.IsReadOnly = false;
                            txtBox.Text = filePath;
                            txtBox.IsReadOnly = true;

                            switch (txtBox.Name)
                            {
                                case "rp":
                                    mvm.RomSet = true;
                                    mvm.RomPath = filePath;
                                    if (mvm.BaseDownloaded)
                                        mvm.CanInject = true;

                                    switch (mvm.GameConfiguration.Console)
                                    {
                                        case GameConsoles.NDS: mvm.getBootIMGNDS(mvm); break;
                                        case GameConsoles.NES: mvm.getBootIMGNES(mvm); break;
                                        case GameConsoles.SNES: mvm.getBootIMGSNES(mvm); break;
                                        case GameConsoles.MSX: mvm.getBootIMGMSX(mvm); break;
                                        case GameConsoles.N64: mvm.getBootIMGN64(mvm); break;
                                        case GameConsoles.GBA:
                                            var fileExtension = Path.GetExtension(filePath).ToLower();
                                            if (fileExtension != ".gb" && fileExtension != ".gbc")
                                                mvm.getBootIMGGBA(mvm);
                                            break;
                                        case GameConsoles.TG16: mvm.getBootIMGTG(mvm); break;
                                    }
                                    break;

                                case "ic": mvm.GameConfiguration.TGAIco.ImgPath = filePath; break;
                                case "tv": mvm.GameConfiguration.TGATv.ImgPath = filePath; break;
                                case "drc": mvm.GameConfiguration.TGADrc.ImgPath = filePath; break;
                                case "log": mvm.GameConfiguration.TGALog.ImgPath = filePath; break;
                                case "ini": mvm.GameConfiguration.N64Stuff.INIPath = filePath; break;
                                case "sound": mvm.BootSound = filePath; break;
                            }
                        }
                    }
                }
            }
        }

        public void LaunchMainApplication()
        {
            LaunchMainApplication(_startupArgs);
        }

        private void LaunchMainApplication(StartupEventArgs e)
        {
            if (Directory.Exists(@"custom") && File.Exists(@"custom\main.dol"))
            {
                string toolsDir = PathResolver.GetToolsPath();
                if (!Directory.Exists(toolsDir))
                    Directory.CreateDirectory(toolsDir);

                File.Copy(@"custom\main.dol", toolsDir + @"\nintendont.dol", true);
                File.Copy(@"custom\main.dol", toolsDir + @"\nintendont_force.dol", true);
            }

            bool check = true;
            bool bypass = false;
            if (e.Args.Length >= 1)
            {
                foreach (var s in e.Args)
                {
                    if (s == "--skip") check = false;
                    if (s == "--spacebypass") bypass = true;
                }
            }

            Process[] pname = Process.GetProcessesByName("UWUVCI AIO");
            if (pname.Length > 1 && check)
            {
                t.Elapsed += KillProg;
                t.Start();
                Custom_Message cm = new Custom_Message("Another Instance Running",
                    "You already have another instance of ZestyTS' UWUVCI running.\nThis instance will terminate in 5 seconds.");
                cm.ShowDialog();
                KillProg(null, null);
            }
            else
            {
                MainWindow wnd = new MainWindow();
                double height = SystemParameters.PrimaryScreenHeight;
                double width = SystemParameters.PrimaryScreenWidth;

                if (width < 1150 || height < 700)
                {
                    t.Elapsed += KillProg;
                    t.Start();
                    Custom_Message cm = new Custom_Message("Resolution not supported",
                        "Your screen resolution is not supported.\nPlease use at least 1152x864 or adjust your zoom level.\nThis instance will terminate in 5 seconds.");
                    cm.ShowDialog();
                    KillProg(null, null);
                }

                if (bypass)
                    wnd.allowBypass();

                if (e.Args.Length >= 1 && e.Args[0] == "--debug")
                    wnd.setDebug(bypass);

                wnd.Show();
            }
        }

        private void KillProg(object sender, ElapsedEventArgs e)
        {
            t.Stop();
            Environment.Exit(1);
        }

        private static void MarkTutorialRequired(ref bool shouldShowTutorial)
        {
            const bool tutorialDefault = true;
            shouldShowTutorial = tutorialDefault;
        }

        private void ConfigureGlobalExceptionHandlers()
        {
            DispatcherUnhandledException -= App_DispatcherUnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Log("Dispatcher unhandled exception: " + e.Exception);
            e.Handled = false;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log("AppDomain unhandled exception: " + (e.ExceptionObject?.ToString() ?? "null"));
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger.Log("TaskScheduler unobserved exception: " + e.Exception);
            e.SetObserved();
        }

        private static string ResolveWritableAppDataPath()
        {
            string[] candidateRoots =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Path.GetTempPath()
            };

            foreach (string root in candidateRoots)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(root))
                        continue;

                    string candidate = Path.Combine(root, "UWUVCI-V3");
                    Directory.CreateDirectory(candidate);
                    return candidate;
                }
                catch
                {
                    // Try next location.
                }
            }

            return Path.Combine(Path.GetTempPath(), "UWUVCI-V3");
        }

        private static void ShowFatalStartupDialog(Exception ex)
        {
            string logPath = string.IsNullOrWhiteSpace(Logger.LogFilePath) ? "(unavailable)" : Logger.LogFilePath;
            MessageBox.Show(
                "UWUVCI hit a startup error and could not continue.\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"Log file: {logPath}",
                "UWUVCI Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
