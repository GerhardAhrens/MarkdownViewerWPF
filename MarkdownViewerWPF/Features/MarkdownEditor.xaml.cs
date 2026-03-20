namespace System.Windows.Documents
{
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    using Microsoft.Win32;

    /// <summary>
    /// Interaktionslogik für MarkdownEditor.xaml
    /// </summary>
    public partial class MarkdownEditor : UserControl
    {
        private const string DATEIFILTER = "Markdown (*.md)|*.md|Textdateien (*.txt)|*.txt|Alle Dateien (*.*)|*.*";
        private ScrollViewer editorScrollViewer;
        private double lineHeight;

        public MarkdownEditor()
        {
            this.InitializeComponent();

            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background, 
                new Action(() => 
                { 
                    this.Editor.Focus(); 
                }));
            

            InputBindings.Add(new KeyBinding(
            new EditorRelayCommand(o => OpenFileDialog()),
            new KeyGesture(Key.O, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                    new EditorRelayCommand(o => this.Save()),
                    new KeyGesture(Key.S, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                        new EditorRelayCommand(o => this.InsertCurrentDate(),null),
                        new KeyGesture(Key.D, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
            new EditorRelayCommand(o => this.InsertHeader("#"), null),
            new KeyGesture(Key.H, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                        new EditorRelayCommand(o => this.WrapSelection("*"),null),
                        new KeyGesture(Key.F, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                        new EditorRelayCommand(o => this.WrapSelection("**"), null),
                        new KeyGesture(Key.I, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                        new EditorRelayCommand(o => this.WrapSelection("***"), null),
                        new KeyGesture(Key.J, ModifierKeys.Control)));
        }

        public string FileName { get; private set; }
        public bool IsModified { get; private set; }

        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            this.editorScrollViewer = FindScrollViewer(Editor);

            if (this.editorScrollViewer != null)
            {
                this.editorScrollViewer.ScrollChanged += EditorScrollChanged;
            }

            this.lineHeight = Editor.GetRectFromCharacterIndex(0).Height;

            if (this.lineHeight <= 0)
            {
                this.lineHeight = Editor.FontSize * 1.4;
            }

            this.UpdateEditorVisuals();
        }
        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.IsModified = true;
            this.UpdateEditorVisuals();
        }

        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            this.UpdateEditorVisuals();
        }

        private void EditorScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.UpdateLineNumbers();
        }

        private void UpdateStatus()
        {
            int caret = Editor.CaretIndex;

            int line = Editor.GetLineIndexFromCharacterIndex(caret);
            int column = caret - Editor.GetCharacterIndexFromLineIndex(line);

            this.StatusCursor.Text = $"Ln {line + 1}, Col {column + 1}";

            int totalLines = Editor.LineCount;
            this.StatusLines.Text = $"Lines: {totalLines}";

            int selection = Editor.SelectionLength;
            this.StatusSelection.Text = $"Sel: {selection}";

            int utfPos = caret;

            int bytePos = Encoding.UTF8.GetByteCount(Editor.Text.AsSpan(0, caret));

            this.StatusUtf.Text = $"UTF: {utfPos}  Bytes: {bytePos}";

            string name = string.IsNullOrEmpty(FileName) ? "Neue Datei" : Path.GetFileName(FileName);

            if (IsModified)
            {
                name += " *";
            }

            StatusFile.Text = name;
        }

        private void UpdateLineNumbers()
        {
            if (this.editorScrollViewer == null || lineHeight <= 0)
            {
                return;
            }

            this.LineNumberCanvas.Children.Clear();

            double offset = editorScrollViewer.VerticalOffset;
            double viewport = editorScrollViewer.ViewportHeight;

            int firstLine = (int)(offset / lineHeight);
            int visibleLines = (int)(viewport / lineHeight) + 2;

            int lastLine = Math.Min(Editor.LineCount, firstLine + visibleLines);

            for (int i = firstLine; i < lastLine; i++)
            {
                TextBlock tb = new TextBlock
                {
                    Text = (i + 1).ToString(CultureInfo.CurrentCulture), FontFamily = Editor.FontFamily, FontSize = Editor.FontSize, Foreground = Brushes.Gray
                };

                double y = (i * lineHeight) - offset;

                Canvas.SetTop(tb, y);
                Canvas.SetRight(tb, 5);

                this.LineNumberCanvas.Children.Add(tb);
            }
        }

        private void UpdateCurrentLineHighlight()
        {
            if (this.editorScrollViewer == null)
            {
                return;
            }

            int line = this.Editor.GetLineIndexFromCharacterIndex(Editor.CaretIndex);

            double y = line * this.lineHeight - this.editorScrollViewer.VerticalOffset;

            this.CurrentLineHighlight.Height = this.lineHeight + 8;
            this.CurrentLineHighlight.Margin = new Thickness(0, y, 0, 0);
        }


        private static ScrollViewer FindScrollViewer(DependencyObject d)
        {
            if (d is ScrollViewer)
            {
                return (ScrollViewer)d;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                var result = FindScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void UpdateEditorVisuals()
        {
            this.UpdateCurrentLineHighlight();
            this.UpdateLineNumbers();
            this.UpdateStatus();
        }

        #region Text per Doppelklick markieren
        private void Editor_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int caret = Editor.CaretIndex;

            if (caret >= Editor.Text.Length)
                return;

            string text = Editor.Text;

            if (char.IsWhiteSpace(text[caret]))
                return;

            int start = caret;
            int end = caret;

            while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
                start--;

            while (end < text.Length && !char.IsWhiteSpace(text[end]))
                end++;

            Editor.SelectionStart = start;
            Editor.SelectionLength = end - start;

            e.Handled = true;
        }
        #endregion Text per Doppelklick markieren

        #region Bereich für Kontextmenü
        private void LoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = DATEIFILTER;

            if (dlg.ShowDialog() == true)
            {
                this.LoadFile(dlg.FileName);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter = DATEIFILTER;

            if (dlg.ShowDialog() == true)
            {
                this.SaveFile(dlg.FileName);
            }
        }

        private void InsertCurrentDate_Click(object sender, RoutedEventArgs e)
        {
            this.InsertCurrentDate();
        }

        private void InsertHeader_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                string parameter = menuItem.Tag.ToString();
                if (parameter == "#")
                {
                    this.InsertHeader(parameter);
                }
                else if (parameter == ">")
                {
                    this.InsertZitat(parameter);
                }
            }
        }

        private void WrapWithStar_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                string parameter = menuItem.Tag.ToString();
                this.WrapSelection(parameter);
            }
        }

        private void InsertOnPosition_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                string parameter = menuItem.Tag.ToString().ToLower(CultureInfo.CurrentCulture);
                if (parameter == "url")
                {
                    this.InsertOnPosition("[AlternateText](WebSeiten-Url)");
                }
                else if (parameter == "bild")
                {
                    this.InsertOnPosition("![AlternateText](Bildname.png=BreitexHöhe)");
                }
                else if (parameter == "tab")
                {
                    this.InsertOnPosition($"| Spalte1 | Spalte2 | Spalte3 |\n|------|------:|------|\n| Text | Zahl | Text |\n");
                }
            }
        }

        public void LoadFile(string path)
        {
            Editor.Text = File.ReadAllText(path);

            FileName = path;
            IsModified = false;
            this.UpdateStatus();
        }

        public void SaveFile(string path)
        {
            File.WriteAllText(path, Editor.Text);

            this.FileName = path;
            this.IsModified = false;
            this.UpdateStatus();
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(FileName))
            {
                SaveFileDialog dlg = new SaveFileDialog();

                dlg.Filter = DATEIFILTER;

                if (dlg.ShowDialog() == true)
                {
                    this.SaveFile(dlg.FileName);
                }
            }
            else
            {
                this.SaveFile(FileName);
            }
        }

        private void OpenFileDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = DATEIFILTER;

            if (dlg.ShowDialog() == true)
            {
                this.LoadFile(dlg.FileName);
            }
        }

        private void InsertOnPosition(string parameter)
        {
            string dataText = parameter;

            int caret = Editor.CaretIndex;

            if (Editor.SelectionLength > 0)
            {
                caret = Editor.SelectionStart;
                Editor.Text = Editor.Text.Remove(Editor.SelectionStart, Editor.SelectionLength);
            }

            Editor.Text = Editor.Text.Insert(caret, dataText);
            Editor.CaretIndex = caret + dataText.Length;
        }


        private void InsertCurrentDate()
        {
            string dateText = DateTime.Now.ToString("dd.MM.yyyy",CultureInfo.CurrentCulture);

            int caret = Editor.CaretIndex;

            if (Editor.SelectionLength > 0)
            {
                caret = Editor.SelectionStart;
                Editor.Text = Editor.Text.Remove(Editor.SelectionStart, Editor.SelectionLength);
            }

            Editor.Text = Editor.Text.Insert(caret, dateText);
            Editor.CaretIndex = caret + dateText.Length;
        }

        private void InsertHeader(string parameter)
        {
            string headerText = $"{parameter} Titel";
            Editor.Text = Editor.Text.Insert(0, headerText);
            Editor.CaretIndex = 0 + headerText.Length;
        }

        private void InsertZitat(string parameter)
        {
            string headerText = $"{parameter} ";
            Editor.Text = Editor.Text.Insert(0, headerText);
            Editor.CaretIndex = 0 + headerText.Length;
        }

        private void WrapSelection(string wrapper)
        {
            int start = Editor.SelectionStart;
            int length = Editor.SelectionLength;

            if (length == 0)
            {
                return;
            }

            string selectedText = Editor.SelectedText;

            string newText = wrapper + selectedText + wrapper;

            Editor.Text = Editor.Text.Remove(start, length);
            Editor.Text = Editor.Text.Insert(start, newText);

            Editor.SelectionStart = start;
            Editor.SelectionLength = newText.Length;
        }
        #endregion Bereich für Kontextmenü
    }

    public class EditorRelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Predicate<object> canExecute;

        public EditorRelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        // Weiterleitung an CommandManager verhindert CS0067 und ermöglicht automatische Requery
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}
