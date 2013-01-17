namespace EtlViewer.Viewer.Models
{
    using EtlViewer.QueryFx;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;
    
    class EventsModel : DependencyObject, INotifyPropertyChanged
    {
        Predicate<object> TrueFilter = (e) => true;

        public const string HighlightTextPropertyName = "HighlightText";
        public static readonly DependencyProperty HighlightTextProperty = DependencyProperty.Register(
            HighlightTextPropertyName,
            typeof(string),
            typeof(EventsModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public string HighlightText
        {
            get { return (string)this.GetValue(HighlightTextProperty); }
            set { this.SetValue(HighlightTextProperty, value); }
        }

        const string CurrentRowPropertyName = "CurrentRow";
        public static readonly DependencyProperty CurrentRowProperty = DependencyProperty.Register(
            CurrentRowPropertyName,
            typeof(string),
            typeof(EventsModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public string CurrentRow
        {
            get { return (string)this.GetValue(CurrentRowProperty); }
            set { this.SetValue(CurrentRowProperty, value); }
        }


        const string RowCountPropertyName = "RowCount";
        public static readonly DependencyProperty RowCountProperty = DependencyProperty.Register(
            RowCountPropertyName,
            typeof(long),
            typeof(EventsModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public long RowCount
        {
            get { return (long)this.GetValue(RowCountProperty); }
            set { this.SetValue(RowCountProperty, value); }
        }

        const string FilteredRowCountPropertyName = "FilteredRowCount";
        public static readonly DependencyProperty FilteredRowCountProperty = DependencyProperty.Register(
            FilteredRowCountPropertyName,
            typeof(long),
            typeof(EventsModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public long FilteredRowCount
        {
            get { return (long)this.GetValue(FilteredRowCountProperty); }
            set { this.SetValue(FilteredRowCountProperty, value); }
        }

        const string SourceFilterPropertyName = "SourceFilter";
        public static readonly DependencyProperty SourceFilterProperty = DependencyProperty.Register(
            SourceFilterPropertyName,
            typeof(string),
            typeof(EventsModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public string SourceFilter
        {
            get { return (string)this.GetValue(SourceFilterProperty); }
            set { this.SetValue(SourceFilterProperty, value); }
        }

        const string ViewFilterPropertyName = "ViewFilter";
        public static readonly DependencyProperty ViewFilterProperty = DependencyProperty.Register(
            ViewFilterPropertyName,
            typeof(string),
            typeof(EventsModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public string ViewFilter
        {
            get { return (string)this.GetValue(ViewFilterProperty); }
            set { this.SetValue(ViewFilterProperty, value); }
        }

        const string SelectedTimeWindowPropertyName = "SelectedTimeWindow";
        public static readonly DependencyProperty SelectedTimeWindowProperty = DependencyProperty.Register(
            SelectedTimeWindowPropertyName,
            typeof(TimeSpan),
            typeof(EventsModel),
            new FrameworkPropertyMetadata(TimeSpan.MinValue, OnPropertyChanaged));

        public TimeSpan SelectedTimeWindow
        {
            get { return (TimeSpan)this.GetValue(SelectedTimeWindowProperty); }
            set { this.SetValue(SelectedTimeWindowProperty, value); }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        static void OnPropertyChanaged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            EventsModel model = sender as EventsModel;
            string propertyName = e.Property.Name;
            model.NotifyPropertyChaned(propertyName);
        }

        private void NotifyPropertyChaned(string propertyName)
        {
            if (propertyName == ItemsPropertyName)
            {
                this.CurrenView = CollectionViewSource.GetDefaultView(this.Items);
                this.CurrenView.Filter = TrueFilter;
                this.RowCount = Items.Count;
                this.FilteredRowCount = Items.Count;
                this.HasEvents = true;
            }

            if (PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// We might be able to avoid keeping this as a dependecy property
        /// as its accesse a lot on 
        /// </summary>
        internal ICollectionView CurrenView { get; set; }

        public bool HasEvents { get; set; }

        Func<EventRecordProxy, bool> GetViewFilter(TxReader reader)
        {
            Func<EventRecordProxy, bool> where = null;

            if (reader.ShouldFilterByWhere)
            {
                where = FilterParser.ParseWhere<EventRecordProxy>(reader.WhereFilter);
            }

            Func<EventRecordProxy, bool> func = (e) =>
            {
                if (reader.ShouldFilterTimeWindow && !reader.TimeFilter(e.TimeStamp.Ticks))
                {
                    return false;
                }

                if (reader.ShouldFilterKeywords && !reader.KeywordFilter(e.Keywords))
                {
                    return false;
                }

                if (reader.ShouldFilterProviders && !reader.ProviderFilter(e.ProviderId))
                {
                    return false;
                }

                if (reader.ShouldFilterByWhere && !where(e))
                {
                    return false;
                }

                return true;
            };

            return func;
        }

        public const string ItemsPropertyName = "Item";
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
          ItemsPropertyName,
          typeof(IList<EventRecordProxy>),
          typeof(EventsModel),
          new FrameworkPropertyMetadata(OnPropertyChanaged));
        public IList<EventRecordProxy> Items
        {
            get { return (IList<EventRecordProxy>)this.GetValue(ItemsProperty); }
            set { this.SetValue(ItemsProperty, value); }
        }

        internal void Filter(TxReader reader)
        {
            var func = this.GetViewFilter(reader);

            int count = 0;
            if (func == null)
            {
                this.CurrenView.Filter = TrueFilter;
            }
            else
            {
                EventRecordProxy previous = null;
                this.CurrenView.Filter = (e) =>
                {
                    EventRecordProxy curr = (EventRecordProxy)e;
                    if (func(curr))
                    {
                        curr.Previous = previous;
                        previous = curr;
                        count++;
                        return true;
                    }

                    return false;
                };
            }

            this.FilteredRowCount = count;
        }

        internal void Select(EventRecordProxy eventRecordProxy)
        {
            if (eventRecordProxy != null)
            {
                this.CurrenView.MoveCurrentTo(eventRecordProxy);
                this.CurrentRow = this.CurrenView.CurrentPosition.ToString();
            }
        }

        public string SearchText { get; set; }


        public IEnumerator<int> SearchEnumerator { get; set; }
    }
}
