using System;
using System.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace UWUVCI_AIO_WPF.Helpers
{
    public static class DialogHelpers
    {
        public static bool TryShowDialog(System.Windows.Forms.OpenFileDialog dialog, out string fileName, out string[] fileNames, Window owner = null, string context = null)
        {
            fileName = string.Empty;
            fileNames = Array.Empty<string>();

            if (dialog == null)
                return false;

            bool underWine = ShouldAvoidWinFormsDialogs();
            try
            {
                // WinForms supports forcing the legacy dialog implementation; this tends to be more robust under Wine.
                dialog.AutoUpgradeEnabled = false;

                var res = owner == null ? dialog.ShowDialog() : dialog.ShowDialog(new Win32WindowWrapper(owner));
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    fileName = dialog.FileName;
                    fileNames = dialog.FileNames ?? Array.Empty<string>();
                    Log($"Dialog OK (WinForms OpenFileDialog){FormatContext(context)}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"WinForms OpenFileDialog failed{FormatContext(context)}: {ex}");
            }

            // Under Wine-like runtimes, WPF's OpenFileDialog can crash in CreateVistaDialog(). Don't try it.
            if (underWine)
                return false;

            try
            {
                var fallback = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = dialog.Filter,
                    Multiselect = dialog.Multiselect,
                    InitialDirectory = dialog.InitialDirectory,
                    Title = dialog.Title,
                    CheckFileExists = dialog.CheckFileExists,
                    CheckPathExists = dialog.CheckPathExists,
                    FileName = dialog.FileName
                };
                var ok = owner == null ? fallback.ShowDialog() == true : fallback.ShowDialog(owner) == true;
                if (ok)
                {
                    fileName = fallback.FileName;
                    fileNames = fallback.FileNames ?? Array.Empty<string>();
                    Log($"Dialog OK (WPF OpenFileDialog fallback){FormatContext(context)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"WPF OpenFileDialog fallback failed{FormatContext(context)}: {ex}");
            }

            return false;
        }

        public static bool TryShowDialog(Microsoft.Win32.OpenFileDialog dialog, out string fileName, out string[] fileNames, Window owner = null, string context = null)
        {
            fileName = string.Empty;
            fileNames = Array.Empty<string>();

            if (dialog == null)
                return false;

            // Under Wine-like runtimes, WPF's OpenFileDialog can crash in CreateVistaDialog(). Prefer WinForms.
            bool underWine = ShouldAvoidWinFormsDialogs();
            if (!underWine)
            {
                try
                {
                    var ok = owner == null ? dialog.ShowDialog() == true : dialog.ShowDialog(owner) == true;
                    if (ok)
                    {
                        fileName = dialog.FileName;
                        fileNames = dialog.FileNames ?? Array.Empty<string>();
                        Log($"Dialog OK (WPF OpenFileDialog){FormatContext(context)}");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"WPF OpenFileDialog failed{FormatContext(context)}: {ex}");
                }
            }

            try
            {
                using var fallback = new System.Windows.Forms.OpenFileDialog
                {
                    Filter = dialog.Filter,
                    Multiselect = dialog.Multiselect,
                    InitialDirectory = dialog.InitialDirectory,
                    Title = dialog.Title,
                    CheckFileExists = dialog.CheckFileExists,
                    CheckPathExists = dialog.CheckPathExists,
                    FileName = dialog.FileName,
                    AutoUpgradeEnabled = false
                };
                var res = owner == null ? fallback.ShowDialog() : fallback.ShowDialog(new Win32WindowWrapper(owner));
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    fileName = fallback.FileName;
                    fileNames = fallback.FileNames ?? Array.Empty<string>();
                    Log($"Dialog OK (WinForms OpenFileDialog fallback){FormatContext(context)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"WinForms OpenFileDialog fallback failed{FormatContext(context)}: {ex}");
            }

            return false;
        }

        public static bool TryShowDialog(Microsoft.Win32.SaveFileDialog dialog, out string fileName, Window owner = null, string context = null)
        {
            fileName = string.Empty;

            if (dialog == null)
                return false;

            // Under Wine-like runtimes, WPF's SaveFileDialog can also go down the Vista-style COM path.
            bool underWine = ShouldAvoidWinFormsDialogs();
            if (!underWine)
            {
                try
                {
                    var ok = owner == null ? dialog.ShowDialog() == true : dialog.ShowDialog(owner) == true;
                    if (ok)
                    {
                        fileName = dialog.FileName;
                        Log($"Dialog OK (WPF SaveFileDialog){FormatContext(context)}");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"WPF SaveFileDialog failed{FormatContext(context)}: {ex}");
                }
            }

            try
            {
                using var fallback = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = dialog.Filter,
                    InitialDirectory = dialog.InitialDirectory,
                    Title = dialog.Title,
                    FileName = dialog.FileName,
                    DefaultExt = dialog.DefaultExt,
                    AddExtension = dialog.AddExtension,
                    OverwritePrompt = dialog.OverwritePrompt,
                    AutoUpgradeEnabled = false
                };
                var res = owner == null ? fallback.ShowDialog() : fallback.ShowDialog(new Win32WindowWrapper(owner));
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    fileName = fallback.FileName;
                    Log($"Dialog OK (WinForms SaveFileDialog fallback){FormatContext(context)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"WinForms SaveFileDialog fallback failed{FormatContext(context)}: {ex}");
            }

            return false;
        }

        public static bool TryShowDialog(CommonOpenFileDialog dialog, out string path, Window owner = null, string context = null)
        {
            path = string.Empty;

            if (dialog == null)
                return false;

            try
            {
                var ownerHandle = owner == null ? IntPtr.Zero : new System.Windows.Interop.WindowInteropHelper(owner).Handle;
                var res = owner == null ? dialog.ShowDialog() : dialog.ShowDialog(ownerHandle);
                if (res == CommonFileDialogResult.Ok)
                {
                    path = dialog.FileName;
                    Log($"Dialog OK (CommonOpenFileDialog){FormatContext(context)}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"CommonOpenFileDialog failed{FormatContext(context)}: {ex}");
            }

            try
            {
                using var fallback = new System.Windows.Forms.FolderBrowserDialog
                {
                    SelectedPath = dialog.InitialDirectory ?? string.Empty,
                    Description = dialog.Title ?? string.Empty
                };
                var res = owner == null ? fallback.ShowDialog() : fallback.ShowDialog(new Win32WindowWrapper(owner));
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    path = fallback.SelectedPath;
                    Log($"Dialog OK (FolderBrowserDialog fallback){FormatContext(context)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"FolderBrowserDialog fallback failed{FormatContext(context)}: {ex}");
            }

            return false;
        }

        public static bool TryShowDialog(System.Windows.Forms.FolderBrowserDialog dialog, out string path, Window owner = null, string context = null)
        {
            path = string.Empty;

            if (dialog == null)
                return false;

            bool avoidWinForms = ShouldAvoidWinFormsDialogs();
            if (avoidWinForms)
            {
                Log($"Skipping FolderBrowserDialog primary path under Wine-like runtime{FormatContext(context)}");
            }
            else
            {
                try
                {
                    var res = owner == null ? dialog.ShowDialog() : dialog.ShowDialog(new Win32WindowWrapper(owner));
                    if (res == System.Windows.Forms.DialogResult.OK)
                    {
                        path = dialog.SelectedPath;
                        Log($"Dialog OK (FolderBrowserDialog){FormatContext(context)}");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"FolderBrowserDialog failed{FormatContext(context)}: {ex}");
                }
            }

            try
            {
                using var fallback = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    InitialDirectory = dialog.SelectedPath,
                    Title = dialog.Description
                };
                var ownerHandle = owner == null ? IntPtr.Zero : new System.Windows.Interop.WindowInteropHelper(owner).Handle;
                var res = owner == null ? fallback.ShowDialog() : fallback.ShowDialog(ownerHandle);
                if (res == CommonFileDialogResult.Ok)
                {
                    path = fallback.FileName;
                    Log($"Dialog OK (CommonOpenFileDialog fallback){FormatContext(context)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"CommonOpenFileDialog fallback failed{FormatContext(context)}: {ex}");
            }

            return false;
        }

        private static void Log(string message)
        {
            try { Logger.Log(message); } catch { }
        }

        private static string FormatContext(string context)
        {
            return string.IsNullOrWhiteSpace(context) ? string.Empty : $" [{context}]";
        }

        private static bool ShouldAvoidWinFormsDialogs()
        {
            try
            {
                return ToolRunner.UnderWine();
            }
            catch
            {
                return false;
            }
        }

        private sealed class Win32WindowWrapper : System.Windows.Forms.IWin32Window
        {
            public Win32WindowWrapper(Window owner)
            {
                Handle = owner == null ? IntPtr.Zero : new System.Windows.Interop.WindowInteropHelper(owner).Handle;
            }

            public IntPtr Handle { get; }
        }
    }
}
