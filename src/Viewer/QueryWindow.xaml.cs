namespace EtlViewer.Viewer
{
    using EtlViewer.Viewer.UIUtils;
    using EtlViewer.Viewer.Views;
    using EtlViewerQuery;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    
    /// <summary>
    /// Interaction logic for QueryWindow.xaml
    /// </summary>
    partial class QueryWindow : Window
    {
        CancellationTokenSource queryCancelationToken;
        internal QueryEditorView View
        {
            get { return this.DataContext as QueryEditorView; }
            set { this.DataContext = value; }
        }
        SynchronizationContext context;
        public Task queryExecutionTask;
        public QueryWindow()
        {
            InitializeComponent();
            this.context = SynchronizationContext.Current;
            SystemCommandHandler.Bind(this);
        }

        internal QueryWindow(QueryEditorView view)
            : this()
        {
            this.View = view;
            view.QueryModel.RunCommand.CanExecuteTargets += () => !view.QueryModel.IsBusy;
            view.QueryModel.RunCommand.ExecuteTargets += (o) => ExecuteRunQuery(o);
            view.ExitCommand.CanExecuteTargets += () => true;
            view.ExitCommand.ExecuteTargets += (o) =>
            {
                this.Close();
            };            
        }

        private static void RenderDiagram(SequenceDiagram model, Controls.SequenceDiagram diagram)
        {
            diagram.Reset();
            diagram.Title = model.Title;
            diagram.SequenceObjects = new System.Collections.ObjectModel.ObservableCollection<SequenceItem>();            
            foreach (var item in model.SequenceSteps)
            {
                diagram.SequenceObjects.Add(item.Value);
            }
            //Diagram needs these to build connectors.
            diagram.UpdateLayout();
            foreach (var item in model.Connectors)
            {
                diagram.AddConnector(item.Item1, item.Item2, item.Item3);
            }
            diagram.UpdateLayout();           
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetQuery();
            }
        }

        private void ExecuteRunQuery(object param)
        {
            ResetQuery();
            this.ResultPanel.Children.Clear();
            this.ResultPanel.RowDefinitions.Clear();
            this.ChartPane.Children.Clear();
            this.ChartPane.RowDefinitions.Clear();
            Playback scope = this.View.QueryModel.GetPlayback();
            StringWriter errors = new StringWriter();
            StringWriter output = new StringWriter();
            this.View.QueryModel.Status = "Executing Query";
            QueryWindowHelpers.ExecuteQueryAsync(this.View.QueryModel,
                this.View.EditorModel.QueryString,
                scope,
                this.queryCancelationToken,
                (type, result) =>
                {
                    return this.OnStartQuery(type, result);                    
                });
        }

        private Action OnStartQuery(Type type, IEnumerable result)
        {
            Controls.SequenceDiagram diagram = null;
            if (type == typeof(SequenceDiagram))
            {
                diagram = new Controls.SequenceDiagram();
                this.ResultPanel.RowDefinitions.Add(new RowDefinition());
                diagram.SetValue(Grid.RowProperty, this.ResultPanel.RowDefinitions.Count - 1);
                this.ResultPanel.Children.Add(diagram);
            }
            else
            {
                this.AddResultGrid(result);
            }

            bool hasX, hasY;
            Action completion = () =>
            {
                if (type == typeof(SequenceDiagram))
                {
                    foreach (var item in result)
                    {
                        RenderDiagram(item as SequenceDiagram, diagram);
                        break;
                    }
                }
                else
                {
                    if (QueryWindowHelpers.CanShowChart(type, out hasX, out hasY))
                    {
                        var chart = this.AddChart();
                        QueryWindowHelpers.PopulateChart(chart, hasX, type, result);
                    }
                }
            };
            return completion;
        }

        private Controls.DurationHistogram AddChart()
        {
            var chart = new Controls.DurationHistogram();
            this.ChartPane.RowDefinitions.Add(new RowDefinition());
            chart.SetValue(Grid.RowProperty, this.ChartPane.RowDefinitions.Count - 1);
            this.ChartPane.Children.Add(chart);
            return chart;
        }

        void AddResultGrid(IEnumerable items)
        {
            DataGrid grid = new DataGrid();
            grid.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            grid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            grid.SelectionUnit = DataGridSelectionUnit.Cell;
            grid.Margin = new Thickness(10);
            grid.HorizontalGridLinesBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightBlue);
            grid.VerticalGridLinesBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightBlue);
            grid.ItemsSource = items;
            grid.BorderThickness = new Thickness(0);
            grid.IsReadOnly = true;
            grid.CanUserSortColumns = false;
            grid.IsSynchronizedWithCurrentItem = true;
            grid.EnableColumnVirtualization = true;
            grid.SelectionMode = DataGridSelectionMode.Extended;
            grid.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, true);
            grid.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            ContextMenu menu = new ContextMenu();
            MenuItem menuitem = new MenuItem();
            menuitem.Header = "Copy with headers";
            menuitem.Command = new CopyWithHeadersCommand();
            menuitem.CommandParameter = grid;
            menu.Items.Add(menuitem);
            grid.ContextMenu = menu;

            this.ResultPanel.RowDefinitions.Add(new RowDefinition());
            grid.SetValue(Grid.RowProperty, this.ResultPanel.RowDefinitions.Count - 1);
            this.ResultPanel.Children.Add(grid);
        }

        private void ResetQuery()
        {
            if (this.queryCancelationToken != null)
            {
                this.queryCancelationToken.Cancel();
            }
            queryCancelationToken = new CancellationTokenSource();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ResetQuery();
            this.View.Close(e);
        }
    }

    class QueryWindows
    {
        static Dictionary<QueryEditorView, QueryWindow> windows = new Dictionary<QueryEditorView, QueryWindow>();

        internal static void Create(QueryEditorView view, bool show)
        {
            CreateNewWindow(view, show);
        }

        internal static void Remove(QueryEditorView model)
        {
            windows.Remove(model);
        }

        internal static void Activate(QueryEditorView view)
        {
            if (windows.ContainsKey(view))
            {
                Window w = windows[view];
                w.Show();
                w.Activate();
            }
        }

        private static void CreateNewWindow(
                                QueryEditorView view,
                                bool showWindow)
        {
            QueryWindow window = new QueryWindow(view);
            windows[view] = window; //Add to the window collection.    
            if (showWindow)
            {
                window.Show();
            }
        }

        internal static bool Activate(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            var model = windows.Keys.Where((e) => String.Compare(e.EditorModel.FileName, fileName, true) == 0).FirstOrDefault();
            if (model != null)
            {
                Activate(model);
                return true;
            }

            return false;
        }

        internal static int Count()
        {
            return windows.Count;
        }
    }


}
