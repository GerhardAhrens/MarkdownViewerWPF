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

        public MarkdownEditor()
        {
            this.InitializeComponent();
        }

        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            editorScrollViewer = FindScrollViewer(Editor);

            if (editorScrollViewer != null)
                editorScrollViewer.ScrollChanged += EditorScrollChanged;

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
            LineNumberCanvas.RenderTransform =
                new TranslateTransform(0, -e.VerticalOffset);

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
            if (editorScrollViewer == null)
                return;

            LineNumberCanvas.Children.Clear();

            double verticalOffset = editorScrollViewer.VerticalOffset;
            double viewportHeight = editorScrollViewer.ViewportHeight;

            int firstLine = Editor.GetLineIndexFromCharacterIndex(
                Editor.GetCharacterIndexFromPoint(new Point(0, verticalOffset), true));

            if (firstLine < 0)
                firstLine = 0;

            int lineCount = Editor.LineCount;

            for (int i = firstLine; i < lineCount; i++)
            {
                int charIndex = Editor.GetCharacterIndexFromLineIndex(i);

                Rect rect = Editor.GetRectFromCharacterIndex(charIndex);

                if (rect.IsEmpty)
                    continue;

                if (rect.Top > viewportHeight)
                    break;

                TextBlock tb = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontFamily = Editor.FontFamily,
                    FontSize = Editor.FontSize,
                    Foreground = Brushes.Gray
                };

                Canvas.SetTop(tb, rect.Top - editorScrollViewer.VerticalOffset);
                Canvas.SetRight(tb, 5);

                LineNumberCanvas.Children.Add(tb);
            }
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

        private void UpdateCurrentLineHighlight()
        {
            int caret = Editor.CaretIndex;

            Rect rect = Editor.GetRectFromCharacterIndex(caret);

            if (rect == Rect.Empty)
                return;

            CurrentLineHighlight.Height = rect.Height;

            CurrentLineHighlight.Margin = new Thickness(
                0,
                rect.Top - editorScrollViewer.VerticalOffset,
                0,
                0);
        }

        private void UpdateEditorVisuals()
        {
            UpdateStatus();
            UpdateCurrentLineHighlight();
            UpdateLineNumbers();
        }
    }
}
