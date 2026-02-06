using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UWUVCI_AIO_WPF.Helpers;

namespace UWUVCI_AIO_WPF.UI.Windows
{
    public partial class HelpWindow : Window
    {
        private readonly bool _plainMode;
        private static readonly string RemoteReadMeUrl =
            "https://raw.githubusercontent.com/ZestyTS/UWUVCI-AIO-WPF/refs/heads/master/UWUVCI%20AIO%20WPF/uwuvci_installer_creator/app/Readme.txt";

        private static readonly string RemotePatchNotesUrl =
            "https://raw.githubusercontent.com/ZestyTS/UWUVCI-AIO-WPF/refs/heads/master/UWUVCI%20AIO%20WPF/uwuvci_installer_creator/app/PatchNotes.txt";

        private static readonly string CacheDir;
        private static readonly string LocalReadMePath;
        private static readonly string LocalPatchNotesPath;

        static HelpWindow()
        {
            try
            {
                // Use a writable cache folder instead of the app directory (safer on macOS)
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                CacheDir = Path.Combine(appData, "UWUVCI-V3", "Cache");
                Directory.CreateDirectory(CacheDir);
            }
            catch
            {
                // Fallback: use current directory if anything goes wrong
                CacheDir = AppDomain.CurrentDomain.BaseDirectory;
            }

            LocalReadMePath = Path.Combine(CacheDir, "ReadMe.txt");
            LocalPatchNotesPath = Path.Combine(CacheDir, "PatchNotes.txt");
        }

        private DispatcherTimer _searchTimer;
        private readonly string _mode; // "readme" or "patchnotes"

        private SolidColorBrush GetSolidBrush(string key, Color fallback)
        {
            if (TryFindResource(key) is SolidColorBrush brush)
                return brush;

            return new SolidColorBrush(fallback);
        }

        public HelpWindow(string mode = "readme")
        {
            InitializeComponent();

            try { _plainMode = MacLinuxHelper.EnvDetect.Get()?.UnderWineLike == true; } catch { _plainMode = false; }

            _mode = mode.ToLowerInvariant();
            WindowTitleText.Text = _mode == "patchnotes"
                ? "ZestyTS' UWUVCI Patch Notes Viewer"
                : "ZestyTS' UWUVCI ReadMe Viewer";

            Loaded += HelpWindow_Loaded;
            PreviewKeyDown += HelpWindow_PreviewKeyDown;

            // dynamic wrap adjustment
            if (!_plainMode)
            {
                SizeChanged += (s, e) =>
                {
                    var scrollBarWidth = SystemParameters.VerticalScrollBarWidth;
                    ReadMeViewer.Document.PageWidth = e.NewSize.Width - scrollBarWidth - 140;
                };
            }

            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                HighlightSearch(SearchBox.Text);
            };
        }

