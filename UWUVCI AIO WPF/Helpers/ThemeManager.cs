using System;
using System.Linq;
using System.Windows;

namespace UWUVCI_AIO_WPF.Helpers
{
    public static class ThemeManager
    {
        private const string DarkThemeSource = "Themes/Theme.Dark.xaml";
        private const string LightThemeSource = "Themes/Theme.Light.xaml";

        public static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
        }

        public static void ApplyTheme(string theme)
        {
            var app = Application.Current;
            if (app == null)
                return;

            string normalized = NormalizeTheme(theme);
            string source = normalized == "Light" ? LightThemeSource : DarkThemeSource;

            try
            {
                var dictionaries = app.Resources.MergedDictionaries;
                var existing = dictionaries.FirstOrDefault(d =>
                    d.Source != null &&
                    d.Source.OriginalString.IndexOf("Theme.", StringComparison.OrdinalIgnoreCase) >= 0);

                var replacement = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };

                if (existing != null)
                {
                    int index = dictionaries.IndexOf(existing);
                    dictionaries[index] = replacement;
                }
                else
                {
                    dictionaries.Insert(0, replacement);
                }
            }
            catch
            {
                // Theme changes are non-critical; ignore if unavailable.
            }
        }
    }
}
