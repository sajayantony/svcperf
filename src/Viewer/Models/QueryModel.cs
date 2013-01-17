namespace EtlViewer.Viewer.Models
{
    using EtlViewer.QueryFx;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive;
    using System.Windows;

    class QueryModel : DependencyObject, INotifyPropertyChanged, IDisposable
    {
        public DelegateCommand<object> RunCommand { get; set; }

        MainModel MainModel { get; set; }
        public long StartTime { get; set; }
        public long StopTime { get; set; }

        public const string IsBusyPropertyName = "IsBusy";
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            IsBusyPropertyName,
            typeof(bool),
            typeof(QueryModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public bool IsBusy
        {
            get { return (bool)this.GetValue(IsBusyProperty); }
            set { this.SetValue(IsBusyProperty, value); }
        }

        public const string IsRealTimePropertyName = "IsRealTime";
        public static readonly DependencyProperty IsRealTimeProperty = DependencyProperty.Register(
          IsRealTimePropertyName,
          typeof(bool),
          typeof(QueryModel),
          new FrameworkPropertyMetadata(OnPropertyChanaged));

        public bool IsRealTime
        {
            get { return (bool)this.GetValue(IsRealTimeProperty); }
            set { this.SetValue(IsRealTimeProperty, value); }
        }

        const string StatusPropertyName = "Status";
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
            StatusPropertyName,
            typeof(string),
            typeof(QueryModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public string Status
        {
            get { return (string)this.GetValue(StatusProperty); }
            set { this.SetValue(StatusProperty, value); }
        }

        public const string ItemsSourcePropertyName = "ItemsSource";
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
          ItemsSourcePropertyName,
          typeof(IEnumerable),
          typeof(QueryModel),
          new FrameworkPropertyMetadata(OnPropertyChanaged));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)this.GetValue(ItemsSourceProperty); }
            set { this.SetValue(ItemsSourceProperty, value); }
        }

        static void OnPropertyChanaged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            QueryModel model = sender as QueryModel;
            string propertyName = e.Property.Name;
            model.OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal Playback GetPlayback()
        {
            Playback playback = TxHelper.GetCurrentEtlScope(this.Inputs, this.IsRealTime);
            if (playback != null)
            {
                playback.KnownTypes = ManifestCompiler.GetKnowntypesforPlayback();
            }
            return playback;
        }

        public List<string> Inputs { get; set; }
        public event EventHandler OnDisposed;

        public QueryModel(MainModel model)
        {
            this.RunCommand = new DelegateCommand<object>();
            this.MainModel = model;
            model.PropertyChanged += MainModelPropertyChanged;
            
            // Need to update the query models values from the main model
            MainModelPropertyChanged(model, new PropertyChangedEventArgs(MainModel.StartTimePropertyName));
            MainModelPropertyChanged(model, new PropertyChangedEventArgs(MainModel.StopTimePropertyName));
            MainModelPropertyChanged(model, new PropertyChangedEventArgs(MainModel.EtlFilesPropertyName));
            MainModelPropertyChanged(model, new PropertyChangedEventArgs(MainModel.SessionPropertyName));
            MainModelPropertyChanged(model, new PropertyChangedEventArgs(MainModel.IsRealTimePropertyName));
        }

        public void Dispose()
        {
            this.MainModel.PropertyChanged -= MainModelPropertyChanged;
            if (this.OnDisposed != null)
            {
                this.OnDisposed(this, EventArgs.Empty);
            }
        }

        void MainModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MainModel mainModel = (MainModel)sender;
            if (e.PropertyName == MainModel.EtlFilesPropertyName)
            {
                this.Inputs = mainModel.EtlFiles;
            }
            else if (e.PropertyName == MainModel.SessionPropertyName)
            {
                if (mainModel.Session != null & !String.IsNullOrEmpty(mainModel.Session))
                {
                    this.Inputs = mainModel.Session.Split(',').ToList();
                }
            }
            else if (e.PropertyName == MainModel.IsRealTimePropertyName)
            {
                this.IsRealTime = mainModel.IsRealTime;
            }
            else if (e.PropertyName == MainModel.StartTimePropertyName)
            {
                this.StartTime = mainModel.StartTime;
            }
            else if (e.PropertyName == MainModel.StopTimePropertyName)
            {
                this.StopTime = mainModel.StopTime;
            }
        }

        public bool HasChanged { get; set; }

        internal void LogActivity(string message)
        {
            Logger.Log(message);
            this.Status = message;
        }
    }
}
