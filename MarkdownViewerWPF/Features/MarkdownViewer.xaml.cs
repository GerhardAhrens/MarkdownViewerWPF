namespace System.Windows.Documents
{
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.Media3D;
    using System.Windows.Navigation;

    /// <summary>
    /// Interaktionslogik für MarkdownViewer.xaml
    /// </summary>
    public partial class MarkdownViewer : UserControl
    {
        public MarkdownViewer()
        {
            this.InitializeComponent();

            PART_RichText.IsReadOnly = true;
            PART_RichText.IsDocumentEnabled = true;

            PART_RichText.AddHandler(
                    Hyperlink.RequestNavigateEvent,
                    new RequestNavigateEventHandler(Hyperlink_RequestNavigate));
        }

        public static readonly DependencyProperty MarkdownTextProperty =
                DependencyProperty.Register(
                    nameof(MarkdownText),
                    typeof(string),
                    typeof(MarkdownViewer),
                    new PropertyMetadata("", OnMarkdownChanged));

        public string MarkdownText
        {
            get => (string)GetValue(MarkdownTextProperty);
            set => SetValue(MarkdownTextProperty, value);
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;
            viewer.RenderMarkdown(e.NewValue as string);
        }

        public void LoadMarkdownFile(string file)
        {
            if (File.Exists(file))
            {
                this.MarkdownText = File.ReadAllText(file);
            }
        }

        private void RenderMarkdown(string markdown)
        {
            FlowDocument doc = MarkdownParser.Parse(markdown ?? "");
            PART_RichText.Document = doc;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });

            e.Handled = true;
        }
    }

    public static class MarkdownParser
    {
        public static string BasePath { get; set; }

        public static FlowDocument Parse(string markdown)
        {
            FlowDocument doc = new FlowDocument();

            var lines = markdown.Split(["\r\n", "\n"], StringSplitOptions.None);

            bool tabelle = false;
            bool codeBlock = false;
            Paragraph codeParagraph = null;
            List<string> tableLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("```", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    codeBlock = !codeBlock;

                    if (codeBlock == true)
                    {
                        codeParagraph = CreateCodeParagraph();
                        doc.Blocks.Add(codeParagraph);
                    }

                    continue;
                }

                if (codeBlock == true)
                {
                    codeParagraph.Inlines.Add(new Run(line + "\n"));
                    continue;
                }

                /* ---------- Tabellen erkennen ----------*/
                if (IsTableRow(line) == true)
                {
                    tabelle = true;

                    if (tabelle == true)
                    {
                        tableLines.Add(line);
                    }

                    continue;
                }

                if (tabelle == true)
                {
                    doc.Blocks.Add(ParseTable(tableLines));
                    tabelle = false;
                }

                if (line.StartsWith("# ", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    doc.Blocks.Add(CreateHeader(line.Substring(2), 28));
                    continue;
                }

                if (line.StartsWith("## ", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    doc.Blocks.Add(CreateHeader(line.Substring(3), 22));
                    continue;
                }

                if (line.StartsWith("### ", StringComparison.CurrentCultureIgnoreCase) ==  true)
                {
                    doc.Blocks.Add(CreateHeader(line.Substring(4), 18));
                    continue;
                }

                if (line.StartsWith("- ", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    var list = new List();
                    list.MarkerStyle = TextMarkerStyle.Disc;

                    var item = new ListItem(new Paragraph(ParseInline(line.Substring(2))));
                    list.ListItems.Add(item);

                    doc.Blocks.Add(list);
                    continue;
                }

                doc.Blocks.Add(new Paragraph(ParseInline(line)));
            }

            return doc;
        }

        private static Paragraph CreateHeader(string text, double size)
        {
            return new Paragraph(new Run(text))
            {
                FontSize = size,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 4)
            };
        }

        private static Paragraph CreateCodeParagraph()
        {
            return new Paragraph()
            {
                FontFamily = new FontFamily("Consolas"),
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Margin = new Thickness(5),
                Padding = new Thickness(6)
            };
        }

        private static Span ParseInline(string text)
        {
            Span span = new Span();

            var matches = Regex.Matches(
                text,
                @"`[^`]+`|\*\*\*[^*]+\*\*\*|\*\*[^*]+\*\*|\*[^*]+\*|!\[[^\]]*\]\([^\)]+\)|\[[^\]]+\]\([^\)]+\)|\\.|[^*`\[\\]+"
            );

            foreach (Match match in matches)
            {
                string token = match.Value;

                if (token.StartsWith("***", StringComparison.CurrentCultureIgnoreCase) == true && token.EndsWith("***", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    string value = token.Substring(3, token.Length - 6);

                    span.Inlines.Add(
                        new Bold(
                            new Italic(
                                new Run(value)
                            )
                        )
                    );
                }
                else if (token.StartsWith("**", StringComparison.CurrentCultureIgnoreCase) && token.EndsWith("**", StringComparison.CurrentCultureIgnoreCase))
                {
                    string value = token.Substring(2, token.Length - 4);

                    span.Inlines.Add(new Bold(new Run(value)));
                }
                else if (token.StartsWith("*", StringComparison.CurrentCultureIgnoreCase) && token.EndsWith("*", StringComparison.CurrentCultureIgnoreCase))
                {
                    string value = token.Substring(1, token.Length - 2);

                    span.Inlines.Add(new Italic(new Run(value)));
                }
                else if (token.StartsWith("`", StringComparison.CurrentCultureIgnoreCase) && token.EndsWith("`", StringComparison.CurrentCultureIgnoreCase))
                {
                    string value = token.Substring(1, token.Length - 2);

                    span.Inlines.Add(new Run(value)
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = Brushes.LightGray
                    });
                }
                else if (token.StartsWith("![", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    var m = Regex.Match(token, @"!\[(.*?)\]\((.*?)(?:\s*=\s*(\d*)x(\d*))?\)");

                    string alt = m.Groups[1].Value;
                    string url = m.Groups[2].Value;
                    string width = m.Groups[3].Value == string.Empty ? "32" : m.Groups[3].Value;
                    string height = m.Groups[4].Value == string.Empty ? "32" : m.Groups[4].Value;

                    try
                    {
                        Image image = new Image
                        {
                            Source = new BitmapImage(new Uri(url, UriKind.RelativeOrAbsolute)),
                            Width = Convert.ToDouble(width, CultureInfo.CurrentCulture),
                            Height = Convert.ToDouble(height, CultureInfo.CurrentCulture),
                            Margin = new Thickness(4)
                        };

                        span.Inlines.Add(new InlineUIContainer(image));
                    }
                    catch
                    {
                        // Fallback wenn Bild nicht geladen werden kann
                        span.Inlines.Add(new Run($"[Image: {alt}]"));
                    }
                }
                else if (token.StartsWith("[", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    var m = Regex.Match(token, @"\[(.*?)\]\((.*?)\)");

                    string textValue = m.Groups[1].Value;
                    string url = m.Groups[2].Value;

                    Hyperlink link = new Hyperlink(new Run(textValue))
                    {
                        NavigateUri = new Uri(url),
                        Cursor = Cursors.Hand,
                        Foreground = Brushes.Blue,
                        TextDecorations = TextDecorations.Underline
                    };

                    span.Inlines.Add(link);
                }
                else if (token.StartsWith("\\", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    span.Inlines.Add(new Run(token.Substring(1)));
                }
                else
                {
                    span.Inlines.Add(new Run(token));
                }
            }

            return span;
        }

        /// <summary>
        /// Tabellen erkennen
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static bool IsTableRow(string line)
        {
            return line.Contains('|', StringComparison.CurrentCultureIgnoreCase);
        }

        private static Table ParseTable(List<string> lines)
        {
            Table table = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            TableRowGroup group = new TableRowGroup();
            table.RowGroups.Add(group);

            var headerCells = lines[0].Split('|', StringSplitOptions.RemoveEmptyEntries);
            int columnCount = headerCells.Length;

            for (int i = 0; i < columnCount; i++)
            {
                table.Columns.Add(new TableColumn());
            }

            // -------- Alignment bestimmen --------
            List<TextAlignment> alignments = new List<TextAlignment>();

            if (lines.Count > 1 && Regex.IsMatch(lines[1], @"^\s*\|?[:\- ]+\|"))
            {
                var alignCells = lines[1].Split('|', StringSplitOptions.RemoveEmptyEntries);

                foreach (var cell in alignCells)
                {
                    string a = cell.Trim();

                    if (a.StartsWith(":", StringComparison.CurrentCultureIgnoreCase) && a.EndsWith(":", StringComparison.CurrentCultureIgnoreCase))
                    {
                        alignments.Add(TextAlignment.Center);
                    }
                    else if (a.EndsWith(":", StringComparison.CurrentCultureIgnoreCase))
                    {
                        alignments.Add(TextAlignment.Right);
                    }
                    else
                    {
                        alignments.Add(TextAlignment.Left);
                    }
                }
            }
            else
            {
                for (int i = 0; i < columnCount; i++)
                {
                    alignments.Add(TextAlignment.Left);
                }
            }

            bool header = true;

            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"^\|\s*[:\-]+\s*(\|\s*[:\-]+\s*)*\|?$"))
                {
                    continue;
                }

                var cells = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

                TableRow row = new TableRow();

                for (int i = 0; i < cells.Length; i++)
                {
                    Paragraph paragraph = new Paragraph(ParseInline(cells[i].Trim()))
                    {
                        TextAlignment = alignments[Math.Min(i, alignments.Count - 1)]
                    };

                    TableCell tableCell = new TableCell(paragraph)
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(5)
                    };

                    if (header)
                    {
                        paragraph.FontWeight = FontWeights.Bold;
                        tableCell.Background = Brushes.LightGray;
                    }

                    row.Cells.Add(tableCell);
                }

                group.Rows.Add(row);
                header = false;
            }

            return table;
        }
    }
}
