using System;

namespace UWUVCI_AIO_WPF.Helpers
{
    public static class BuildInfo
    {
        public static string FormatMainWindowTitle(Version version, DateTime buildDate)
        {
            string release = $"v{version.Major}.{version.Minor}.{version.Build}";
            string stamp = buildDate.ToString("MMM dd, yyyy");
            return string.Concat("ZestyTS' UWUVCI ", release, "  (", stamp, ")");
        }
    }
}
