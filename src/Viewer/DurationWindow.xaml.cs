namespace EtlViewer
{
    using EtlViewer.QueryFx;
    using EtlViewer.Viewer.Models;
    using EtlViewer.Viewer.UIUtils;
    using EtlViewerQuery;
    using System;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for DurationWindows.xaml
    /// </summary>
    partial class DurationWindow : Window
    {
        string filename = string.Empty;
        string startEventName;
        string stopEventName;
        CancellationTokenSource drillDownCTS;
        CancellationTokenSource idQueryCTS;

        public DurationWindow()
        {
            InitializeComponent();
            SystemCommandHandler.Bind(this);
            this.txtDrillDown.Text = @"
// You can hit F5 in these query windows to execute your queries after you make any changes. 
// You should dump a duration item at the end of your query since the drill down query depends on this.";

            DataGridUtilities.SetDynamicItemSource(this.gridDetails,
                                "Drill down activities will be displayed here.");
        }

        internal QueryModel Model
        {
            get { return this.DataContext as QueryModel; }
            set { this.DataContext = value; }
        }

        internal void ShowDuration(Resolver resolver,
                                    long startTime,
                                    long stopTime,
                                    string startEvent,
                                    string stopEvent)
        {
            this.Title = string.Format("Duration Between {0} & {1} joined on ActivityId", startEvent, stopEvent);

            this.startEventName = startEvent.Split(' ')[0];
            this.stopEventName = stopEvent.Split(' ')[0];
            StringDictionary fields = new StringDictionary();
            fields.Add("startEvent", startEventName);
            fields.Add("stopEvent", stopEventName);
            fields.Add("startTime", new DateTime(startTime).ToString());
            fields.Add("stopTime", new DateTime(stopTime).ToString());
            string query = Summary.GetDurationQuery(fields);
            this.txtDurationQuery.Text = query;
            this.Show();
        }

        #region Drill Down
        private void gridEvents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) &&
                !(dep is DataGridCell) &&
                !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            if (dep is DataGridColumnHeader)
            {
                DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
                // do something
            }

            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;

                // navigate further up the tree
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                DataGridRow row = dep as DataGridRow;
                if (row != null)
                {
                    DurationItem item = row.Item as DurationItem;
                    if (item != null)
                    {

                        string query = Summary.GetSlowRequestQuery(
                                                        this.Model.StartTime,
                                                        this.Model.StopTime,
                                                        startEventName,
                                                        stopEventName,
                                                        item.Duration.ToString());
                        this.txtDrillDown.Text = query;
                        this.gridDetails.Visibility = System.Windows.Visibility.Visible;
                        this.txtDrillDown.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }

        #endregion

        private void txtDurationQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (this.drillDownCTS != null)
                {
                    drillDownCTS.Cancel();
                }

                drillDownCTS = new CancellationTokenSource();

                string query = this.txtDurationQuery.Text;
                QueryWindowHelpers.ExecuteQueryAsync(this.Model,
                    query,
                    this.Model.GetPlayback(),
                    drillDownCTS,
                    (type, result) =>
                    {
                        this.gridDuration.ItemsSource = result;
                        Action completion = () =>
                        {
                            bool hasX, hasY;
                            if (QueryWindowHelpers.CanShowChart(type, out hasX, out hasY))
                            {
                                QueryWindowHelpers.PopulateChart(this.histogramChart, hasX, type, result);
                            }
                            else
                            {
                                this.histogramChart.Visibility = Visibility.Collapsed;
                            }
                        };

                        return completion;
                    });
                this.Show();
            }
        }

        private void txtDrillDown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (this.idQueryCTS != null)
                {
                    this.idQueryCTS.Cancel();
                }
                this.idQueryCTS = new CancellationTokenSource();

                string query = this.txtDrillDown.Text;
                QueryWindowHelpers.ExecuteQueryAsync(this.Model,
                    query,
                    this.Model.GetPlayback(),
                    this.idQueryCTS,
                    (type, result) => { this.gridDetails.ItemsSource = result; return null; });
                this.Show();
            }
        }
    }
}
