namespace System.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Input;
    using System.Windows.Threading;

    /// <summary>
    /// Stellt eine <see cref="ICommand"/>-Implementierung bereit, die die Methoden „Execute“ und „CanExecute“
    /// an die angegebenen Delegaten weiterleitet.
    /// </summary>
    /// <remarks>
    /// https://unclassified.software/de/source/delegatecommand
    /// this.QuitCommand = new BaseCommand(this.OnQuit);
    /// this.QuitParamCommand = new BaseCommand(() => this.OnQuit("Argument"));
    /// this.QuitParamCommand = BaseCommand.Disabled;
    /// this.QuitParamCommand.IsEnabled = true;
    /// 
    /// Direktes Aufrufen der Methoden über einen Command:
    /// this.QuitParamCommand.TryExecute();
    /// this.QuitParamCommand.RaiseCanExecuteChanged()
    /// </remarks>
    public class CommandBase : ICommand
    {
        /// <summary>
        /// Eine <see cref="CommandBase"/>-Instanz, die nichts tut und niemals ausgeführt werden kann.
        /// </summary>
        public static readonly CommandBase Disabled = new CommandBase(() => { }) { IsEnabled = false };

        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;
        private List<WeakReference> weakHandlers;
        private bool? isEnabled;
        private bool raiseCanExecuteChangedPending;

        #region Constructors

        /// <summary>
        /// Initialisiert eine neue Instanz der Klasse <see cref="CommandBase"/>.
        /// </summary>
        /// <param name="execute">Delegieren Sie die Ausführung, wenn „Execute“ für den Befehl aufgerufen wird.</param>
        /// <exception cref="ArgumentNullException">Das Ausführungsargument darf nicht null sein.</exception>
        public CommandBase(Action execute)
            : this(execute != null ? p => execute() : (Action<object>)null, (Func<object, bool>)null)
        {
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der Klasse <see cref="CommandBase"/>.
        /// </summary>
        /// <param name="execute">Delegieren Sie die Ausführung, wenn „Execute“ für den Befehl aufgerufen wird.</param>
        /// <param name="canExecute">Delegieren Sie die Ausführung, wenn CanExecute für den Befehl aufgerufen wird.</param>
        /// <exception cref="ArgumentNullException">Das Ausführungsargument darf nicht null sein.</exception>
        public CommandBase(Action execute, Func<bool> canExecute)
            : this(execute != null ? p => execute() : (Action<object>)null, canExecute != null ? p => canExecute() : (Func<object, bool>)null)
        {
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der Klasse <see cref="CommandBase"/>.
        /// </summary>
        /// <param name="execute">Delegieren Sie die Ausführung, wenn „Execute“ für den Befehl aufgerufen wird.</param>
        /// <param name="canExecute">Delegieren Sie die Ausführung, wenn CanExecute für den Befehl aufgerufen wird.</param>
        /// <exception cref="ArgumentNullException">Das Ausführungsargument darf nicht null sein.</exception>
        public CommandBase(Action execute, Func<object, bool> canExecute)
            : this(execute != null ? p => execute() : (Action<object>)null, canExecute)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBase"/> class.
        /// </summary>
        /// <param name="execute">Delegieren Sie die Ausführung, wenn „Execute“ für den Befehl aufgerufen wird.</param>
        /// <exception cref="ArgumentNullException">Das Ausführungsargument darf nicht null sein.</exception>
        public CommandBase(Action<object> execute)
            : this(execute, (Func<object, bool>)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBase"/> class.
        /// </summary>
        /// <param name="execute">Delegieren Sie die Ausführung, wenn „Execute“ für den Befehl aufgerufen wird.</param>
        /// <param name="canExecute">Delegieren Sie die Ausführung, wenn CanExecute für den Befehl aufgerufen wird.</param>
        /// <exception cref="ArgumentNullException">Das Ausführungsargument darf nicht null sein.</exception>
        public CommandBase(Action<object> execute, Func<bool> canExecute)
            : this(execute, canExecute != null ? p => canExecute() : (Func<object, bool>)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBase"/> class.
        /// </summary>
        /// <param name="execute">Delegieren Sie die Ausführung, wenn „Execute“ für den Befehl aufgerufen wird.</param>
        /// <param name="canExecute">Delegieren Sie die Ausführung, wenn CanExecute für den Befehl aufgerufen wird.</param>
        /// <exception cref="ArgumentNullException">Das Ausführungsargument darf nicht null sein.</exception>
        public CommandBase(Action<object> execute, Func<object, bool> canExecute)
        {
            ArgumentNullException.ThrowIfNull(execute);

            this.execute = execute;
            this.canExecute = canExecute;
        }

        #endregion Constructors

        #region CanExecuteChanged event

        /// <summary>
        /// Tritt auf, wenn Änderungen auftreten, die sich darauf auswirken, ob der Befehl ausgeführt werden soll oder nicht.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (this.weakHandlers == null)
                {
                    this.weakHandlers = new List<WeakReference>(new[] { new WeakReference(value) });
                }
                else
                {
                    this.weakHandlers.Add(new WeakReference(value));
                }
            }
            remove
            {
                if (this.weakHandlers == null) return;

                for (int i = this.weakHandlers.Count - 1; i >= 0; i--)
                {
                    WeakReference weakReference = this.weakHandlers[i];
                    EventHandler handler = weakReference.Target as EventHandler;
                    if (handler == null || handler == value)
                    {
                        this.weakHandlers.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Löst das Ereignis <see cref="CanExecuteChanged"/> aus.
        /// </summary>
        [DebuggerStepThrough]
        public void RaiseCanExecuteChanged() => OnCanExecuteChanged();

        /// <summary>
        /// Löst das Ereignis <see cref="CanExecuteChanged"/> aus, nachdem alle anderen Verarbeitungsvorgänge abgeschlossen sind. 
        /// Mehrere Aufrufe dieser Funktion vor dem Start der asynchronen Aktion werden ignoriert.
        /// </summary>
        [DebuggerStepThrough]
        public void RaiseCanExecuteChangedAsync()
        {
            if (this.raiseCanExecuteChangedPending == false)
            {
                // Führen Sie keine Aktionen aus, wenn Sie sich nicht im UI-Thread befinden. Das Dispatcher-Ereignis wird dort niemals ausgelöst,
                // und wahrscheinlich interessiert sich ohnehin niemand für geänderte Eigenschaften
                // in diesem Thread.
                if (Dispatcher.CurrentDispatcher == Application.Current.Dispatcher)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke((Action)OnCanExecuteChanged, DispatcherPriority.Loaded);
                    this.raiseCanExecuteChangedPending = true;
                }
            }
        }

        /// <summary>
        /// Löst das Ereignis <see cref="CanExecuteChanged"/> aus.
        /// </summary>
        [DebuggerStepThrough]
        protected virtual void OnCanExecuteChanged()
        {
            this.raiseCanExecuteChangedPending = false;
            this.PurgeWeakHandlers();
            if (this.weakHandlers == null) return;

            WeakReference[] handlers = this.weakHandlers.ToArray();
            foreach (WeakReference reference in handlers)
            {
                EventHandler handler = reference.Target as EventHandler;
                handler?.Invoke(this, EventArgs.Empty);
            }
        }

        [DebuggerStepThrough]
        private void PurgeWeakHandlers()
        {
            if (this.weakHandlers == null) return;

            for (int i = this.weakHandlers.Count - 1; i >= 0; i--)
            {
                if (this.weakHandlers[i].IsAlive == false)
                {
                    this.weakHandlers.RemoveAt(i);
                }
            }

            if (this.weakHandlers.Count == 0)
            {
                this.weakHandlers = null;
            }
        }

        #endregion CanExecuteChanged event

        #region ICommand methods

        /// <summary>
        /// Definiert die Methode, die feststellt, ob der Befehl in seinem aktuellen Zustand ausgeführt werden kann.
        /// </summary>
        /// <param name="parameter">Vom Befehl verwendete Daten. Wenn der Befehl keine Datenübergabe erfordert, kann dieses Objekt auf null gesetzt werden.</param>
        /// <returns>true, wenn dieser Befehl ausgeführt werden kann; andernfalls, false.</returns>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter) => this.isEnabled ?? canExecute?.Invoke(parameter) ?? true;

        /// <summary>
        /// Convenience method that invokes CanExecute without parameters.
        /// </summary>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        [DebuggerStepThrough]
        public bool CanExecute() => CanExecute(null);

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Vom Befehl verwendete Daten. Wenn der Befehl keine Datenübergabe erfordert, kann dieses Objekt auf null gesetzt werden.</param>
        /// <exception cref="InvalidOperationException">The <see cref="CanExecute(object)"/> method returns false.</exception>
        [DebuggerStepThrough]
        public void Execute(object parameter)
        {
            if (!CanExecute(parameter))
            {
                throw new InvalidOperationException("The command cannot be executed because CanExecute returned false.");
            }
            execute(parameter);
        }

        /// <summary>
        /// Einfache Methode ohne Parameter, die den Befehl ohne Parameter aufruft.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="CanExecute(object)"/> method returns false.</exception>
        [DebuggerStepThrough]
        public void Execute() => Execute(null);

        /// <summary>
        /// Invokes the command if the <see cref="CanExecute(object)"/> method returns true.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>true if this command was executed; otherwise, false.</returns>
        public bool TryExecute(object parameter)
        {
            if (CanExecute(parameter))
            {
                Execute(parameter);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Bequeme Methode, die den Befehl ohne Parameter aufruft, wenn die Methode <see cref="CanExecute(object)"/> den Wert „true“ zurückgibt.
        /// </summary>
        /// <returns>true if this command was executed; otherwise, false.</returns>
        [DebuggerStepThrough]
        public bool TryExecute() => TryExecute(null);

        #endregion ICommand methods

        /// <summary>
        /// Ruft einen Wert ab oder legt einen Wert fest, der angibt, ob das aktuelle DelegateCommand aktiviert ist. 
        /// Wenn dieser Wert nicht null ist, hat er Vorrang vor der Funktion canExecute, die im Konstruktor übergeben wurde. 
        /// Wenn keine Funktion übergeben wurde und dieser Wert null ist, ist der Befehl aktiviert.
        /// </summary>
        public bool? IsEnabled
        {
            [DebuggerStepThrough]
            get
            {
                return this.isEnabled;
            }
            [DebuggerStepThrough]
            set
            {
                if (value != this.isEnabled)
                {
                    this.isEnabled = value;
                    this.RaiseCanExecuteChanged();
                }
            }
        }
    }
}