        private async void HelpWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Toggle viewers based on platform
                if (_plainMode)
                {
                    if (DocScroll != null) DocScroll.Visibility = Visibility.Collapsed;
                    if (PlainScroll != null) PlainScroll.Visibility = Visibility.Visible;
                }
                else
                {
                    if (DocScroll != null) DocScroll.Visibility = Visibility.Visible;
                    if (PlainScroll != null) PlainScroll.Visibility = Visibility.Collapsed;
                }
            }
            catch { }

            if (_mode == "patchnotes")
            {
                Title = "ZestyTS' UWUVCI Patch Notes Viewer";
                DisplayText("Loading Patch Notes...");
                await LoadTextFileAsync(LocalPatchNotesPath, RemotePatchNotesUrl);
                Background = (Brush)FindResource("AppBackgroundBrush");
            }
            else
            {
                Title = "ZestyTS' UWUVCI ReadMe Viewer";
                DisplayText("Loading ReadMe...");
                await LoadTextFileAsync(LocalReadMePath, RemoteReadMeUrl);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == "patchnotes")
                DisplayText("Refreshing latest Patch Notes...");
            else
                DisplayText("Refreshing latest ReadMe...");

            await LoadTextFileAsync(
                _mode == "patchnotes" ? LocalPatchNotesPath : LocalReadMePath,
                _mode == "patchnotes" ? RemotePatchNotesUrl : RemoteReadMeUrl,
                forceOnline: true
            );
        }

        private async Task LoadTextFileAsync(string localPath, string remoteUrl, bool forceOnline = false)
        {
            try
            {
                string content = null;

                if (forceOnline || !File.Exists(localPath))
                {
                    try
                    {
                        content = await DownloadTextAsync(remoteUrl);
                        File.WriteAllText(localPath, content);
                    }
                    catch
                    {
                        // Ignore and fallback to local
                    }
                }

                if (string.IsNullOrWhiteSpace(content) && File.Exists(localPath))
                    content = File.ReadAllText(localPath);

                if (string.IsNullOrWhiteSpace(content))
                    DisplayText("Unable to load file — no internet or local copy found.");
                else
                    DisplayText(content, parseLinks: true);
            }
            catch (Exception ex)
            {
                try { Logger.Log("HelpWindow.LoadTextFileAsync error: " + ex.ToString()); } catch { }
                DisplayText($"Error loading file:\n{ex.Message}");
            }
        }

        private async Task<string> DownloadTextAsync(string url)
        {
            using var client = new WebClient();
            client.Encoding = System.Text.Encoding.UTF8;
            return await client.DownloadStringTaskAsync(url);
        }

        // -------- Display Logic --------
        private void DisplayText(string text, bool parseLinks = false)
        {
            if (_plainMode)
            {
                if (PlainViewer != null)
                {
                    PlainViewer.Text = text ?? string.Empty;
                }
                // In plain mode we skip rich parsing/highlighting for stability under Wine.
                ResultCountText.Text = "";
                return;
            }

            ReadMeViewer.Document.Blocks.Clear();
            ReadMeViewer.Document.LineHeight = 22;

            var urlRegex = new Regex(@"https?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var boldRegex = new Regex(@"\*\*(.*?)\*\*");
            var italicRegex = new Regex(@"__(.*?)__");
            var quoteRegex = new Regex("“([^”]+)”");

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool firstVersionShown = false;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // ==========================================
                // Header Line (======)
                // ==========================================
                if (Regex.IsMatch(line, @"^={6,}$"))
                {
                    var hr = new Paragraph(new Run(" "))
                    {
                        Margin = new Thickness(0, 10, 0, 10),
                        BorderBrush = GetSolidBrush("AppAccentBrush", Color.FromRgb(38, 182, 197)),
                        BorderThickness = new Thickness(0, 0, 0, 3)
                    };
                    ReadMeViewer.Document.Blocks.Add(hr);
                    continue;
                }

                // ==========================================
                // Divider Line (------)
                // ==========================================
                if (Regex.IsMatch(line, @"^-{6,}$"))
                {
                    var hr = new Paragraph(new Run(" "))
                    {
                        Margin = new Thickness(0, 6, 0, 6),
                        BorderBrush = GetSolidBrush("AppBorderBrush", Color.FromRgb(42, 50, 59)),
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    ReadMeViewer.Document.Blocks.Add(hr);
                    continue;
                }

                // ==========================================
                // Version Header
                // ==========================================
                if (line.StartsWith("Version ", StringComparison.OrdinalIgnoreCase))
                {
                    bool isFirstVersion = !firstVersionShown;
                    firstVersionShown = true;

                    DateTime? releaseDate = null;
                    var match = Regex.Match(line, @"\b(\w+)\s+(\d{1,2}),\s*(\d{4})\b");
                    if (match.Success && DateTime.TryParse($"{match.Groups[1].Value} {match.Groups[2].Value}, {match.Groups[3].Value}", out var parsed))
                        releaseDate = parsed;

                    var run = new Run(line)
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 18,
                        Foreground = GetSolidBrush("AppAccentBrush", Color.FromRgb(38, 182, 197))
                    };

                    var p = new Paragraph(run)
                    {
                        Margin = new Thickness(0, 12, 0, 3)
                    };

                    if (isFirstVersion && (!releaseDate.HasValue || (DateTime.Now - releaseDate.Value).TotalDays <= 7))
                    {
                        var tag = new Run("  ⭐ NEW")
                        {
                            Foreground = GetSolidBrush("AppAccentAltBrush", Color.FromRgb(123, 97, 255)),
                            FontWeight = FontWeights.Bold
                        };
                        p.Inlines.Add(tag);
                    }

                    ReadMeViewer.Document.Blocks.Add(p);
                    continue;
                }

                // ==========================================
                // Major Titles (uppercase text)
                // ==========================================
                if (Regex.IsMatch(line, @"^[A-Z\s]{4,}$") && line.Length < 60)
                {
                    var title = new Run(line)
                    {
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = GetSolidBrush("AppAccentBrush", Color.FromRgb(38, 182, 197))
                    };
                    var p = new Paragraph(title) { Margin = new Thickness(0, 10, 0, 4) };
                    ReadMeViewer.Document.Blocks.Add(p);
                    continue;
                }

                // ==========================================
                // FAQ: Question (Q#)
                // ==========================================
                if (Regex.IsMatch(line, @"^Q\d+\)"))
                {
                    var match = Regex.Match(line, @"^(Q\d+\))\s*(.*)");
                    var p = new Paragraph { Margin = new Thickness(0, 8, 0, 2) };
                    p.Inlines.Add(new Run(match.Groups[1].Value + " ")
                    {
                        Foreground = GetSolidBrush("AppAccentBrush", Color.FromRgb(38, 182, 197)),
                        FontWeight = FontWeights.Bold
                    });
                    p.Inlines.Add(new Run(match.Groups[2].Value)
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = GetSolidBrush("AppTextBrush", Color.FromRgb(233, 238, 245))
                    });
                    ReadMeViewer.Document.Blocks.Add(p);
                    continue;
                }

                // ==========================================
                // FAQ: Answer (A))
                // ==========================================
                if (Regex.IsMatch(line, @"^A\)"))
                {
                    var p = new Paragraph(new Run(line))
                    {
                        Margin = new Thickness(20, 0, 0, 6)
                    };
                    ReadMeViewer.Document.Blocks.Add(p);
                    continue;
                }

                // ==========================================
                // Bullets (- item and sub-bullets)
                // ==========================================
                if (Regex.IsMatch(line, @"^\s*-\s"))
                {
                    int indentLevel = 0;
                    while (indentLevel < line.Length && line[indentLevel] == ' ')
                        indentLevel++;

                    string bulletText = line.TrimStart('-', ' ').Trim();
                    bool isSubBullet = indentLevel >= 2; // detect "  - " pattern

                    Run bulletRun;
                    Paragraph p;

                    if (!isSubBullet)
                    {
                        // Top-level bullet
                        bulletRun = new Run("• " + bulletText)
                        {
                            FontSize = 15,
                            Foreground = GetSolidBrush("AppTextBrush", Color.FromRgb(233, 238, 245))
                        };
                        p = new Paragraph(bulletRun)
                        {
                            Margin = new Thickness(20, 4, 0, 2),
                            TextIndent = -10,
                            LineHeight = 24
                        };
                    }
                    else
                    {
                        // Sub-bullet
                        bulletRun = new Run("◦ " + bulletText)
                        {
                            FontSize = 14,
                            Foreground = GetSolidBrush("AppMutedTextBrush", Color.FromRgb(154, 167, 184))
                        };
                        p = new Paragraph(bulletRun)
                        {
                            Margin = new Thickness(30, 0, 0, 0),
                            TextIndent = 30,
                            LineHeight = 16
                        };
                    }

                    ReadMeViewer.Document.Blocks.Add(p);
                    continue;
                }



                // ==========================================
                // Hyperlinks
                // ==========================================
                if (urlRegex.IsMatch(line))
                {
                    var p = new Paragraph { Margin = new Thickness(0) };
                    int lastIndex = 0;
                    foreach (Match m in urlRegex.Matches(line))
                    {
                        if (m.Index > lastIndex)
                            p.Inlines.Add(new Run(line.Substring(lastIndex, m.Index - lastIndex)));

                        var link = new Hyperlink(new Run(m.Value))
                        {
                            NavigateUri = new Uri(m.Value),
                            Foreground = GetSolidBrush("AppAccentSoftBrush", Color.FromRgb(61, 125, 224))
                        };
                        link.RequestNavigate += (s, e) =>
                        {
                            if (!ToolRunner.OpenOnHost(e.Uri.ToString()))
                            {
                                try { Logger.Log("HelpWindow link open failed: " + e.Uri.ToString()); } catch { }
                                try { Clipboard.SetText(e.Uri.ToString()); } catch { }
                                UWUVCI_MessageBox.Show("Unable to open link", "The link was copied to the clipboard.", UWUVCI_MessageBoxType.Ok, UWUVCI_MessageBoxIcon.Info, this, true);
                            }
                        };

                        p.Inlines.Add(link);
                        lastIndex = m.Index + m.Length;
                    }

                    if (lastIndex < line.Length)
                        p.Inlines.Add(new Run(line.Substring(lastIndex)));

                    ReadMeViewer.Document.Blocks.Add(p);
                    continue;
                }

                // ==========================================
                // Inline Formatting (**bold**, __italic__)
                // ==========================================
                Paragraph inlineP = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
                string remaining = line;
                int pos = 0;

                while (pos < remaining.Length)
                {
                    Match boldMatch = boldRegex.Match(remaining, pos);
                    Match italicMatch = italicRegex.Match(remaining, pos);
                    Match quoteMatch = quoteRegex.Match(remaining, pos);

                    Match nextMatch = null;
                    if (boldMatch.Success) nextMatch = boldMatch;
                    if (italicMatch.Success && (nextMatch == null || italicMatch.Index < nextMatch.Index)) nextMatch = italicMatch;
                    if (quoteMatch.Success && (nextMatch == null || quoteMatch.Index < nextMatch.Index)) nextMatch = quoteMatch;

                    if (nextMatch == null)
                    {
                        inlineP.Inlines.Add(new Run(remaining.Substring(pos)));
                        break;
                    }

                    if (nextMatch.Index > pos)
                        inlineP.Inlines.Add(new Run(remaining.Substring(pos, nextMatch.Index - pos)));

                    if (nextMatch == boldMatch)
                    {
                        inlineP.Inlines.Add(new Run(boldMatch.Groups[1].Value)
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = GetSolidBrush("AppAccentBrush", Color.FromRgb(94, 66, 214))
                        });
                    }
                    else if (nextMatch == italicMatch)
                    {
                        inlineP.Inlines.Add(new Run(italicMatch.Groups[1].Value)
                        {
                            FontStyle = FontStyles.Italic,
                            Foreground = GetSolidBrush("AppMutedTextBrush", Color.FromRgb(154, 167, 184))
                        });
                    }
                    else
                    {
                        inlineP.Inlines.Add(new Run(quoteMatch.Value)
                        {
                            FontWeight = FontWeights.SemiBold,
                            Foreground = GetSolidBrush("AppAccentSoftBrush", Color.FromRgb(44, 176, 197))
                        });
                    }

                    pos = nextMatch.Index + nextMatch.Length;
                }

                if (inlineP.Inlines.Count > 0)
                    ReadMeViewer.Document.Blocks.Add(inlineP);
            }
        }

        // -------- Search Logic --------
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void HighlightSearch(string query)
        {
            if (_plainMode)
            {
                // Count occurrences only
                if (string.IsNullOrWhiteSpace(query) || PlainViewer == null)
                {
                    ResultCountText.Text = "";
                    return;
                }
                try
                {
                    var text = PlainViewer.Text ?? string.Empty;
                    int count = 0, idx = 0;
                    var comp = CaseSensitiveBox.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    while ((idx = text.IndexOf(query, idx, comp)) >= 0) { count++; idx += query.Length; }
                    ResultCountText.Text = $"{count} match{(count == 1 ? "" : "es")} found";
                }
                catch { ResultCountText.Text = ""; }
                return;
            }

            var doc = ReadMeViewer.Document;
            TextRange full = new TextRange(doc.ContentStart, doc.ContentEnd);
            full.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);

            if (string.IsNullOrWhiteSpace(query))
            {
                ResultCountText.Text = "";
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                int count = 0;
                StringComparison comp = CaseSensitiveBox.IsChecked == true
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                var highlightBase = GetSolidBrush("AppAccentSoftBrush", Color.FromRgb(61, 125, 224));
                var highlight = new SolidColorBrush(Color.FromArgb(90, highlightBase.Color.R, highlightBase.Color.G, highlightBase.Color.B));
                highlight.Freeze();

                TextPointer pos = doc.ContentStart;
                while (pos != null && pos.CompareTo(doc.ContentEnd) < 0)
                {
                    string text = pos.GetTextInRun(LogicalDirection.Forward);
                    int index = text.IndexOf(query, comp);
                    if (index >= 0)
                    {
                        TextPointer startPos = pos.GetPositionAtOffset(index);
                        TextPointer endPos = pos.GetPositionAtOffset(index + query.Length);
                        if (startPos != null && endPos != null)
                        {
                            new TextRange(startPos, endPos)
                                .ApplyPropertyValue(TextElement.BackgroundProperty, highlight);
                            count++;
                        }
                        pos = pos.GetPositionAtOffset(index + query.Length);
                    }
                    else
                        pos = pos.GetNextContextPosition(LogicalDirection.Forward);
                }

                ResultCountText.Text = $"{count} match{(count == 1 ? "" : "es")} found";
            }), DispatcherPriority.Background);
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_plainMode && PlainViewer != null)
                {
                    Clipboard.SetText(PlainViewer.Text ?? string.Empty);
                }
                else
                {
                    string allText = new TextRange(ReadMeViewer.Document.ContentStart, ReadMeViewer.Document.ContentEnd).Text;
                    Clipboard.SetText(allText);
                }
            }
            catch (Exception ex)
            {
                try { Logger.Log("HelpWindow.Copy_Click error: " + ex.ToString()); } catch { }
                UWUVCI_MessageBox.Show("Copy Failed", ex.Message, UWUVCI_MessageBoxType.Ok, UWUVCI_MessageBoxIcon.Error, this, true);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void HelpWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                HighlightSearch(SearchBox.Text);
        }
    }
}
