namespace EtlViewer.Viewer.Models
{
    using EtlViewer.QueryFx;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    
    class MainModel : DependencyObject, INotifyPropertyChanged
    {
        public static CancelEventHandler CloseEventHandler;

        internal SynchronizationContext context;
        List<string> etlFiles;
        public const string EtlFilesPropertyName = "EtlFiles";

        internal List<string> EtlFiles
        {
            get
            {
                return this.etlFiles;
            }
            set
            {
                this.etlFiles = value;
                OnPropertyChanged(this, EtlFilesPropertyName);
            }
        }
        DateTime activityStartTime;
        public CommandProcessor CommandLine { get; set; }
        TxReader reader;

        public MainModel(CommandProcessor commandProcessor, SynchronizationContext context)
        {
            this.context = context;
            this.CommandLine = commandProcessor;

            //Initialize and parse commandline arguments        
            if (this.CommandLine.noGui)
            {
                //The application will exit after showing the usersguide;
                MessageBox.Show("Please run SvcPerf.exe for the console version.");
                Environment.Exit(1);
                return;
            }

            this.FindNextCommand = new StringDelegateCommand();
            this.SendFeedbackCommand = new DelegateCommand<object>();
            this.StopReaderCommand = new DelegateCommand<object>();

            this.StopReaderCommand.CanExecuteTargets += () => this.CurrentObserver != null;
            this.StopReaderCommand.ExecuteTargets += (o) =>
                {
                    this.StopReading();
                };
        }


        #region Dependency properties
        public const string IsRealTimePropertyName = "IsRealTime";
        public static readonly DependencyProperty IsRealTimeProperty = DependencyProperty.Register(
          IsRealTimePropertyName,
          typeof(bool),
          typeof(MainModel),
          new FrameworkPropertyMetadata(OnPropertyChanaged));

        public bool IsRealTime
        {
            get { return (bool)this.GetValue(IsRealTimeProperty); }
            set { this.SetValue(IsRealTimeProperty, value); }
        }

