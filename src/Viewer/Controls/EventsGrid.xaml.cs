namespace EtlViewer.Viewer.Controls
{
    using EtlViewer.Viewer.Models;
    using EtlViewer.Viewer.UIUtils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for EventsGrid.xaml
    /// </summary>
    partial class EventsGrid : UserControl
    {
        string[] defaultColumns = new String[]{
                        "Id",
                        "Task",
                        "Opcode",
                        "Symbol",
                        "TimeStamp",
                        "ActivityId",
                        "RelatedActivityId",
                        "Message",
                        "Pid",
                        "Tid",
                        "TimeFromLastEvent",
        };

        ObservableCollection<CheckedItem<Column>> columns;
        public event Action<string, EventRecordProxy> OnCellCopy;
        public event Action<TimeSpan, TimeSpan> OnSelectInterval;
        public event Action<TimeSpan, TimeSpan> OnRangeSelected;
        private Dictionary<string, DataGridColumn> GridColumns;

        EventDetails detailsWindow;

        EventsModel Model { get { return this.DataContext as EventsModel; } }

        public EventsGrid()
        {
            InitializeComponent();
            this.InitializeDefaultColumns();
            this.InitializeColumnsPopup();
            this.gridEvents.Loaded += gridEvents_Loaded;
            this.DataContextChanged += EventsGrid_DataContextChanged;
        }

        void EventsGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.NewValue is EventsModel)
            {
                EventsModel model = (EventsModel)e.NewValue;
                model.PropertyChanged += model_PropertyChanged;
            }
        }

        void model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == EventsModel.ItemsPropertyName)
            {
                this.Model.HasEvents = true;
                this.gridEvents.DataContext = this.Model.CurrenView;
            }
        }

        private void gridEvents_Loaded(object sender, RoutedEventArgs e)
        {
            // Hide the select all button to avoid users from select the entire grid, 
            var dataGrid = (DataGrid)sender;
            HideSelectAllButton(dataGrid);
            this.GridColumns = new Dictionary<string, DataGridColumn>();
            foreach (var column in dataGrid.Columns)
            {
                this.GridColumns.Add(DataGridUtil.GetName(column), column);
            }
            this.detailsWindow = new EventDetails();
            this.detailsWindow.Closing += (s, args) =>
            {
                this.detailsWindow.Hide();
                args.Cancel = true;
            };
        }

        private static void HideSelectAllButton(DataGrid dataGrid)
        {
            if (dataGrid != null && VisualTreeHelper.GetChildrenCount(dataGrid) > 0)
            {
                var border = VisualTreeHelper.GetChild(dataGrid, 0) as Border;
                if (border != null && VisualTreeHelper.GetChildrenCount(border) > 0)
                {
                    ScrollViewer scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                    if (scrollViewer != null && VisualTreeHelper.GetChildrenCount(scrollViewer) > 0
                        && VisualTreeHelper.GetChildrenCount(scrollViewer) > 0)
                    {
                        var grid = (Grid)VisualTreeHelper.GetChild(scrollViewer, 0);
                        if (VisualTreeHelper.GetChildrenCount(grid) > 0)
                        {
                            var button = VisualTreeHelper.GetChild(grid, 0) as Button;
                            if (button != null)
                            {
                                button.IsEnabled = false;
                            }
                        }
                    }
                }
            }
        }

        private void InitializeDefaultColumns()
        {
            foreach (var column in this.gridEvents.Columns)
            {
                column.Visibility = System.Windows.Visibility.Hidden;
            }

            foreach (var name in this.defaultColumns)
            {
                foreach (var column in this.gridEvents.Columns.Where((c) => DataGridUtil.GetName(c).ToString() == name))
                {
                    column.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void InitializeColumnsPopup()
        {
            List<CheckedItem<Column>> checkboxes = (from p in this.gridEvents.Columns
                                                    let name = DataGridUtil.GetName(p)
                                                    select new CheckedItem<Column>
                                                    {
                                                        Item = new Column() { Name = name },
                                                        Name = name,
                                                        IsChecked = p.Visibility == System.Windows.Visibility.Visible
                                                    }).ToList();

            this.columns = new ObservableCollection<CheckedItem<Column>>(checkboxes);
            this.ColumnsFilter.ItemsSource = this.columns;
        }

        private void gridEvents_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            bool exclude = false;
            bool addFilter = false;

            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0 &&
                (Keyboard.Modifiers & ModifierKeys.Shift) > 0 &&
                (Keyboard.IsKeyDown(Key.C) || Keyboard.IsKeyDown(Key.E)))
            {
                addFilter = true;
                exclude = Keyboard.IsKeyDown(Key.E);
            }

            if (addFilter)
            {
                DataGrid grid = sender as DataGrid;
                if (grid == null)
                {
                    return;
                }

                DependencyObject dep = e.OriginalSource as DependencyObject;
                DataGridCell cell;
                DataGridRow row;
                EventRecordProxy proxy;
                dep = GetCellRow(dep, out cell, out row);

                if (cell != null && row != null)
                {
                    TextBlock txtBlock = cell.Content as TextBlock;
                    if (txtBlock == null)
                    {
                        return;
                    }

                    proxy = row.Item as EventRecordProxy;
                    if (proxy == null)
                    {
                        return;
                    }
                    string op = exclude ? "!=" : "=";
                    string filter = null;
                    string value = txtBlock.Text;
                    string columnName = DataGridUtil.GetName(cell.Column);
                    switch (columnName)
                    {
                        case "Id":
                        case "Level":
                        case "Pid":
                        case "Tid":
                            filter = string.Format("{0}{1}{2}", columnName, op, value);
                            break;
                        case "ActivityId":
                        case "RelatedActivityId":
                            if (exclude)
                            {
                                filter = string.Format("(ActivityId!={0} and RelatedActivityId!={0})", value);
                            }
                            else
                            {
                                filter = string.Format("(ActivityId={0} OR RelatedActivityId={0})", value);
                            }
                            break;
                        case "Symbol":
                            filter = "Id=" + proxy.Id;
                            break;
                        case "TimeStamp":
                            filter = "TimeStamp=DateTime.Parse(\"" + proxy.TimeStamp.ToString() + "\")";
                            break;
                        case "Message":
                            filter = string.Format("Message.Contains(\"{0}\")", value);
                            if (exclude)
                            {
                                filter = string.Format("!({0})", filter);
                            }
                            break;
                    }



                    if (this.OnCellCopy != null && filter != null)
                    {
                        this.OnCellCopy(filter, proxy);
                    }
                }
            }
        }

        void CopyingCellClipboardContent_TextFilter(object sender, DataGridCellClipboardEventArgs e)
        {
            //TODO: Handle any special cell copy activity.
        }

        void gridEvents_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Guid activity = Guid.Empty;
            DateTime? max;
            DateTime? min;
            DataGrid grid = sender as DataGrid;
            if (grid == null)
            {
                return;
            }

            DependencyObject dep = e.OriginalSource as DependencyObject;
            DataGridCell cell;
            DataGridRow row;
            dep = GetCellRow(dep, out cell, out row);

            if (cell != null && row != null)
            {
                EventRecordProxy record = row.Item as EventRecordProxy;
                if (record == null)
                {
                    return;
                }

                if (cell.Column.Header.ToString() == "ActivityId")
                {
                    activity = record.ActivityId;
                }
                else if (cell.Column.Header.ToString() == "RelatedActivityId")
                {
                    activity = record.RelatedActivityId;
                }
            }

            if (this.gridEvents.SelectedCells.Count > 1)
            {
                if (GetSelectedInterval(out max, out min, grid))
                {
                    if (this.OnRangeSelected != null)
                    {
                        this.OnRangeSelected(new TimeSpan(min.Value.Ticks), new TimeSpan(max.Value.Ticks));
                    }
                    this.Model.SelectedTimeWindow = new TimeSpan(max.Value.Subtract(min.Value).Ticks);
                }
            }
            else
            {
                this.Model.SelectedTimeWindow = TimeSpan.MinValue;
            }

            if (activity != Guid.Empty)
            {
                this.Model.HighlightText = activity.ToString();
            }
        }

        private static DependencyObject GetCellRow(DependencyObject dep, out DataGridCell cell, out DataGridRow row)
        {
            cell = null;
            row = null;
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridCell)
            {
                cell = dep as DataGridCell;
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                if (dep is DataGridRow)
                {
                    row = dep as DataGridRow;
                }
            }
            return dep;
        }

        private static bool GetSelectedInterval(out DateTime? max, out DateTime? min, DataGrid grid)
        {
            max = DateTime.MinValue;
            min = DateTime.MaxValue;
            int count = 0;
            foreach (var item in grid.SelectedCells)
            {
                var record = item.Item as EventRecordProxy;
                EventRecordProxy evt = record != null ? record : null;
                if (evt == null)
                {
                    continue;
                }
                if (evt.TimeStamp != null && evt.TimeStamp > max)
                {
                    max = evt.TimeStamp;
                }

                if (evt.TimeStamp != null && evt.TimeStamp < min)
                {
                    min = evt.TimeStamp;
                }
                count++;
            }

            if (count == 0)
            {
                max = DateTime.MinValue;
                min = DateTime.MaxValue;
            }

            return count != 0;
        }

        private void gridEvents_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        void gridEvents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object source = e.OriginalSource;
            DependencyObject dep = DataGridUtil.GetDataGridRow(source);
            DataGridRow row = dep as DataGridRow;
            if (row != null)
            {                
                EventRecordProxy item = row.Item as EventRecordProxy;
                if (item != null)
                {
                    this.detailsWindow.Title = "EVENT DETAILS ROW " + row.Header;
                    this.detailsWindow.DataContext = item.Details.ToString();
                    if (!this.detailsWindow.IsVisible || !this.detailsWindow.IsActive)
                    {
                        this.detailsWindow.Show();
                        this.detailsWindow.Activate();
                    }
                }
            }
        }

        public IEnumerable<int> FindTextInEventsTable(string pattern)
        {
            if (String.IsNullOrEmpty(pattern) || this.gridEvents.Items == null || this.gridEvents.Items.Count == 0)
            {
                this.Model.HighlightText = string.Empty;
                yield return -1;
            }

            int startIndex = 0;
            if (this.Model.CurrenView.CurrentPosition >= 0)
            {
                startIndex = this.Model.CurrenView.CurrentPosition;
            }

            this.Model.HighlightText = pattern;
            for (int i = startIndex; i < this.gridEvents.Items.Count; i++)
            {
                EventRecordProxy proxy = this.gridEvents.Items[i] as EventRecordProxy;

                foreach (var column in this.SearchRow(proxy, pattern))
                {
                    this.Model.CurrentRow = i.ToString();
                    this.gridEvents.SelectedCells.Clear();
                    DataGridCellInfo cellInfo = new DataGridCellInfo(proxy, column);
                    this.gridEvents.SelectedCells.Add(cellInfo);
                    this.gridEvents.ScrollIntoView(proxy);
                    this.Model.CurrenView.MoveCurrentTo(proxy);
                    yield return i;

                }
            }

            if (this.gridEvents.Items.Count > 0)
            {
                //this.gridEvents.SelectedItem = this.gridEvents.Items[0];
                //this.gridEvents.ScrollIntoView(this.gridEvents.Items[0]);
                //this.Model.CurrentRow = "0";
            }

            yield return -1;
        }

        IEnumerable<DataGridColumn> SearchRow(EventRecordProxy proxy, string pattern)
        {
            if (proxy.Id.ToString().IndexOf(pattern, 0, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                yield return GridColumns["Id"];
            }

            if (GuidToStringConverter.ToString(proxy.ActivityId).IndexOf(pattern, 0, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                yield return GridColumns["ActivityId"];
            }

            if (GuidToStringConverter.ToString(proxy.RelatedActivityId).IndexOf(pattern, 0, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                yield return GridColumns["RelatedActivityId"];
            }

            if (proxy.Symbol.IndexOf(pattern, 0, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                yield return GridColumns["Symbol"];
            }

            if (proxy.Message.IndexOf(pattern, 0, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                yield return GridColumns["Message"];
            }
        }

        void MenuSelectInterval_Click(object sender, RoutedEventArgs e)
        {
            DateTime? min, max;
            bool success = GetSelectedInterval(out max, out min, this.gridEvents);

            if (success)
            {
                if (this.OnSelectInterval != null)
                {
                    this.OnSelectInterval(new TimeSpan(min.Value.Ticks), new TimeSpan(max.Value.Ticks));
                }
            }
        }

        private void MenuChooseColumns_Click(object sender, RoutedEventArgs e)
        {
            this.ColumnChooser.IsOpen = true;
        }

        private void MenuShowRowNumbers_Click(object sender, RoutedEventArgs e)
        {
            if (this.gridEvents.HeadersVisibility == DataGridHeadersVisibility.Column)
            {
                this.gridEvents.HeadersVisibility = DataGridHeadersVisibility.All;
            }
            else
            {
                this.gridEvents.HeadersVisibility = DataGridHeadersVisibility.Column;
            }
        }

        private void BtnColumsChanged_Click(object sender, RoutedEventArgs e)
        {
            this.ColumnChooser.IsOpen = false;

            foreach (var checkboxItem in this.columns)
            {
                foreach (var column in this.gridEvents.Columns.Where((c) => DataGridUtil.GetName(c).ToString() == checkboxItem.Name))
                {
                    column.Visibility = checkboxItem.IsChecked == true ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                }
            }
        }

        class Column
        {
            public string Name { get; set; }
        }

        private void gridEvents_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (e.AddedCells.Count > 0)
            {
                this.Model.Select(e.AddedCells[0].Item as EventRecordProxy);
            }
        }

        private void gridEvents_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            // Make sure we preserve the grid format when the user copies to the clipboard
            for (int i = 0; i < e.ClipboardRowContent.Count; ++i)
            {
                var clipboardItem = e.ClipboardRowContent[i];
                string header = clipboardItem.Column.Header as string;

                if (header != null)
                {
                    if (header == "TimeStamp" && clipboardItem.Content is DateTime)
                    {
                        DateTime content = (DateTime)clipboardItem.Content;
                        e.ClipboardRowContent[i] = new DataGridClipboardCellContent(clipboardItem.Item, clipboardItem.Column, content.ToString("0:HH:mm:ss.ffffff"));
                    }
                    else if (header == "Delta (ms)" && clipboardItem.Content is double)
                    {
                        double content = (double)clipboardItem.Content;
                        e.ClipboardRowContent[i] = new DataGridClipboardCellContent(clipboardItem.Item, clipboardItem.Column, string.Format("{0:0}", content));
                    }
                }
            }
        }
    }

    class DataGridUtil
    {
        public static string GetName(DependencyObject obj)
        {
            return (string)obj.GetValue(NameProperty);
        }

        public static void SetName(DependencyObject obj, string value)
        {
            obj.SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached("Name", typeof(string), typeof(DataGridUtil), new UIPropertyMetadata(""));

        public static DependencyObject GetDataGridRow(object source)
        {
            DependencyObject dep = (DependencyObject)source;

            while ((dep != null) &&
                !(dep is DataGridCell) &&
                !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep != null)
            {
                if (dep is DataGridColumnHeader)
                {
                    DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
                    // do something
                }

                if (dep is DataGridCell)
                {
                    DataGridCell cell = dep as DataGridCell;
                    while ((dep != null) && !(dep is DataGridRow))
                    {
                        dep = VisualTreeHelper.GetParent(dep);
                    }
                }
            }
            return dep;
        }

        public static DataGridCell TryToFindGridCell(DataGridCellInfo cellInfo, DataGrid grid)
        {
            DataGridCell result = null;
            DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(cellInfo.Item);
            if (row != null)
            {
                int columnIndex = grid.Columns.IndexOf(cellInfo.Column);
                if (columnIndex > -1)
                {
                    DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);
                    result = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
                }
            }
            return result;
        }

        static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
    }

    class CustomCollectionView : ICollectionView
    {
        ICollectionView inner;
        bool canFilter;
        public CustomCollectionView(ICollectionView innerCollection)
        {
            this.inner = innerCollection;
        }

        public bool CanFilter
        {
            get { return this.canFilter; }
        }

        public bool CanGroup
        {
            get { return false; }
        }

        public bool CanSort
        {
            get { return false; }
        }

        public bool Contains(object item)
        {
            return this.inner.Contains(item);
        }

        public System.Globalization.CultureInfo Culture
        {
            get
            {
                return this.inner.Culture;
            }
            set
            {
                this.inner.Culture = value;
            }
        }

        public event EventHandler CurrentChanged
        {
            add { this.inner.CurrentChanged += value; }
            remove { this.inner.CurrentChanged -= value; }
        }

        public event CurrentChangingEventHandler CurrentChanging
        {
            add { this.inner.CurrentChanging += value; }
            remove { this.inner.CurrentChanging -= value; }
        }

        public object CurrentItem
        {
            get { return this.inner.CurrentItem; }
        }

        public int CurrentPosition
        {
            get { return this.inner.CurrentPosition; }
        }

        public IDisposable DeferRefresh()
        {
            return this.inner.DeferRefresh();
        }

        public Predicate<object> Filter
        {
            get
            {
                return this.inner.Filter;
            }
            set
            {
                this.inner.Filter = value;
                this.canFilter = this.inner.Filter != null;
            }
        }

        public ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return this.inner.GroupDescriptions; }
        }

        public ReadOnlyObservableCollection<object> Groups
        {
            get { return this.inner.Groups; }
        }

        public bool IsCurrentAfterLast
        {
            get { return inner.IsCurrentAfterLast; }
        }

        public bool IsCurrentBeforeFirst
        {
            get { return this.IsCurrentBeforeFirst; }
        }

        public bool IsEmpty
        {
            get { return this.inner.IsEmpty; }
        }

        public bool MoveCurrentTo(object item)
        {
            return this.inner.MoveCurrentTo(item);
        }

        public bool MoveCurrentToFirst()
        {
            return this.inner.MoveCurrentToFirst();
        }

        public bool MoveCurrentToLast()
        {
            return this.inner.MoveCurrentToLast();
        }

        public bool MoveCurrentToNext()
        {
            return this.inner.MoveCurrentToNext();
        }

        public bool MoveCurrentToPosition(int position)
        {
            return this.inner.MoveCurrentToPosition(position);
        }

        public bool MoveCurrentToPrevious()
        {
            return this.inner.MoveCurrentToPrevious();
        }

        public void Refresh()
        {
            this.inner.Refresh();
        }

        public SortDescriptionCollection SortDescriptions
        {
            get { return this.inner.SortDescriptions; }
        }

        public IEnumerable SourceCollection
        {
            get { return this.inner.SourceCollection; }
        }

        public IEnumerator GetEnumerator()
        {
            return new ViewEnumerator(this.inner.GetEnumerator());
        }


        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { this.inner.CollectionChanged += value; }
            remove { this.inner.CollectionChanged -= value; }
        }

        class ViewEnumerator : IEnumerator
        {
            IEnumerator inner;
            EventRecordProxy previous;
            EventRecordProxy current;
            public ViewEnumerator(IEnumerator inner)
            {
                this.inner = inner;
            }
            public object Current
            {
                get { return this.inner.Current; }
            }

            public bool MoveNext()
            {
                bool canmove = this.inner.MoveNext();

                if (canmove)
                {
                    this.current = (EventRecordProxy)this.inner.Current;
                    this.current.Previous = previous;
                    previous = this.current;
                }
                return canmove;
            }

            public void Reset()
            {
                this.inner.Reset();
            }
        }

    }

}
