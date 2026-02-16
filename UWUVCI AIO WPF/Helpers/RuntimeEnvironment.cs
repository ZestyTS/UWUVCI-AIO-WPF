using Microsoft.Win32;
using System.Globalization;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media;

namespace UWUVCI_AIO_WPF.Helpers
{
    internal static class RuntimeEnvironment
    {
        internal static void ApplyWineCompatibilityDefaults()
        {
            try
            {
                if (!ToolRunner.UnderWine())
                    return;

                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

                // Persist via WPF registry toggle (no-op under Wine, but harmless).
                Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Avalon.Graphics",
                    "DisableHWAcceleration",
                    1,
                    RegistryValueKind.DWord);
            }
            catch
            {
                // Best-effort only.
            }
        }

        internal static void ApplyInvariantCulture()
        {
            // This prevents Turkish locale issues and other culture-sensitive parsing bugs.
            var invariant = CultureInfo.InvariantCulture;

            Thread.CurrentThread.CurrentCulture = invariant;
            Thread.CurrentThread.CurrentUICulture = invariant;
            CultureInfo.DefaultThreadCurrentCulture = invariant;
            CultureInfo.DefaultThreadCurrentUICulture = invariant;
        }
    }
}
