//-----------------------------------------------------------------------
// <copyright file="MainWindow.cs" company="Lifeprojects.de">
//     Class: MainWindow
//     Copyright © Lifeprojects.de 2026
// </copyright>
//
// <author>Gerhard Ahrens - Lifeprojects.de</author>
// <email>developer@lifeprojects.de</email>
// <date>05.03.2026 18:21:36</date>
//
// <summary>
// WPF Template mit Minimalfunktionen
// </summary>
//-----------------------------------------------------------------------

namespace MarkdownViewerWPF
{
    using System.ComponentModel;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        public MainWindow()
        {
            this.InitializeComponent();
            WeakEventManager<WindowBase, RoutedEventArgs>.AddHandler(this, "Loaded", this.OnLoaded);
            WeakEventManager<WindowBase, CancelEventArgs>.AddHandler(this, "Closing", this.OnWindowClosing);

            this.QuitCommand = new CommandBase(this.OnQuit);
            this.QuitParamCommand = new CommandBase(() => this.OnQuit("Argument"));
            this.StartCommand = new CommandBase(OnStart);

            this.WindowTitel = "Minimal WPF Template";
            this.DataContext = this;
        }

        public CommandBase QuitCommand { get; private set; }
        public CommandBase QuitParamCommand { get; private set; }
        public CommandBase StartCommand { get; private set; }

        public string WindowTitel
        {
            get => base.GetValue<string>();
            set => base.SetValue(value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            StatusbarMain.Statusbar.DatabaseInfo = "Keine";
            StatusbarMain.Statusbar.DatabaseInfoTooltip = "Keine Datenbank verbunden";
            StatusbarMain.Statusbar.Notification = "Bereit";

            string gruppeA = $"# Titel\n## Abschnitt\nText mit **Bold** und *Italic*\nText mit ***Bold und Italic***\n- Item 1\n- Item 2\n\nInline Code `var x = 5;`\n[GoogleWeb](https://google.com)";
            string gruppeB = $"# Titel\n| Name | Alter | Beruf |\n|------|------:|------|\n| Anna | 28 | Entwickler |\n| Max | 35 | Designer |\nNormaler Text mit **Bold** und *Italic*.\n![Logo](C:\\_Projekte\\_Git_Private\\MarkdownViewerWPF\\MarkdownViewerWPF\\Resources\\Picture\\_PreviewImage.png=64x64)";
            string gruppeC = $"- Bullet Lists1\n- Bullet Lists2\n\n1. Bullet Lists1\n2. Bullet Lists2\n";
            string gruppeD = $"> Dies ist ein Zitat\r\n> über mehrere Zeilen\r\n> mit **Markdown**\n";
            this.markdownViewer.MarkdownText = $"{gruppeA}\n\n{gruppeB}\n{gruppeC}\n{gruppeD}";
        }

        private void OnCloseApplication(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnQuit()
        {
            this.Tag = null;
            this.Close();
        }

        private void OnStart()
        {
            this.QuitParamCommand.TryExecute();
        }

        private void OnQuit(string param)
        {
            this.Tag = param;
            this.Close();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = false;

            MessageBoxResult msgYN;
            if (this.Tag != null)
            {
                msgYN = MessageBox.Show($"Wollen Sie die Anwendung beenden? ({this.Tag})", "Beenden", MessageBoxButton.YesNo, MessageBoxImage.Question);
            }
            else
            {
                msgYN = MessageBox.Show("Wollen Sie die Anwendung beenden?", "Beenden", MessageBoxButton.YesNo, MessageBoxImage.Question);
            }

            if (msgYN == MessageBoxResult.Yes)
            {
                App.ApplicationExit();
            }
            else
            {
                e.Cancel = true;
            }
        }
    }
}