namespace EtlViewer.Viewer.Controls
{
    using EtlViewer.Viewer.Models;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    
    /// <summary>
    /// Interaction logic for EventStatsGrid.xaml
    /// </summary>
    partial class EventStatsGrid : UserControl
    {
        private EventStatsModel Model;
        public LoadingAdorner loadingAdorner { get; set; }
        public EventStatsGrid()
        {
            InitializeComponent();
            this.DataContextChanged += StatsWindow_DataContextChanged;
            this.loadingAdorner = new LoadingAdorner(this.statsGrid, "Loading Stats");
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this.statsGrid);
            adornerLayer.Add(loadingAdorner);

        }

        void StatsWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is EventStatsModel)
            {
                this.Model = (EventStatsModel)e.NewValue;
            }
        }

        internal void LoadEvents()
        {
            if (this.statsGrid.ItemsSource != null && this.statsGrid.Items.Count > 0)
            {
                return;
            }

            var t = this.Model.GetStatsAsync();
            TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            t.ContinueWith((tresult) =>
            {
                IList<EventStat> stats = tresult.Result.Items;
                if (stats.Count > 0)
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(stats);
                    view.GroupDescriptions.Add(new PropertyGroupDescription("ProviderId"));
                    view.GroupDescriptions.Add(new PropertyGroupDescription("Level"));

                    this.statsGrid.ItemsSource = view;
                    loadingAdorner.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    loadingAdorner.Message = "No events found.\nConfirm manifests are loaded.";
                }

            }, scheduler);
        }

        private void statsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = DataGridUtil.GetDataGridRow(e.OriginalSource) as DataGridRow;
            if (row != null && row.Item is EventStat)
            {
                EventStat statItem = (EventStat)row.Item;
                if (this.Model.EventStatSelectedCommand.CanExecute(null))
                {
                    this.Model.EventStatSelectedCommand.Execute(statItem);
                }
            }
        }
    }
}
