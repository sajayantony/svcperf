namespace EtlViewer.Viewer
{
    using EtlViewer.QueryFx;
    using EtlViewer.Viewer.Controls;
    using EtlViewer.Viewer.Models;
    using EtlViewer.Viewer.Plugins;
    using EtlViewer.Viewer.UIUtils;
    using EtlViewer.Viewer.Views;
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Navigation;
    using System.Windows.Threading;
    using Utilities;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    partial class MainWindow : Window
    {
        public static MainWindow Instance;
        internal MainModel Model { get; set; }
        public EventsModel EventsModel { get; set; }
        public FilterModel FilterModel { get; set; }
        public EventStatsModel EventStatsModel { get; set; }
        LogViewer logwindow;
        CommandProcessor CommandLine { get { return this.Model.CommandLine; } }

        internal SynchronizationContext Context = null;
        bool onInitialized = false;
        LoadingAdorner loadingAdorner;

        Task timelineLoadingTask;

        public List<string> EtlFiles
        {
            get { return this.Model.EtlFiles; }
            set { this.Model.EtlFiles = value; }
        }

        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(ExceptionHelper.HanldeUnhandledException);
            this.Model = new MainModel(App.CommandLineArgs, SynchronizationContext.Current);
            this.logwindow = new LogViewer();
            this.logwindow.DataContext = this.Model;
            ExceptionHelper.logviewer = this.logwindow;
            this.EventsModel = new EventsModel();
            this.FilterModel = new FilterModel();
            this.TimelineModel = new TimelineModel(this.Model);
            this.EventStatsModel = new EventStatsModel();
            Instance = this;

            InitializeComponent();
            SystemCommandHandler.Bind(this);
            this.DataContext = this.Model;
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Setup adorner for the grid.
            this.loadingAdorner = new LoadingAdorner(this.eventsGrid,
                "Press F5 or Enter in the Source Filter box to refresh grid");
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this.eventsGrid);
            adornerLayer.Add(loadingAdorner);
            this.loadingAdorner.Visibility = System.Windows.Visibility.Visible;

            // Setup help for the main window.
            HelpProvider.OnHelp += this.CommandBinding_Help;

            this.EventTimeline.DataContext = this.TimelineModel;
            this.Spinny.DataContext = this.Model;
            this.eventsGrid.DataContext = this.EventsModel;
            this.eventsGrid.OnCellCopy += new Action<string, EventRecordProxy>(eventsGrid_OnCellCopy);
            this.eventsGrid.OnRangeSelected += new Action<TimeSpan, TimeSpan>(eventsGrid_OnRangeSelected);
            this.eventsGrid.OnSelectInterval += new Action<TimeSpan, TimeSpan>(eventsGrid_OnSelectInterval);
            this.Model.PropertyChanged += Model_PropertyChanged;

            // FindCommand Setup
            this.Model.FindNextCommand.CanExecuteTargets += () => this.EventsModel.HasEvents;
            this.Model.FindNextCommand.ExecuteTargets += (o) =>
                {
                    this.FilterModel.Mode = FilterMode.Search;
                    this.SearchText(this.FilterModel.FilterText.Trim());
                };

            this.filterbox.DataContext = this.FilterModel;
            this.KeywordFilterListBox.DataContext = this.FilterModel;
            this.ProviderFilterListBox.DataContext = this.FilterModel;
            this.FilterModel.FilterCommand.ExecuteTargets += FilterCommand_ExecuteTargets;
            this.FilterModel.PropertyChanged += FilterModel_PropertyChanged;
            this.SetupEventStats();
            this.SetupQueryCommand();
            this.SetupPlugins();
        }

        private void SetupEventStats()
        {
            this.EventStatsModel.ViewStatsCommand.CanExecuteTargets += () =>
            {
                return this.Model.Reader != null && !this.Model.IsRealTime;
            };
            this.EventStatsModel.ViewStatsCommand.ExecuteTargets += (o) =>
            {
                EventStatsWindow stats = new EventStatsWindow();
                stats.DataContext = this.EventStatsModel;
                stats.Show();
            };

            this.EventStatsModel.EventStatSelectedCommand.CanExecuteTargets += () =>
            {
                return this.Model.Reader != null;
            };
            this.EventStatsModel.EventStatSelectedCommand.ExecuteTargets += (stat) =>
            {
                this.FilterModel.FilterText = "Id=" + stat.Id;
                this.Activate();
                this.EnsureFindPanel();
            };
        }

        private void SetupPlugins()
        {
            PluginProcessor.LoadPluginsAsync()
                .ContinueWith((t) =>
                {
                    if (!t.IsFaulted)
                    {
                        PluginProcessor.AddEntryPoints(t.Result, new EtlViewerContext(this.Model), this.ViewMenu);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext()); ;
        }

        void FilterCommand_ExecuteTargets(string obj)
        {
            switch (FilterModel.Mode)
            {
                case FilterMode.Source:
                    this.RefreshSource();
                    break;
                case FilterMode.View:
                    this.RefreshView();
                    break;
                case FilterMode.Search:
                    this.SearchText(this.FilterModel.FilterText.Trim());
                    break;
            }
        }

        void CommandBinding_Refresh(object sender, ExecutedRoutedEventArgs e)
        {
            this.FilterCommand_ExecuteTargets(null);
        }

        void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MainModel.ReaderPropertyName)
            {
                if (this.Model.Reader != null)
                {
                    this.LoadTimeLineAsync();
                    this.EventStatsModel.Reader = this.Model.Reader;
                }
            }
            else if (e.PropertyName == MainModel.IsBusyPropertyName)
            {
                //TODO: Fix realtime busy indicators.
                if (!this.Model.IsRealTime)
                {
                    Mouse.OverrideCursor = this.Model.IsBusy ? Cursors.Wait : null;
                }
            }
        }

        void FilterModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == FilterModel.ModePropertyName)
            {
                this.EventsModel.HighlightText = string.Empty;
            }
            else if (e.PropertyName == FilterModel.ResolverPropertyName)
            {
                this.LoadDurationEvents(this.FilterModel.Resolver);
            }
        }

        #region EventsGrid eventhandlers

        void eventsGrid_OnSelectInterval(TimeSpan start, TimeSpan stop)
        {
            this.Model.StartTime = start.Ticks;
            this.Model.StopTime = stop.Ticks;
        }

        void eventsGrid_OnRangeSelected(TimeSpan start, TimeSpan stop)
        {
            this.txtDuration.Text = stop.Subtract(start).TotalMilliseconds + " ms";
        }

        void eventsGrid_OnCellCopy(string filter, EventRecordProxy arg2)
        {
            if (String.IsNullOrEmpty(this.FilterModel.FilterText))
            {
                this.FilterModel.FilterText = filter;
            }
            else
            {
                this.FilterModel.FilterText += " and " + filter;
            }

            //Ensure after you set the text to hide watermark;
            this.EnsureFindPanel();
        }
        #endregion

        private void EnsureFindPanel()
        {
            if (this.FindPanel.Visibility != System.Windows.Visibility.Visible)
            {
                this.FindPanel.Visibility = System.Windows.Visibility.Visible;
            }

            this.filterbox.Focus();
        }

        void LoadDurationEvents(Resolver resolver)
        {
            if (resolver != null)
            {
                var symbols = resolver.Symbols;
                if (symbols != null)
                {
                    var items = resolver.Symbols.OrderBy((e) => e.Name).ToList();
                    this.StartEvent.ItemsSource = items;
                    this.StopEvent.ItemsSource = items;
                    this.ProviderFilterListBox.SelectedItem = this.FilterModel.Providers.Where((e) => e.Name == resolver.ProviderName).FirstOrDefault();
                }
                else
                {
                    if (this.StartEvent.Items != null)
                    {
                        this.StartEvent.Items.Clear();
                    }
                    if (this.StopEvent.Items != null)
                    {
                        this.StopEvent.Items.Clear();
                    }
                }
            }
        }

        void BtnDuration_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(this.StartEvent.Text) || String.IsNullOrWhiteSpace(this.StopEvent.Text))
            {
                return;
            }
            QueryModel queryModel = new QueryModel(this.Model);
            DurationWindow durationWindow = new DurationWindow();
            durationWindow.Model = queryModel;
            durationWindow.ShowDuration(this.FilterModel.Resolver,
                                        this.Model.StartTime,
                                        this.Model.StopTime,
                                        this.StartEvent.Text,
                                        this.StopEvent.Text);
        }

        void LoadEtlFileFromDialog()
        {
            if (this.EtlFiles == null
                || this.EtlFiles.Count == 0
                || MessageBoxResult.Yes == MessageBox.Show("Do you want to unload the current set of files and load a new one?", "Confirm File Load", MessageBoxButton.YesNo))
            {
                SelectFileAndExecuteAction("etl", "ETL files (*.etl)|*.etl|All files (*.*)|*.*", this.ParseAndLoadEtlFiles);
            }
        }

        void LoadEtlsOnContext(IEnumerable<string> validatedFileNames)
        {
            string[] etlfiles = validatedFileNames.ToArray();
            int fileCount = etlfiles.Length;
            if (fileCount > 63)
            {
                ExceptionHelper.ShowException(new InvalidOperationException(
                    string.Format("Trying to load {0} files. SvcPerf supports only up to 63 files.", fileCount)));
                var dest = new string[63];
                Array.Copy(etlfiles, dest, 63);
                etlfiles = dest;
            }

            this.EtlFiles = etlfiles.ToList();
            string files = string.Join(",", validatedFileNames.ToArray());
            this.Model.StartActivity(string.Format("Loading Etl {0} files\n\t {1}",
                                    etlfiles.Length,
                                    etlfiles.Aggregate((c, acc) => c + "\n\t" + acc)));
            this.Title = files;
            if (this.CheckContinueIfLargeFile(validatedFileNames))
            {
                this.Model.InitializeEtlReader().ContinueWith((task) =>
                {
                    // You have loaded a new ETL file so you need to requery the source.
                    this.FilterModel.Mode = FilterMode.Source;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            // The findpanel watermark should not show up. 
            this.EnsureFindPanel();
        }

        void StopActivity(string message)
        {
            this.Post(() => this.Model.StopActivity(message));
        }

        bool CheckContinueIfLargeFile(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                FileInfo info = new FileInfo(file);
                if (info.Length > 10 * 1024 * 1024)
                {
                    if (file.StartsWith("\\"))
                    {
                        switch (MessageBox.Show(
                            string.Format("This is {0} MB ETL file on a network share. Do you want to continue accessing the file from the network? If NO then please copy the file locally and reopen. ", info.Length / (1024 * 1024)),
                            "File Load",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning))
                        {
                            case MessageBoxResult.No:
                                return false;
                            case MessageBoxResult.Yes:
                                return true;
                        }
                    }
                }
            }

            return true;
        }

        void RefreshSource()
        {
            if (this.Model.Reader == null)
            {
                this.LoadEtlFileFromDialog();
                return;
            }
            else if (this.timelineLoadingTask != null
                    && this.timelineLoadingTask.Status != TaskStatus.RanToCompletion)
            {
                MessageBox.Show("Please wait for timeline to be loaded.");
                return;
            }
            this.FilterModel.Mode = FilterMode.Source;
            this.loadingAdorner.Message = "Loading events.....";
            this.loadingAdorner.Visibility = System.Windows.Visibility.Visible;
            this.Model.StartActivity("Loading and updating events table ...");
            this.EventsModel.SourceFilter = this.FilterModel.UpdateReaderFilters(this.Model.Reader);
            this.RefreshGrid();
        }

        void RefreshGrid()
        {
            Logger.Log("Refreshing grid.");
            TxReader reader = this.Model.Reader;
            IList<EventRecordProxy> events = null;
            int bufferCount = reader.IsRealtime ? 1 : 1000;
            IObservable<EventRecordProxy> observable = null;
            IDisposable subscriber = null;
            TaskCompletionSource<object> eventLoading = new TaskCompletionSource<object>();
            TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Action<IList<EventRecordProxy>, IList<EventRecordProxy>> AddRange = (list, data) =>
            {
                lock (list)
                {
                    EventRecordProxy prev = list.Count > 0 ? list[list.Count - 1] : null;
                    foreach (var item in data)
                    {
                        item.Previous = prev;
                        list.Add(item);
                        prev = item;
                    }
                }
            };

            Action onComplete = () =>
            {
                if (!eventLoading.Task.IsCompleted
                    && !eventLoading.Task.IsFaulted)
                {
                    eventLoading.SetResult(null);
                }
            };

            try
            {
                observable = this.Model.GetObservable();
                // Starting grid loading            
                this.Model.StartActivity("Loading events ....");
                if (reader.IsRealtime)
                {
                    events = new ObservableCollection<EventRecordProxy>();
                    this.EventsModel.Items = events;
                    this.loadingAdorner.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    events = new List<EventRecordProxy>();
                    this.loadingAdorner.Message = "Event loading in progress.";
                    this.loadingAdorner.Visibility = System.Windows.Visibility.Visible;
                }

                subscriber = observable
                    .Buffer(bufferCount)
                    // TODO why posting on the UI thread doesn't work
                    // .SubscribeOn(SynchronizationContext.Current)
                    .Finally(onComplete)
                    .Subscribe(loadedData =>
                    {
                        try
                        {
                            if (reader.IsRealtime)
                            {
                                this.Post(() => AddRange(events, loadedData));
                            }
                            else
                            {
                                AddRange(events, loadedData);
                            }

                            this.Post(() =>
                            {
                                this.Model.LogActivity(string.Format("Loaded {0} events ", events.Count));
                            });
                        }
                        catch (Exception ex)
                        {
                            eventLoading.SetException(ex);
                        }
                    },
                    (exception) =>
                    {
                        eventLoading.SetException(exception);
                        Logger.Log(exception);
                        this.Post(() =>
                        {
                            this.Model.StopActivity(exception.Message);
                        });
                    });

                reader.StartPublish(observable);
            }
            catch (Exception ex)
            {
                eventLoading.SetException(ex);
            }


            eventLoading.Task.ContinueWith((t) =>
                {
                    if (t.IsFaulted)
                    {
                        Logger.Log(t.Exception);
                        this.Model.StopReaderCommand.Execute(null);
                        this.Model.StopActivity("Could not load events");
                        this.FilterModel.FilterException = t.Exception;
                        this.loadingAdorner.Visibility = System.Windows.Visibility.Hidden;
                        return;
                    }

                    if (reader.IsRealtime)
                    {
                        return;
                    }
                    else
                    {
                        //Finally set the item source to avoid adding on bound item collection.
                        this.EventsModel.Items = events;
                        this.loadingAdorner.Visibility = System.Windows.Visibility.Hidden;
                        this.Model.StopActivity(string.Format(
                                            "Loaded {0} in {1} seconds",
                                            events.Count,
                                            this.Model.ActivityDuration.TotalSeconds));

                        // Switch to View mode after filtering source automatically.
                        this.FilterModel.Mode = FilterMode.View;
                    }

                }, scheduler);
        }

        void LoadTimeLineAsync()
        {
            this.Model.StartActivity("Loading timeline....");
            TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            this.timelineLoadingTask = tcs.Task;

            TxReader reader = this.Model.Reader;
            Contract.Assert(reader != null);
            int bufferSize = 5000;
            int count = 0;
            this.EventTimeline.StartLoad();
            this.TimelinePanel.Visibility = System.Windows.Visibility.Visible;

            Task timelineTask = new Task(() =>
            {
                var timeline = reader.GetRawEventsForTimeWindow();
                var q = from e in timeline
                        select new TimelineEvent
                        {
                            Level = (byte)(e.Level % 6),
                            Ticks = e.TimeStamp.Ticks
                        };

                q.Buffer(bufferSize)
                    .Finally(() => tcs.SetResult(null))
                    .Subscribe((data) =>
                    {
                        count += data.Count;

                        this.Dispatcher.BeginInvoke(((Action)(() =>
                       {
                           this.TimelineModel.Populate(data);
                           this.Model.LogActivity(String.Format("Timeline Loaded {0} Events", count));
                       })));
                    });
            });
            timelineTask.Start();

            timelineTask.ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception);
                }
            });

            tcs.Task.ContinueWith((loadingTask) =>
            {
                if (loadingTask.IsFaulted)
                {
                    ExceptionHelper.ShowException(loadingTask.Exception);
                    return;
                }

                this.EventTimeline.LoadComplete();
                this.Model.StartTime = this.TimelineModel.StartTime;
                this.Model.StopTime = this.TimelineModel.StopTime;
                this.Model.StopActivity(String.Format("Loaded with {0} events in {1}", count, this.Model.ActivityDuration.TotalSeconds));

                if (!reader.SessionWindowInitialized)
                {
                    reader.SessionWindowInitialized = true;
                    reader.SessionStartTime = this.Model.StartTime;
                    reader.SessionStopTime = this.Model.StopTime;
                }
            }, scheduler);
        }

        DateTime? ParseInterval(string p)
        {
            if (String.IsNullOrWhiteSpace(p) || p.Length < 2)
                return null;

            try
            {
                char[] unit = p.Where((c) => char.IsLetter(c)).ToArray();
                string value = new string(p.Where((c) => !char.IsLetter(c)).ToArray());
                double t;
                double.TryParse(value, out t);
                switch (new string(unit))
                {
                    case "ms": return new DateTime(TimeSpan.FromMilliseconds(t).Ticks);
                }
            }
            catch (Exception)
            {

            }

            return null;
        }

        void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                this.ParseAndLoadEtlFiles(droppedFilePaths);
            }
        }

        void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.LoadEtlFileFromDialog();
        }

        void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void CommandBinding_CanExecute_Find(object sender, CanExecuteRoutedEventArgs e)
        {
            FilterMode mode = FilterMode.Source;
            if (e.Parameter != null)
            {
                mode = (FilterMode)e.Parameter;
            }

            if (mode == FilterMode.Source && this.Model.Reader != null)
            {
                e.CanExecute = true;
            }
            else if (this.EventsModel.HasEvents)
            {
                e.CanExecute = true;
            }
        }

        void CommandBinding_Duration(object sender, ExecutedRoutedEventArgs e)
        {
            this.myExpander.IsExpanded = true;

            if (this.myExpander.IsExpanded)
            {
                this.myExpander.Focus();
                this.StartEvent.Focus();
            }
        }

        private void RefreshView()
        {
            if (!this.EventsModel.HasEvents)
            {
                this.RefreshSource();
                return;
            }

            this.Model.StartActivity("Applying filter on events view.");
            Exception completionException = null;
            try
            {
                this.EnsureFindPanel();
                this.EventsModel.ViewFilter = this.FilterModel.UpdateReaderFilters(this.Model.Reader);
                this.Dispatcher.Invoke((Action)(() =>
                {
                    try
                    {
                        this.EventsModel.Filter(this.Model.Reader);
                        this.Model.StopActivity("Applied Filter");
                    }
                    catch (Exception ex)
                    {
                        completionException = ex;
                        this.Model.StopActivity("Exception:" + ex.Message);
                    }
                    finally
                    {
                        this.FilterModel.FilterException = completionException;
                    }
                }));
            }
            catch (Exception ex)
            {
                completionException = ex;
            }
            finally
            {
                this.FilterModel.FilterException = completionException;
            }
        }

        void CommandBinding_Clear(object sender, ExecutedRoutedEventArgs e)
        {
            this.FilterModel.FilterText = string.Empty;
        }

        void CommandBinding_Close(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        void CommandBinding_Help(object sender, ExecutedRoutedEventArgs e)
        {
            FrameworkElement source = e.Source as FrameworkElement;

            if (source != null)
            {
                string helpString = HelpProvider.GetHelp(source);
                if (!String.IsNullOrEmpty(helpString))
                {
                    this.DisplayUsersGuide(helpString);
                    return;
                }
            }

            this.DisplayUsersGuide();
        }

        void CommandBinding_Find(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter != null)
            {
                FilterMode mode = (FilterMode)e.Parameter;
                this.FilterModel.Mode = mode;
            }

            this.EnsureFindPanel();
        }

        void CommandBinding_AddManifest(object sender, ExecutedRoutedEventArgs e)
        {
            this.AddManifestMenu_Click(sender, null);
        }       

        void Window_Activated(object sender, EventArgs e)
        {
            if (this.onInitialized)
            {
                return;
            }

            this.Context = SynchronizationContext.Current;
            this.onInitialized = true;
            this.FilterModel.FilterText = CommandLine.filter;


            ThreadPool.QueueUserWorkItem((s) =>
            {
                string[] manifests = this.CommandLine.manifests;
                string[] queries = this.CommandLine.queries;
                string[] inputFiles = this.CommandLine.inputFiles;

                // There might be manifests without qualifiers.
                List<string> manifestsWithoutQualifiers = null;
                string error;
                if (FileUtilities.ValidateAndEnumerate(inputFiles, ref manifestsWithoutQualifiers, ".man", out error))
                {
                    if (manifests != null)
                    {
                        manifestsWithoutQualifiers.AddRange(manifests);
                    }

                    manifests = manifestsWithoutQualifiers.ToArray();
                }

                this.ParseAndLoadManifests(manifests);
                this.ParseAndLoadQueries(inputFiles);
                this.ParseAndLoadQueries(queries);

                if (this.CommandLine.command == SvcPerfCommand.ShowUIRealtime &&
                    !(this.CommandLine.session == null && this.CommandLine.session.Length > 0))
                {
                    this.Post(() => this.Model.InitializeSession(this.CommandLine.session));
                }
                else
                {
                    this.ParseAndLoadEtlFiles(inputFiles);
                }
            });
        }

        void ParseAndLoadEtlFiles(string[] paths)
        {
            List<string> files = null;

            // Try load ETL"s if found. 
            if (ValidateAndEnumerate(paths, ref files, ".etl", false))
            {
                this.Post(() => this.LoadEtlsOnContext(files));
            }
        }

        void ParseAndLoadManifests(string[] manifests)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                List<string> files = null;
                string error;
                if (ValidateAndEnumerate(manifests, ref files, ".man"))
                {
                    Logger.Log("Loading Manifests: " + String.Join(",", files));
                    if (this.Model.CompileManifest(files, out error))
                    {                        
                        Resolver[] resolvers = SymbolHelper.Parse(manifests);
                        this.FilterModel.AddProviders(resolvers);
                        // You have loaded a manifest so you need to requery the source.
                        this.FilterModel.Mode = FilterMode.Source;
                        this.Model.LogActivity("Manifest loaded.");
                    }
                    else
                    {
                        this.FilterModel.FilterException = new Exception(error);
                        ExceptionHelper.ShowException(new InvalidOperationException(error));
                    }
                }

            }));

            Task.Factory.StartNew(() => CsharpSyntaxWalker.EnsureInitialized());
        }

        static bool ValidateAndEnumerate(string[] input, ref List<string> files, string validExtension, bool shouldExist = true)
        {
            string errorMessage;
            if (!FileUtilities.ValidateAndEnumerate(input, ref files, validExtension, out errorMessage))
            {
                if (!string.IsNullOrEmpty(errorMessage) && shouldExist)
                {
                    MessageBox.Show(errorMessage, "ArgumentException", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return false;
            }

            return true;
        }

        void ShowErrors_Click(object sender, RoutedEventArgs e)
        {
            this.FilterModel.FilterText = "Level<3";
            this.RefreshSource();
        }


        void DurationStats_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Select the provider and the start and stop events and click 'Find Duration'");
            CommandBinding_Duration(null, null);
        }

        void RangeSelect_Click(object sender, RoutedEventArgs e)
        {
            this.RefreshSource();
        }

        void AddManifestMenu_Click(object sender, RoutedEventArgs e)
        {
            SelectFileAndExecuteAction("man", "manifests (*.man)|*.man|All files (*.*)|*.*", this.ParseAndLoadManifests);

            this.EnsureFindPanel();
        }

        private void SetupQueryCommand()
        {
            Dictionary<string, ParameterResolver> resolvers = new Dictionary<string, ParameterResolver>();
            resolvers.Add("StartEvent", new EventParameterResolver(null));
            resolvers.Add("StopEvent", new EventParameterResolver(null));
            resolvers.Add("StartTime", new ParameterResolver(DateTime.Now.ToString()));
            resolvers.Add("StopTime", new ParameterResolver(DateTime.Now.ToString()));
            resolvers.Add("BucketSize", new ParameterResolver(100));
            resolvers.Add("ProviderId", new ParameterResolver(Guid.Empty.ToString()));

            //Diagram default
            resolvers.Add("ActivityDepth", new ParameterResolver(5));
            resolvers.Add("MaxEvents", new ParameterResolver(100));
            resolvers.Add("DiagramName", new ParameterResolver(null));

            //Pseudo perf counter defaults.
            resolvers.Add("WaitTime", new ParameterResolver(60));
            resolvers.Add("SampleTime", new ParameterResolver(10));

            QueryEditorView.NewCommand.CanExecuteTargets += () => true;
            QueryEditorView.NewCommand.ExecuteTargets += (o) =>
            {
                ShowNewQueryWindow(null, true, resolvers);
            };

            QueryEditorView.OpenCommand.CanExecuteTargets += () => true;
            QueryEditorView.OpenCommand.ExecuteTargets += (s) =>
            {
                if (File.Exists(s))
                {
                    ShowNewQueryWindow(s, true, resolvers);
                }
                else
                {
                    OpenNewQuery(resolvers);
                }
            };

            this.Model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == MainModel.StartTimePropertyName)
                {
                    if (this.Model.StartTime == long.MinValue)
                    {
                        resolvers["StartTime"].Value = DateTime.MinValue.ToString();
                    }
                    else
                    {
                        resolvers["StartTime"].Value = new DateTime(this.Model.StartTime).ToString();
                    }
                }
                else if (e.PropertyName == MainModel.StopTimePropertyName)
                {
                    if (this.Model.StopTime == long.MaxValue)
                    {
                        resolvers["StopTime"].Value = DateTime.MaxValue.ToString();
                    }
                    else
                    {
                        resolvers["StopTime"].Value = new DateTime(this.Model.StopTime).ToString();
                    }
                }
            };

            this.FilterModel.PropertyChanged += (s, p) =>
            {
                if (p.PropertyName == FilterModel.ResolverPropertyName)
                {
                    if (this.FilterModel.Resolver != SymbolHelper.Empty && this.FilterModel.Resolver != null)
                    {
                        resolvers["ProviderId"].Value = this.FilterModel.Resolver.ProviderId.ToString();
                        resolvers["DiagramName"].Value = this.FilterModel.Resolver.ProviderName;
                        var options = resolvers["StartEvent"].Options;
                        options.Clear();
                        foreach (var item in this.FilterModel.Resolver.Symbols)
                        {
                            options.Add(new ParameterOption()
                            {
                                Name = item.Name + " - " + item.Id,
                                Value = item.Name
                            });
                        }
                        options = resolvers["StopEvent"].Options;
                        options.Clear();
                        foreach (var item in this.FilterModel.Resolver.Symbols)
                        {
                            options.Add(new ParameterOption()
                            {
                                Name = item.Name + " - " + item.Id,
                                Value = item.Name
                            });
                        }
                    }
                }
            };
        }

        void OpenNewQuery(Dictionary<string, ParameterResolver> parameterResolvers)
        {
            SelectFileAndExecuteAction("linq", "linq query (*.linq)|*.linq|All files (*.*)|*.*", this.ParseAndLoadQueries);
        }

        void ParseAndLoadQueries(string[] queries)
        {
            List<string> files = null;
            if (ValidateAndEnumerate(queries, ref files, ".linq", false))
            {
                Logger.Log("Loading Queries: " + String.Join(",", files));
                if (files.Count() > 0)
                {
                    foreach (var file in files)
                    {
                        Post(() =>
                            {
                                if (QueryEditorView.OpenCommand.CanExecute(file))
                                {
                                    QueryEditorView.OpenCommand.Execute(file);
                                }
                            });
                    }
                }
            }            
        }

        private void ShowNewQueryWindow(string fileName,
                                        bool show,
                                        Dictionary<string, ParameterResolver> resolvers)
        {
            if (QueryWindows.Activate(fileName))
            {
                return;
            }


            QueryModel queryModel = new QueryModel(this.Model);
            QueryEditorModel editor = new QueryEditorModel();
            editor.ParameterResolvers = resolvers;
            editor.LoadFile(fileName);
            QueryEditorView view = new QueryEditorView(editor, queryModel);
            QueryWindows.Create(view, show);

            // Add to the menu collection.
            MenuItem newMenu = new MenuItem();
            newMenu.Click += (s, eargs) => QueryWindows.Activate(view);
            newMenu.DataContext = view;
            Binding b = new Binding("Title");
            b.Source = view;
            newMenu.SetBinding(MenuItem.HeaderProperty, b);
            int index = QueryWindows.Count();
            if (String.IsNullOrEmpty(fileName))
            {
                //  Add ":" to give create an invalid filename.
                view.Title = string.Format("{0}{1}New Query", index, QueryEditorView.TitleSeperator);
            }
            else
            {
                view.Title = string.Format("{0}{1}{2}", index, QueryEditorView.TitleSeperator, fileName);
            }

            this.QueriesMenu.Items.Add(newMenu);

            // Remove from the menu collection upon close.
            CancelEventHandler closeHandler = (sender, args) =>
            {
                QueryWindows.Activate(view);
                view.Close(args);
            };

            queryModel.OnDisposed += (s, e1) =>
            {
                this.QueriesMenu.Items.Remove(newMenu);
                QueryWindows.Remove(view);
                MainModel.CloseEventHandler -= closeHandler;
            };


            MainModel.CloseEventHandler += closeHandler;
        }

        void Post(Action action)
        {
            this.Dispatcher.BeginInvoke(action);
        }

        public static RoutedUICommand AddManifestCommand
        {
            get
            {
                RoutedUICommand command = new RoutedUICommand("Add Manifest", "Add Manifest", typeof(MainWindow));
                command.InputGestures.Add(new KeyGesture(Key.M, ModifierKeys.Control));
                return command;
            }
        }

        public static RoutedUICommand UsersGuideCommand
        {
            get
            {
                RoutedUICommand command = new RoutedUICommand("UsersGuide", "UsersGuide", typeof(MainWindow));
                command.InputGestures.Add(new KeyGesture(Key.F1));
                return command;
            }
        }

        #region Find methods
        private void SearchText(string pattern)
        {
            if (!this.EventsModel.HasEvents)
            {
                StopActivity("Events not loaded.");
                return;
            }

            if (String.IsNullOrEmpty(pattern))
            {
                StopActivity("Enter find text");
                return;
            }

            if (this.EventsModel.SearchText != pattern ||
                (this.EventsModel.SearchEnumerator != null && this.EventsModel.CurrenView.CurrentPosition != this.EventsModel.SearchEnumerator.Current))
            {
                this.EventsModel.SearchText = pattern;
                this.EventsModel.SearchEnumerator = this.eventsGrid.FindTextInEventsTable(pattern).GetEnumerator();
            }
            this.Model.StartActivity("Searching for pattern" + pattern);
            if (this.EventsModel.SearchEnumerator.MoveNext())
            {
                if (this.EventsModel.SearchEnumerator.Current >= 0)
                {
                    this.StopActivity("Found event at index " + this.EventsModel.SearchEnumerator.Current);
                }
                else
                {
                    StopActivity(string.Format("No match found for search string '{0}'", pattern));
                }
            }
        }

        private void FindPanelCloseClick(object sender, RoutedEventArgs e)
        {
            this.FindPanel.Visibility = System.Windows.Visibility.Collapsed;
            this.EventsModel.HighlightText = string.Empty;
        }

        #endregion Find methods

        #region Help Methods
        WebBrowserWindow s_Browser;
        private static string s_BrowserUrl;
        private TimelineModel TimelineModel;

        internal bool DisplayUsersGuide(string anchor = null)
        {
            string usersGuideFilePath = Path.Combine(SupportFiles.SupportFileDir, "UsersGuide.htm");
            string baseUrl = "file://" + usersGuideFilePath.Replace('\\', '/').Replace(" ", "%20");
            string fullUrl = baseUrl;
            if (!string.IsNullOrEmpty(anchor))
                fullUrl = fullUrl + "#" + anchor;

            if (s_Browser == null)
            {
                s_Browser = new WebBrowserWindow();
                s_Browser.Title = "SvcPerf's Help";

                // When you simply navigate, you don't remember your position.  In the case
                // Where the browser was closed you can at least fix it easily by starting over.
                // Thus we abandon browers on close.  
                s_Browser.Closing += delegate
                {
                    s_Browser = null;
                };

                s_Browser.Browser.Navigating += delegate(object sender, NavigatingCancelEventArgs e)
                {
                    var uri = e.Uri;
                    if (uri != null && uri.Host.Length > 0)
                    {
                        var naviateToWeb = ConfigurationManager.AppSettings["AllowNavigateToWeb"];
                        if (naviateToWeb != "true")
                        {
                            var result = MessageBox.Show(
                                "SvcPerf is about to fetch content from the web.\r\nIs this OK?",
                                "Navigate to Web", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                naviateToWeb = "true";
                                ConfigurationManager.AppSettings["AllowNavigateToWeb"] = naviateToWeb;
                            }
                            else
                                e.Cancel = true;
                        }
                    }
                };
                s_Browser.Browser.Navigated += delegate(object sender, NavigationEventArgs e)
                {
                    Logger.Log("Navigated to {0}", e.Uri);
                    if (s_BrowserUrl != fullUrl)
                    {
                        Logger.Log("Again to {0}", fullUrl);
                        s_BrowserUrl = fullUrl;
                        s_Browser.Browser.Navigate(new Uri(fullUrl));
                        return;
                    }
                    s_Browser.Browser.Focus();
                    //Images don't show up on anchors.
                    // If navigation is broken uncomment this for older browsers.
                    //s_Browser.Browser.Refresh();
                };
                s_Browser.Show();
                s_BrowserUrl = baseUrl;
                // First navigate to the top of the page (otherwise it seems to fail an not get to the anchor).
                // This produces an annoying flash, but not going to the correct anchor is worse.  
                Logger.Log("Navigating to {0}", baseUrl);
                s_Browser.Browser.Navigate(new Uri(baseUrl));
            }
            else
            {
                // GuiApp.MainWindow.StatusBar.LogWriter.WriteLine("Navigating to {0}", fullUrl);
                s_BrowserUrl = fullUrl;
                s_Browser.Browser.Navigate(new Uri(fullUrl));
                s_Browser.Show();
            }
            return true;
        }
        #endregion Help Methods


        private void txtStartTime_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {

        }

        private void txtStopTime_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {

        }

        private void ProviderFilterListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.FilterModel.SelectProviderCommand.CanExecute(null))
            {
                this.FilterModel.SelectProviderCommand.Execute(this.ProviderFilterListBox.SelectedItem);
            }
        }

        private void window_Closing_1(object sender, CancelEventArgs e)
        {
            this.Model.Close(e);
        }

        private void window_Closed_1(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        void SelectFileAndExecuteAction(string mruFileType, string filter, Action<string[]> actionOnSuccess)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = filter;
            dialog.FilterIndex = 1;
            dialog.Multiselect = true;
            dialog.InitialDirectory = MruFileHelper.GrabMruDirectory(mruFileType);
            if (dialog.ShowDialog() == true)
            {
                actionOnSuccess(dialog.FileNames);
            }
        }
    }
}