        public const string SessionPropertyName = "Session";
        public static readonly DependencyProperty SessionProperty = DependencyProperty.Register(
            SessionPropertyName,
            typeof(string),
            typeof(MainModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public string Session
        {
            get { return (string)this.GetValue(SessionProperty); }
            set { this.SetValue(SessionProperty, value); }
        }

        public const string IsBusyPropertyName = "IsBusy";
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            IsBusyPropertyName,
            typeof(bool),
            typeof(MainModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public bool IsBusy
        {
            get { return (bool)this.GetValue(IsBusyProperty); }
            set { this.SetValue(IsBusyProperty, value); }
        }

        const string ActivityPropertyName = "Activity";
        public static readonly DependencyProperty ActivityProperty = DependencyProperty.Register(
            ActivityPropertyName,
            typeof(string),
            typeof(MainModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public string Activity
        {
            get { return (string)this.GetValue(ActivityProperty); }
            set { this.SetValue(ActivityProperty, value); }
        }

        public const string ReaderPropertyName = "Reader";
        public TxReader Reader
        {
            get { return this.reader; }
            set
            {
                this.reader = value;
                OnPropertyChanged(this, ReaderPropertyName);
            }
        }

        public const string StartTimePropertyName = "StartTime";
        public long StartTime
        {
            get { return (long)this.GetValue(StartTimeProperty); }
            set { this.SetValue(StartTimeProperty, value); }
        }

        public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register(
          StartTimePropertyName,
          typeof(long),
          typeof(MainModel),
          new PropertyMetadata(long.MinValue, OnStartTimeChanged));

        static void OnStartTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MainModel thisPtr = d as MainModel;
            long newStart = (long)e.NewValue;
            if (thisPtr.reader != null && newStart < thisPtr.Reader.SessionStartTime)
            {
                thisPtr.StartTime = thisPtr.Reader.SessionStartTime;
                Logger.Log("Cannot set start time as less than session start time.");
            }
            else if (thisPtr.reader != null && newStart > thisPtr.Reader.SessionStopTime)
            {
                thisPtr.StartTime = thisPtr.Reader.SessionStopTime;
            }
            else if (newStart > thisPtr.StopTime)
            {
                thisPtr.StartTime = thisPtr.StopTime;
            }

            OnPropertyChanged(thisPtr, e.Property.Name);
        }

        public const string StopTimePropertyName = "StopTime";
        public long StopTime
        {
            get { return (long)this.GetValue(StopTimeProperty); }
            set { this.SetValue(StopTimeProperty, value); }
        }

        public static readonly DependencyProperty StopTimeProperty = DependencyProperty.Register(
          StopTimePropertyName,
          typeof(long),
          typeof(MainModel),
          new PropertyMetadata(long.MaxValue, OnStopTimeChanged));

        static void OnStopTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MainModel thisPtr = d as MainModel;
            long newStop = (long)e.NewValue;
            if (thisPtr.reader != null && newStop < thisPtr.Reader.SessionStartTime)
            {
                thisPtr.StopTime = thisPtr.Reader.SessionStartTime;
                Logger.Log("Cannot set start time as less than session start time.");
            }
            else if (thisPtr.reader != null && newStop > thisPtr.Reader.SessionStopTime)
            {
                thisPtr.StopTime = thisPtr.Reader.SessionStopTime;
            }
            else if (newStop < thisPtr.StartTime)
            {
                thisPtr.StopTime = thisPtr.StartTime;
            }

            OnPropertyChanged(thisPtr, e.Property.Name);
        }

        public const string CanSelectTimeWindowPropertyName = "CanSelectTimeWindow";
        public static readonly DependencyProperty CanSelectTimeWindowProperty = DependencyProperty.Register(
                  CanSelectTimeWindowPropertyName,
                  typeof(bool),
                  typeof(MainModel),
                  new PropertyMetadata(false));

        public bool CanSelectTimeWindow
        {
            get { return (bool)GetValue(CanSelectTimeWindowProperty); }
            set { SetValue(CanSelectTimeWindowProperty, value); }
        }

        public const string CanShowEventsPropertyName = "CanShowEvents";
        public static readonly DependencyProperty CanShowEventsProperty = DependencyProperty.Register(
                  CanShowEventsPropertyName,
                  typeof(bool),
                  typeof(MainModel),
                  new PropertyMetadata(false));

        public bool CanShowEvents
        {
            get { return (bool)GetValue(CanShowEventsProperty); }
            set { SetValue(CanShowEventsProperty, value); }
        }

        static void OnPropertyChanaged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            MainModel model = sender as MainModel;
            string propertyName = e.Property.Name;
            OnPropertyChanged(model, propertyName);
        }

        private static void OnPropertyChanged(MainModel model, string propertyName)
        {
            if (propertyName == MainModel.StartTimePropertyName || propertyName == MainModel.StopTimePropertyName)
            {
                if (model.reader != null)
                {
                    model.reader.SetTimeFilter(model.StartTime, model.StopTime);
                }
            }
            else if (propertyName == IsRealTimePropertyName)
            {
                if (!model.IsRealTime)
                {
                    model.Session = null;
                }
            }

            model.UpdateState();

            if (model.PropertyChanged != null)
            {
                model.PropertyChanged(model, new PropertyChangedEventArgs(propertyName));
            }


        }

        #endregion

        #region Commands
        public static readonly DependencyProperty
                FindNextCommandProperty = DependencyProperty.Register("FindNextCommand",
                typeof(StringDelegateCommand),
                typeof(MainModel));

        public StringDelegateCommand FindNextCommand
        {
            get { return (StringDelegateCommand)GetValue(FindNextCommandProperty); }
            set { SetValue(FindNextCommandProperty, value); }
        }


        public static readonly DependencyProperty
        StopReaderCommandProperty = DependencyProperty.Register("StopReaderCommand",
        typeof(DelegateCommand<object>),
        typeof(MainModel));

        public DelegateCommand<object> StopReaderCommand
        {
            get { return (DelegateCommand<object>)GetValue(StopReaderCommandProperty); }
            set { SetValue(StopReaderCommandProperty, value); }
        }


        public static readonly DependencyProperty
        SendFeedbackCommandProperty = DependencyProperty.Register("SendFeedbackCommand",
                typeof(DelegateCommand<object>),
                typeof(MainModel));

        public DelegateCommand<object> SendFeedbackCommand
        {
            get { return (DelegateCommand<object>)GetValue(SendFeedbackCommandProperty); }
            set { SetValue(SendFeedbackCommandProperty, value); }
        }
        #endregion

        public TimeSpan ActivityDuration
        {
            get { return DateTime.Now - activityStartTime; }
        }

        #region Tx and Playback methods

        IObservable<EventRecordProxy> CurrentObserver { get; set; }

        public Task InitializeEtlReader()
        {
            if (this.Reader != null)
            {
                this.Reader.Dispose();
                this.Reader = null;
                this.ResetTimeWindow();
            }

            var t = TxHelper.GetReader(this.EtlFiles);
            this.IsRealTime = false;
            var context = TaskScheduler.FromCurrentSynchronizationContext();
            return t.ContinueWith((getReaderTask) =>
                {
                    this.Reader = getReaderTask.Result;
                }, context);
        }

        private void ResetTimeWindow()
        {
            this.StartTime = long.MinValue;
            this.StopTime = long.MaxValue;
        }

        public Task InitializeSession(string[] sessions)
        {
            TaskScheduler context = TaskScheduler.FromCurrentSynchronizationContext();
            var readerTask = TxHelper.GetReader(sessions, true);
            return readerTask.ContinueWith((t) =>
                {
                    this.reader = t.Result;
                    this.IsRealTime = true;
                    this.Session = sessions.Aggregate((p, c) => p + "," + c);
                }, context);
        }


        private void StopReading()
        {
            if (this.Reader != null && this.CurrentObserver != null)
            {
                var t = this.CurrentObserver;
                this.CurrentObserver = null;
                this.Reader.Stop(t);
                this.StopActivity("Stopped loading events.");
            }
        }

        internal IObservable<EventRecordProxy> GetObservable()
        {
            this.StopReading();
            this.CurrentObserver = this.reader.GetObservable();
            return this.CurrentObserver;
        }

        #endregion

        #region Manifest Methods
        public bool CompileManifest(List<string> files, out string error)
        {
            error = null;
            foreach (var manifest in files)
            {                
                string manifestError;
                ManifestCompiler.Compile(manifest, out manifestError);
                error += manifestError;
            }

            return String.IsNullOrEmpty(error);
        }
        #endregion

        internal void StartActivity(string message)
        {
            this.activityStartTime = DateTime.Now;
            this.Activity = message;
            this.IsBusy = true;
            Logger.Log("Start - " + message);
        }

        internal void StopActivity(string message)
        {
            this.IsBusy = false;
            this.Activity = message;
            Logger.Log(message);
        }

        internal void LogActivity(string message)
        {
            this.Activity = message;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void UpdateState()
        {
            bool canSelectTimeWindow = !this.IsRealTime &&
                            this.reader != null &&
                            this.StartTime != long.MinValue &&
                            this.StopTime != long.MaxValue;
            if (this.CanSelectTimeWindow != canSelectTimeWindow)
            {
                this.CanSelectTimeWindow = canSelectTimeWindow;
            }

            bool canShowEvents = this.IsRealTime && reader != null || canSelectTimeWindow;
            if (this.CanShowEvents != canShowEvents)
            {
                this.CanShowEvents = canShowEvents;
            }

            this.StopReaderCommand.CanExecute(null);
        }

        internal void Close(CancelEventArgs args)
        {
            if(CloseEventHandler != null)
            {
                CloseEventHandler.Invoke(this, args);
            }
        }
    }

    class DelegateCommand<T> : ICommand
    {
        Action<T> m_ExecuteTargets = delegate { };
        Func<bool> m_CanExecuteTargets = delegate { return false; };
        bool m_Enabled = false;

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            Delegate[] targets = m_CanExecuteTargets.GetInvocationList();
            foreach (Func<bool> target in targets)
            {
                m_Enabled = false;
                bool localenable = target.Invoke();
                if (localenable)
                {
                    m_Enabled = true;
                    break;
                }
            }
            return m_Enabled;
        }

        public void Execute(object parameter)
        {
            if (m_Enabled)
                m_ExecuteTargets((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion

        public event Action<T> ExecuteTargets
        {
            add
            {
                m_ExecuteTargets += value;
            }
            remove
            {
                m_ExecuteTargets -= value;
            }
        }

        public event Func<bool> CanExecuteTargets
        {
            add
            {
                m_CanExecuteTargets += value;
                //CanExecuteChanged(this, EventArgs.Empty);
            }
            remove
            {
                m_CanExecuteTargets -= value;
                //CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }

}
