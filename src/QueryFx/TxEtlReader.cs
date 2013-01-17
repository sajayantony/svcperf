namespace EtlViewer.QueryFx
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading;
    using Tx.Windows;

    class TxReader : EtlReader
    {
        static object thislock = new object();
        public readonly Func<Guid, bool> ProviderFilter;
        public readonly Func<ulong, bool> KeywordFilter;
        public readonly Func<long, bool> TimeFilter;

        public TxReader(IEnumerable<string> files, bool isRealtime)
            : base(files)
        {
            this.IsRealtime = isRealtime;

            //Currently these are not thread safe.
            ProviderFilter = (id) => this.EnabledProviders.Contains(id);
            KeywordFilter = (kw) => (this.EnabledKeywords.Value & kw) != 0;
            TimeFilter = (ticks) => ticks >= this.StartTime && ticks <= this.EndTime;
        }

        public bool IsRealtime { get; set; }

        public IObservable<EtwNativeEvent> GetRawEventsForTimeWindow()
        {
            Playback scope = new Playback();
            foreach (var item in this.files)
            {
                scope.AddEtlFiles(item);
            }

            return EtwObservable.FromFiles(this.files.ToArray());
        }

        internal override IObservable<EventRecordProxy> GetObservableEvents()
        {
            Playback playback = new Playback();

            if (this.IsRealtime)
            {
                playback.AddRealTimeSession(this.files.First());
            }
            else
            {
                foreach (var item in this.files)
                {
                    playback.AddEtlFiles(item);
                }
            }

            playback.KnownTypes = ManifestCompiler.GetKnowntypesforPlayback();

            return new EventRecordProxyObserver(playback, this);
        }

        internal override void StartPublish(IObservable<EventRecordProxy> observer)
        {
            ThreadPool.QueueUserWorkItem((s) =>
            {
                EventRecordProxyObserver o = observer as EventRecordProxyObserver;
                try
                {
                    o.Start();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                    throw;
                }

            });
        }

        private IObservable<SystemEvent> ApplyFilters(IObservable<SystemEvent> events)
        {
            if (this.ShouldFilterTimeWindow)
            {
                events = events.Where((s) => TimeFilter(s.Header.Timestamp.Ticks));
            }

            if (this.ShouldFilterProviders)
            {                
                events = events.Where((s) => ProviderFilter(s.Header.ProviderId));
            }

            if (this.ShouldFilterKeywords)
            {
                ulong mask = this.EnabledKeywords.Value;
                events = events.Where((s) => KeywordFilter(s.Header.Keywords & mask));
            }

            if (this.ShouldFilterByWhere)
            {
                if (FilterParser.MessageExpr.IsMatch(this.WhereFilter))
                {
                    var whereMsgFunc = FilterParser.ParseWhere<SystemEventWithMessage>(this.WhereFilter);
                    events = events.Where((s)=> whereMsgFunc(new SystemEventWithMessage(ref s)));
                }
                else
                {
                    var whereMsgFunc = FilterParser.ParseWhere<SystemEvent>(this.WhereFilter);
                    events = events.Where((s) => whereMsgFunc(s));
                }
            }

            return events;
        }

        internal void Stop(IObservable<EventRecordProxy> t)
        {
            Contract.Assert(t is EventRecordProxyObserver);
            EventRecordProxyObserver observer = (EventRecordProxyObserver)t;
            observer.Dispose();
        }

        public override void Dispose()
        {

        }

        class EventRecordProxyObserver : IObservable<EventRecordProxy>
        {
            Playback playback;
            TxReader reader;
            IDisposable subscriber;
            IObserver<EventRecordProxy> observer;
            bool disposed = false;

            public EventRecordProxyObserver(Playback playback, TxReader reader)
            {
                this.reader = reader;
                this.playback = playback;
            }

            object ThisLock { get { return this.reader; } }

            public IDisposable Subscribe(IObserver<EventRecordProxy> observer)
            {
                IObservable<SystemEvent> events = playback.GetObservable<SystemEvent>();
                Contract.Assert(this.observer == null, "Multiple subscriptions not supported");
                this.observer = observer;

                //Apply filters on the reader
                events = reader.ApplyFilters(events);
                var q = from e in events
                        select new EventRecordProxy(ref e);

                this.subscriber = q.Subscribe(observer);
                return this.subscriber;
            }

            public void Start()
            {
                this.playback.Start();
            }

            internal void Dispose()
            {
                lock (this.ThisLock)
                {
                    if (disposed)
                    {
                        return;
                    }

                    disposed = true;
                }

                if (this.observer != null)
                {
                    this.observer.OnCompleted();
                    this.observer = null;
                }

                if (this.subscriber != null)
                {
                    this.subscriber.Dispose();
                    this.subscriber = null;
                }
            }
        }



        internal IDictionary<Type, long> GetStats()
        {
            var stat = new TypeOccurenceStatistics(ManifestCompiler.GetKnowntypesforPlayback());
            foreach (var item in this.files)
            {
                stat.AddEtlFiles(item);
            }
            
            stat.Run();
            return stat.Statistics;

        }
    }
}
