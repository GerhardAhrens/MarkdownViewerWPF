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
    using System.Windows.Navigation;

    /// <summary>
    /// Interaktionslogik für MarkdownViewer.xaml
    /// </summary>
    public partial class MarkdownViewer : UserControl
    {
        public MarkdownViewer()
        {
            this.InitializeComponent();

            this.PART_RichText.IsReadOnly = true;
            this.PART_RichText.IsDocumentEnabled = true;

            this.PART_RichText.AddHandler(
                    Hyperlink.RequestNavigateEvent,
                    new RequestNavigateEventHandler(this.Hyperlink_RequestNavigate));
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
            this.PART_RichText.Document = doc;
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
        static MarkdownParser()
        {
            BasePath = AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string BasePath { get; set; }

        public static FlowDocument Parse(string markdown)
        {
            FlowDocument doc = new FlowDocument();

            var lines = markdown.Split(["\r\n", "\n"], StringSplitOptions.None);

            bool tabelle = false;
            bool codeBlock = false;
            bool numericListActive = false;
            bool quoteActive = false;
            Paragraph codeParagraph = null;
            List<string> tableLines = new List<string>();
            List numericList = new List { MarkerStyle = TextMarkerStyle.Decimal };
            List<string> quoteLines = new List<string>();

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
                    tableLines.Clear();
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

                if (line.StartsWith("### ", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    doc.Blocks.Add(CreateHeader(line.Substring(4), 18));
                    continue;
                }

                if (line.StartsWith(">", StringComparison.CurrentCultureIgnoreCase))
                {
                    quoteActive = true;
                    if (quoteActive == true)
                    {
                        quoteLines.Add(line.Substring(1).TrimStart());
                    }

                    continue;
                }

                if (quoteActive == true)
                {
                    doc.Blocks.Add(ParseQuote(quoteLines));
                    quoteLines.Clear();
                    quoteActive = false;
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

                /* Nummerische Aufzählung */
                if (Regex.IsMatch(line, @"^\d+\.\s+"))
                {
                    numericListActive = true;

                    if (numericListActive == true)
                    {
                        string itemText = Regex.Replace(line, @"^\d+\.\s+", "");
                        ListItem item = new ListItem(new Paragraph(ParseInline(itemText)));
                        numericList.ListItems.Add(item);
                    }

                    continue;
                }

                if (numericListActive == true)
                {
                    doc.Blocks.Add(numericList);
                    numericList = null;
                    numericListActive = false;
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

            var matches = Regex.Matches(text,
                @"`[^`]+`|\*\*\*[^*]+\*\*\*|\*\*[^*]+\*\*|\*[^*]+\*|!\[[^\]]*\]\([^\)]+\)|\[[^\]]+\]\([^\)]+\)|\\.|[^*`\[\\]+");

            foreach (Match match in matches)
            {
                string token = match.Value;

                if (token.StartsWith("***", StringComparison.CurrentCultureIgnoreCase) == true && token.EndsWith("***", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    string value = token.Substring(3, token.Length - 6);

                    span.Inlines.Add(new Bold( new Italic(new Run(value))));
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
                else if (token.StartsWith("<!--", StringComparison.CurrentCultureIgnoreCase) && token.EndsWith("-->", StringComparison.CurrentCultureIgnoreCase))
                {
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
                else if (token.StartsWith("![", StringComparison.CurrentCultureIgnoreCase))
                {
                    var m = Regex.Match(token, @"!\[(.*?)\]\((.*?)(?:\s*=\s*(\d*)x(\d*))?\)");

                    string alt = m.Groups[1].Value;
                    string url = m.Groups[2].Value;
                    string width = m.Groups[3].Value == string.Empty ? "32" : m.Groups[3].Value;
                    string height = m.Groups[4].Value == string.Empty ? "32" : m.Groups[4].Value;

                    Image image = new Image
                    {
                        Width = Convert.ToDouble(width, CultureInfo.CurrentCulture),
                        Height = Convert.ToDouble(height, CultureInfo.CurrentCulture),
                        Margin = new Thickness(4)
                    };

                    try
                    {
                        image.Source = LoadImageSource(url);
                        span.Inlines.Add(new InlineUIContainer(image));
                    }
                    catch
                    {
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
            return Regex.IsMatch(line, @"^\s*\|.*\|\s*$");
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

            // ---------- Header analysieren ----------
            var headerCells = lines[0]
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToArray();

            int columnCount = headerCells.Length;

            // ---------- Alignment bestimmen ----------
            List<TextAlignment> alignments = new List<TextAlignment>();

            if (lines.Count > 1)
            {
                var alignCells = lines[1]
                    .Split('|', StringSplitOptions.RemoveEmptyEntries);

                foreach (var cell in alignCells)
                {
                    string a = cell.Trim();

                    if (a.StartsWith(":", StringComparison.CurrentCultureIgnoreCase) && a.EndsWith(":", StringComparison.CurrentCultureIgnoreCase))
                    {
                        alignments.Add(TextAlignment.Center);
                    }
                    else if (a.EndsWith(":", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        alignments.Add(TextAlignment.Right);
                    }
                    else
                    {
                        alignments.Add(TextAlignment.Left);
                    }
                }
            }

            while (alignments.Count < columnCount)
                alignments.Add(TextAlignment.Left);

            // ---------- Tabelleninhalte vorbereiten ----------
            List<string[]> parsedRows = new List<string[]>();

            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"^\|\s*[:\-]+\s*(\|\s*[:\-]+\s*)*\|?$"))
                {
                    continue;
                }

                parsedRows.Add(
                    line.Split('|', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .ToArray()
                );
            }

            // ---------- automatische Spaltenbreite ----------
            double[] columnWidths = CalculateColumnWidths(parsedRows);

            for (int i = 0; i < columnWidths.Length; i++)
            {
                table.Columns.Add(new TableColumn
                {
                    Width = new GridLength(columnWidths[i])
                });
            }

            // ---------- Tabellenzeilen erstellen ----------
            bool header = true;

            foreach (var rowData in parsedRows)
            {
                TableRow row = new TableRow();

                for (int i = 0; i < rowData.Length; i++)
                {
                    Paragraph paragraph = new Paragraph(ParseInline(rowData[i]))
                    {
                        TextAlignment = alignments[Math.Min(i, alignments.Count - 1)]
                    };

                    TableCell cell = new TableCell(paragraph)
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(6)
                    };

                    if (header)
                    {
                        paragraph.FontWeight = FontWeights.Bold;
                        cell.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    }

                    row.Cells.Add(cell);
                }

                group.Rows.Add(row);
                header = false;
            }

            return table;
        }

        private static double MeasureTextWidth(string text)
        {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                14,
                Brushes.Black,
                VisualTreeHelper.GetDpi(new System.Windows.Controls.Control()).PixelsPerDip);

            return formattedText.Width;
        }

        private static double[] CalculateColumnWidths(List<string[]> rows)
        {
            int columnCount = rows[0].Length;
            double[] widths = new double[columnCount];

            foreach (var row in rows)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    double w = MeasureTextWidth(row[i]) + 20; // Padding

                    if (w > widths[i])
                    {
                        widths[i] = w;
                    }
                }
            }

            return widths;
        }

        private static Section ParseQuote(List<string> lines)
        {
            Section section = new Section
            {
                Margin = new Thickness(10, 4, 0, 4),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(10, 2, 2, 2)
            };

            foreach (var line in lines)
            {
                section.Blocks.Add(new Paragraph(ParseInline(line)));
            }

            return section;
        }

        private static BitmapImage LoadImageSource(string path)
        {
            BitmapImage result = null;
            if (path.StartsWith("res:", StringComparison.CurrentCultureIgnoreCase))
            {
                /* Eigenschaften: Ressource */
                /* ![Logo](res:Resources/Picture/_PreviewImage.png=64x64) */
                string resourcePath = path.Substring(4);

                var uri = new Uri($"pack://application:,,,/{resourcePath}", UriKind.RelativeOrAbsolute);

                try
                {
                    result = new BitmapImage(uri);
                }
                catch (Exception)
                {
                    return new BitmapImage();
                }

                return result;
            }

            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                return new BitmapImage(new Uri(path));
            }

            if (System.IO.File.Exists(path) == true)
            {
                return new BitmapImage(new Uri(path, UriKind.Absolute));
            }

            return new BitmapImage(new Uri(path, UriKind.Relative));
        }
    }
}
