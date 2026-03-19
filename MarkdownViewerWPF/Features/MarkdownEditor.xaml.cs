namespace System.Windows.Documents
{
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// Interaktionslogik für MarkdownEditor.xaml
    /// </summary>
    public partial class MarkdownEditor : UserControl
    {
        private ScrollViewer editorScrollViewer;
        private double lineHeight;

        public MarkdownEditor()
        {
            this.InitializeComponent();
        }

        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            editorScrollViewer = FindScrollViewer(Editor);

            if (editorScrollViewer != null)
                editorScrollViewer.ScrollChanged += EditorScrollChanged;

            lineHeight = Editor.GetRectFromCharacterIndex(0).Height;

            if (lineHeight <= 0)
                lineHeight = Editor.FontSize * 1.4;

            UpdateEditorVisuals();
        }
        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateEditorVisuals();
        }

        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateEditorVisuals();
        }

        private void EditorScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateLineNumbers();
        }

        private void UpdateStatus()
        {
            int caret = Editor.CaretIndex;

            int line = Editor.GetLineIndexFromCharacterIndex(caret);
            int column = caret - Editor.GetCharacterIndexFromLineIndex(line);

            StatusCursor.Text = $"Ln {line + 1}, Col {column + 1}";

            int totalLines = Editor.LineCount;
            StatusLines.Text = $"Lines: {totalLines}";

            int selection = Editor.SelectionLength;
            StatusSelection.Text = $"Sel: {selection}";

            int utfPos = caret;

            int bytePos = Encoding.UTF8.GetByteCount(Editor.Text.Substring(0, caret));

            StatusUtf.Text = $"UTF: {utfPos}  Bytes: {bytePos}";
        }

        private void UpdateLineNumbers()
        {
            if (editorScrollViewer == null || lineHeight <= 0)
                return;

            LineNumberCanvas.Children.Clear();

            double offset = editorScrollViewer.VerticalOffset;
            double viewport = editorScrollViewer.ViewportHeight;

            int firstLine = (int)(offset / lineHeight);
            int visibleLines = (int)(viewport / lineHeight) + 2;

            int lastLine = Math.Min(Editor.LineCount, firstLine + visibleLines);

            for (int i = firstLine; i < lastLine; i++)
            {
                TextBlock tb = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontFamily = Editor.FontFamily,
                    FontSize = Editor.FontSize,
                    Foreground = Brushes.Gray
                };

                double y = (i * lineHeight) - offset;

                Canvas.SetTop(tb, y);
                Canvas.SetRight(tb, 5);

                LineNumberCanvas.Children.Add(tb);
            }
        }

        private void UpdateCurrentLineHighlight()
        {
            if (editorScrollViewer == null)
                return;

            int line = Editor.GetLineIndexFromCharacterIndex(Editor.CaretIndex);

            double y = line * lineHeight - editorScrollViewer.VerticalOffset;

            CurrentLineHighlight.Height = lineHeight;
            CurrentLineHighlight.Margin = new Thickness(0, y, 0, 0);
        }


        private static ScrollViewer FindScrollViewer(DependencyObject d)
        {
            if (d is ScrollViewer)
                return (ScrollViewer)d;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                var result = FindScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void UpdateEditorVisuals()
        {
            UpdateCurrentLineHighlight();
            UpdateLineNumbers();
            UpdateStatus();
        }
    }
}
