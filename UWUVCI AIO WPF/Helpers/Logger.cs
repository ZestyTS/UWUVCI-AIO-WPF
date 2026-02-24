using System;
using System.Collections.Generic;
using System.IO;
using UWUVCI_AIO_WPF.Services;

namespace UWUVCI_AIO_WPF.Helpers
{
    public static class Logger
    {
        private static readonly object Sync = new object();
        private static string logDirectory;
        private static string logFilePath;

        public static string LogDirectory => logDirectory ?? string.Empty;
        public static string LogFilePath => logFilePath ?? string.Empty;
        public static bool IsInitialized => !string.IsNullOrWhiteSpace(logFilePath);

        static Logger()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (IsInitialized)
                return;

            lock (Sync)
            {
                if (IsInitialized)
                    return;

                foreach (string candidate in GetCandidateLogDirectories())
                {
                    if (TryInitializeAt(candidate))
                        return;
                }
            }
        }

        public static void Log(string message)
        {
            EnsureInitialized();
            if (!IsInitialized)
                return;

            try
            {
                lock (Sync)
                {
                    using StreamWriter sw = new StreamWriter(logFilePath, true);
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch (Exception)
            {
                // If logging fails, there is nothing more to do.
            }
        }

        private static bool TryInitializeAt(string candidateDirectory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(candidateDirectory))
                    return false;

                Directory.CreateDirectory(candidateDirectory);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string candidateFile = Path.Combine(candidateDirectory, $"log_{timestamp}.txt");

                using (FileStream fs = new FileStream(candidateFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Logger initialized.");
                }

                logDirectory = candidateDirectory;
                logFilePath = candidateFile;
                CleanupOldLogs(7);
                WriteStartupFingerprint();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger init failed at '{candidateDirectory}': {ex.Message}");
                return false;
            }
        }

        private static IEnumerable<string> GetCandidateLogDirectories()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
                yield return Path.Combine(localAppData, "UWUVCI-V3", "Logs");

            string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(roamingAppData))
                yield return Path.Combine(roamingAppData, "UWUVCI-V3", "Logs");

            string tempPath = Path.GetTempPath();
            if (!string.IsNullOrWhiteSpace(tempPath))
                yield return Path.Combine(tempPath, "UWUVCI-V3", "Logs");
        }

        private static void CleanupOldLogs(int daysToKeep)
        {
            if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
                return;

            try
            {
                foreach (var file in Directory.GetFiles(logDirectory, "tool-*.txt"))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        if (fi.CreationTime < DateTime.Now.AddDays(-daysToKeep))
                            fi.Delete();
                    }
                    catch { }
                }

                foreach (var file in Directory.GetFiles(logDirectory, "log_*.txt"))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        if (fi.CreationTime < DateTime.Now.AddDays(-daysToKeep))
                            fi.Delete();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clean up old logs: {ex.Message}");
            }
        }

        private static void WriteStartupFingerprint()
        {
            try
            {
                string fp = DeviceFingerprint.GetHashedFingerprint();
                if (string.IsNullOrWhiteSpace(fp))
                    return;

                Log("Device fingerprint (hashed): " + fp);
            }
            catch
            {
                // Ignore fingerprint logging failures.
            }
        }
    }
}
