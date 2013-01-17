namespace EtlViewer.Viewer.Models
{
    using EtlViewer.QueryFx;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Tx.Windows;
    
    class EventStatsModel : DependencyObject, INotifyPropertyChanged
    {
        public DelegateCommand<object> ViewStatsCommand { get; set; }
        public DelegateCommand<EventStat> EventStatSelectedCommand { get; set; }
        
        TxReader reader;        

        public EventStatsModel()
        {
            this.ViewStatsCommand = new DelegateCommand<object>();
            this.EventStatSelectedCommand = new DelegateCommand<EventStat>();
        }

        internal TxReader Reader
        {
            get
            {
                return this.reader;
            }
            set
            {
                this.reader = value;
                this.NotifyPropertyChanged("Reader");
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
        }

        internal Task<Stats> GetStatsAsync()
        {
            Task<Stats> t = new Task<Stats>(() =>
                {
                    IDictionary<Type, long> items = this.Reader.GetStats();
                    Stats stats = new Stats
                    {
                        Items = (from s in items
                                 let attr = (ManifestEventAttribute)s.Key.GetCustomAttributes(true).Where((e) => e is ManifestEventAttribute).FirstOrDefault()
                                 orderby s.Value descending
                                 select
                                 new EventStat()
                                 {
                                     EventName = s.Key.Name,
                                     Count = s.Value,
                                     Id = attr != null ? (uint?)attr.EventId : null,
                                     ProviderId = attr != null ? (Guid?)attr.ProviderGuid : null,
                                     Level = attr != null ? attr.Level : string.Empty,
                                 }).ToList()
                    };

                    return stats;
                });

            t.Start();
            return t;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    class Stats
    {
        public IList<EventStat> Items { get; set; }
    }

    struct EventStat
    {
        public string EventName { get; set; }
        public long Count { get; set; }
        public uint? Id { get; set; }
        public Guid? ProviderId { get; set; }
        public string Level { get; set; }
    }
}
