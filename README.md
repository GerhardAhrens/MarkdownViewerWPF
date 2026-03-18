# MarkdownViever als WPF Anwendung

![NET](https://img.shields.io/badge/NET-10.0-green.svg)
![License](https://img.shields.io/badge/License-MIT-blue.svg)
![VS2026](https://img.shields.io/badge/Visual%20Studio-2026-white.svg)
![Version](https://img.shields.io/badge/Version-1.0.2026.0-yellow.svg)]

## Features des MarkdownViever

Der MarkdownViewer ist als eigenes Control implementiert, das in WPF-Anwendungen eingebettet werden kann.

<img src="MainWindow.png" style="width:650px;"/>

Grundsätzlich werden die Unterstützung von Standard-Markdown-Syntaxelementen verarbeite.
Folgende Funktionen werden dabei unterstützt:

| Funktion | Beschreibung |
|---|---|
| # Titel | Titel, oberste Ebene |
| ## Titel | Titel, eine Ebene tiefer|
| ### Titel | Titel, untere Ebene tiefer|
| *Italic* | Italic geschrieben|
| **Fett** | Fett geschrieben|
| ***Fett und Italic*** | Fett und Italic geschrieben|
| `var x = 5;` | Inline Code|
|```var x = 5;```| Codeblock |
| [GitHub](https://github.com/GerhardAhrens?tab=repositories) | anklickbarer Web Link|


## Parser des MarkdownViever

