using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using UWUVCI_AIO_WPF.Models;

namespace UWUVCI_AIO_WPF.Helpers
{
    public class JsonSettingsManager
    {
        private static string AppDataPath;
        public static string SettingsFile;

        public static JsonAppSettings Settings { get; private set; } = new JsonAppSettings();

        static JsonSettingsManager()
        {
            EnsureStoragePathInitialized();
        }

        private static void EnsureStoragePathInitialized()
        {
            if (!string.IsNullOrWhiteSpace(AppDataPath) && !string.IsNullOrWhiteSpace(SettingsFile))
                return;

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

                    string candidateDir = Path.Combine(root, "UWUVCI-V3");
                    Directory.CreateDirectory(candidateDir);

                    AppDataPath = candidateDir;
                    SettingsFile = Path.Combine(AppDataPath, "settings.json");
                    return;
                }
                catch
                {
                    // Try next location.
                }
            }

            AppDataPath = Path.Combine(Path.GetTempPath(), "UWUVCI-V3");
            SettingsFile = Path.Combine(AppDataPath, "settings.json");
            try { Directory.CreateDirectory(AppDataPath); } catch { }
        }

        public static void LoadSettings()
        {
            EnsureStoragePathInitialized();

            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    Settings = JsonConvert.DeserializeObject<JsonAppSettings>(json) ?? new JsonAppSettings();
                }
                else
                {
                    Settings = new JsonAppSettings();
                }

                string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

                string defaultTools;
                string defaultTemp;

                if (ToolRunner.HostIsMac())
                {
                    defaultTools = Path.Combine(home, "Library", "ApplicationSupport", "UWUVCI-V3", "Tools");
                    defaultTemp = Path.Combine(home, "Library", "ApplicationSupport", "UWUVCI-V3", "temp");
                }
                else if (ToolRunner.HostIsLinux())
                {
                    defaultTools = Path.Combine(home, ".uwuvci-v3", "Tools");
                    defaultTemp = Path.Combine(home, ".uwuvci-v3", "temp");
                }
                else
                {
                    defaultTools = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Tools");
                    defaultTemp = Path.Combine(Directory.GetCurrentDirectory(), "bin", "temp");
                }

                if (string.IsNullOrWhiteSpace(Settings.ToolsPath))
                    Settings.ToolsPath = defaultTools;

                if (string.IsNullOrWhiteSpace(Settings.TempPath))
                    Settings.TempPath = defaultTemp;

                if (Settings.FileCopyParallelism <= 0)
                    Settings.FileCopyParallelism = 6;

                if (string.IsNullOrWhiteSpace(Settings.Theme))
                    Settings.Theme = "Dark";
                Settings.Theme = ThemeManager.NormalizeTheme(Settings.Theme);

                SaveSettings();
                ToolRunner.InitializePaths(Settings.ToolsPath, Settings.TempPath);
            }
            catch (Exception ex)
            {
                Logger.Log("LoadSettings failed: " + ex);
                Settings = new JsonAppSettings();
                SaveSettings();
            }
        }

        public static void SaveSettings()
        {
            EnsureStoragePathInitialized();

            string caller = null;
#if DEBUG
            try
            {
                var st = new StackTrace();
                caller = st.GetFrame(1)?.GetMethod()?.DeclaringType?.FullName + "." + st.GetFrame(1)?.GetMethod()?.Name;
            }
            catch { }
#endif

            try
            {
                if (!Directory.Exists(AppDataPath))
                    Directory.CreateDirectory(AppDataPath);
            }
            catch (Exception ex)
            {
                Logger.Log("Settings directory could not be created: " + ex);
                return;
            }

            if (!IsFileWritable(SettingsFile))
            {
                Logger.Log("Settings file is not writable. Path: " + SettingsFile);
                return;
            }

            int retryCount = 0;
            const int maxRetry = 3;
            const int delayBetweenRetries = 1000;

            while (retryCount < maxRetry)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);

                    if (File.Exists(SettingsFile))
                    {
                        var existing = File.ReadAllText(SettingsFile);
                        if (string.Equals(existing, json, StringComparison.Ordinal))
                        {
#if DEBUG
                            if (!string.IsNullOrWhiteSpace(caller))
                                Console.WriteLine($"Settings unchanged; skip save (caller {caller}).");
#endif
                            break;
                        }
                    }

                    File.WriteAllText(SettingsFile, json);
                    Console.WriteLine("Settings saved successfully.");
#if DEBUG
                    if (!string.IsNullOrWhiteSpace(caller))
                        Console.WriteLine($"Settings saved by {caller}.");
#endif
                    break;
                }
                catch (IOException ex)
                {
                    retryCount++;
                    Logger.Log($"Error saving settings (attempt {retryCount}/{maxRetry}): {ex}");

                    if (retryCount < maxRetry)
                        System.Threading.Thread.Sleep(delayBetweenRetries);
                    else
                        Logger.Log("Failed to save settings after multiple attempts.");
                }
                catch (Exception ex)
                {
                    Logger.Log("Unexpected error saving settings: " + ex);
                    break;
                }
            }
        }

        public static bool IsFileWritable(string path)
        {
            try
            {
                using FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
