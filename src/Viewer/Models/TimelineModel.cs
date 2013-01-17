namespace EtlViewer.Viewer.Models
{
    using EtlViewer.Viewer.Controls;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;

    class TimelineModel : DependencyObject, INotifyPropertyChanged
    {
        MainModel mainModel;

        public event Action<IList<TimelineEvent>> AddData;

        public long StartTime { get; set; }
        public long StopTime { get; set; }

        public const string SelectionStartPropertyName = "SelectionStart";
        public long SelectionStart
        {
            get { return (long)this.GetValue(SelectionStartProperty); }
            set { this.SetValue(SelectionStartProperty, value); }
        }

        public static readonly DependencyProperty SelectionStartProperty = DependencyProperty.Register(
          SelectionStartPropertyName,
          typeof(long),
          typeof(TimelineModel),
          new PropertyMetadata(long.MinValue, OnPropertyChanged));

        public const string SelectionEndPropertyName = "SelectionEnd";
        public long SelectionEnd
        {
            get { return (long)this.GetValue(SelectionEndProperty); }
            set { this.SetValue(SelectionEndProperty, value); }
        }

        public static readonly DependencyProperty SelectionEndProperty = DependencyProperty.Register(
          SelectionEndPropertyName,
          typeof(long),
          typeof(TimelineModel),
          new PropertyMetadata(long.MaxValue, OnPropertyChanged));

        public TimelineModel(MainModel model)
        {
            this.mainModel = model;
            model.PropertyChanged += model_PropertyChanged;
        }

        void model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MainModel.StartTimePropertyName)
            {
                if (this.mainModel.StartTime != long.MinValue && this.SelectionStart != this.mainModel.StartTime)
                {
                    this.SelectionStart = this.mainModel.StartTime;
                }
            }
            else if (e.PropertyName == MainModel.StopTimePropertyName && this.SelectionEnd != this.mainModel.StopTime)
            {
                if (this.mainModel.StopTime != long.MaxValue)
                {
                    this.SelectionEnd = this.mainModel.StopTime;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TimelineModel model = sender as TimelineModel;
            string propertyName = e.Property.Name;
            OnPropertyChanged(model, propertyName);
        }

        private static void OnPropertyChanged(TimelineModel model, string propertyName)
        {
            if (propertyName == TimelineModel.SelectionStartPropertyName && model.mainModel.StartTime != model.SelectionStart)
            {
                model.mainModel.StartTime = model.SelectionStart;
            }
            else if (propertyName == TimelineModel.SelectionEndPropertyName && model.mainModel.StopTime != model.SelectionEnd)
            {
                model.mainModel.StopTime = model.SelectionEnd;
            }

            if (model.PropertyChanged != null)
            {
                model.PropertyChanged(model, new PropertyChangedEventArgs(propertyName));
            }
        }

        internal void SelectView()
        {
            this.SelectionStart = this.StartTime;
            this.SelectionEnd = this.StopTime;

            //Propogate the change to the main model as well
            this.mainModel.StartTime = this.StartTime;
            this.mainModel.StopTime = this.StopTime;
        }

        internal void Reset()
        {
            this.StartTime = DateTime.MinValue.Ticks;
            this.StopTime = DateTime.MaxValue.Ticks;
        }

        internal void Populate(IList<Controls.TimelineEvent> data)
        {
            if (this.StartTime > data[0].Ticks || this.StartTime == DateTime.MinValue.Ticks)
            {
                this.StartTime = data[0].Ticks;
            }

            if (this.StopTime < data[data.Count - 1].Ticks || this.StopTime == DateTime.MaxValue.Ticks)
            {
                this.StopTime = data[data.Count - 1].Ticks;
            }

            this.AddData(data);

        }
    }
}
