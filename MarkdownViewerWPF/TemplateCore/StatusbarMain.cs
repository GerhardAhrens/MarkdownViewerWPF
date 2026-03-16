//-----------------------------------------------------------------------
// <copyright file="StatusbarMain.cs" company="Lifeprojects.de">
//     Class: StatusbarMain
//     Copyright © Lifeprojects.de 2026
// </copyright>
//
// <author>Gerhard Ahrens - Lifeprojects.de</author>
// <email>developer@lifeprojects.de</email>
// <date>12.03.2026</date>
//
// <summary>
// StatusbarMain Klasse für die Statusleiste der Anwendung. Sie enthält Informationen über den aktuellen Benutzer, das aktuelle Datum,
// die aktuelle Datenbank und Benachrichtigungen. Die Klasse implementiert das INotifyPropertyChanged-Interface, um Änderungen an den
// Eigenschaften zu melden und die Benutzeroberfläche entsprechend zu aktualisieren.
// </summary>
//-----------------------------------------------------------------------

namespace System.Windows
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public static class StatusbarMain
    {
        private static StatusbarModel _StatusbarModel = new StatusbarModel();
        public static StatusbarModel Statusbar
        {
            get { return _StatusbarModel; }
            set { _StatusbarModel = value; }
        }
    }

    public class StatusbarModel : INotifyPropertyChanged
    {
        public StatusbarModel()
        {
            this.CurrentUser = $"{Environment.UserDomainName}\\{Environment.UserName}";
            this.CurrentDate = $"{DateTime.Now.ToShortDateString()}";
        }

        private string currentUser = string.Empty;
        public string CurrentUser
        {
            get { return currentUser; }
            set
            {
                currentUser = value;
                this.OnPropertyChanged();
            }
        }

        private string currentDate = string.Empty;
        public string CurrentDate
        {
            get { return currentDate; }
            set
            {
                currentDate = value;
                this.OnPropertyChanged();
            }
        }

        private string databaseInfo = string.Empty;
        public string DatabaseInfo
        {
            get { return this.databaseInfo; }
            set
            {
                this.databaseInfo = value;
                this.OnPropertyChanged();
            }
        }

        private string databaseInfoTooltip = string.Empty;
        public string DatabaseInfoTooltip
        {
            get { return this.databaseInfoTooltip; }
            set
            {
                this.databaseInfoTooltip = value;
                this.OnPropertyChanged();
            }
        }

        private string notification = string.Empty;
        public string Notification
        {
            get { return this.notification; }
            set
            {
                this.notification = value;
                this.OnPropertyChanged();
            }
        }

        /*
        public void SetNotification(string notification = null)
        {
            if (string.IsNullOrEmpty(notification) == true)
            {
                this.Notification = string.Empty;
            }
            else
            {
                this.Notification = notification;
            }
        }

        public void SetDatabaseInfo(string notification = null)
        {
            if (string.IsNullOrEmpty(notification) == true)
            {
                this.DatabaseInfo = string.Empty;
            }
            else
            {
                this.DatabaseInfo = notification;
            }
        }
        */

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
