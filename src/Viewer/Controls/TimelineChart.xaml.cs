namespace EtlViewer.Viewer.Controls
{
    using EtlViewer.Viewer.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms.DataVisualization.Charting;
    
    /// <summary>
    /// Interaction logic for TimelineChart.xaml
    /// </summary>
    partial class TimelineChart : UserControl
    {

        List<DataPointCollection> levelPoints;
        private Chart chart1;
        TimelineState[] states;
        private long resolution;
        TimelineModel Model;

        public TimelineChart()
        {
            InitializeComponent();
            SetupChart();
            this.DataContextChanged += TimelineChart_DataContextChanged;
        }

        void TimelineChart_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TimelineModel)
            {
                this.Model = e.NewValue as TimelineModel;
                this.Model.PropertyChanged += Model_PropertyChanged;
                this.Model.AddData += this.Populate;
            }
        }

        void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == TimelineModel.SelectionStartPropertyName)
            {
                this.chart1.ChartAreas[0].CursorX.SelectionStart = new DateTime(this.Model.SelectionStart).ToOADate();
            }
            else if (e.PropertyName == TimelineModel.SelectionEndPropertyName)
            {
                this.chart1.ChartAreas[0].CursorX.SelectionEnd = new DateTime(this.Model.SelectionEnd).ToOADate();
            }
        }

        void SetupChart()
        {
            this.chart1 = this.FindName("MyWinformChart") as Chart;
            this.ApplyStyle(chart1);
            this.levelPoints = (from s in chart1.Series select s.Points).ToList();
        }

        private void ApplyStyle(Chart chart1)
        {
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();

            // 
            // chart1
            // 
            this.chart1.BackColor = System.Drawing.Color.White;
            //this.chart1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
            this.chart1.BackSecondaryColor = System.Drawing.Color.White;
            chartArea1.Area3DStyle.Inclination = 15;
            chartArea1.Area3DStyle.IsClustered = true;
            chartArea1.Area3DStyle.IsRightAngleAxes = false;
            chartArea1.Area3DStyle.Perspective = 10;
            chartArea1.Area3DStyle.Rotation = 10;
            chartArea1.Area3DStyle.WallWidth = 0;
            chartArea1.AxisX.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 7F, System.Drawing.FontStyle.Regular);
            chartArea1.AxisX.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisY.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 7F, System.Drawing.FontStyle.Regular);
            chartArea1.AxisY.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            //chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisY.MajorGrid.LineWidth = 0;
            chartArea1.BackColor = System.Drawing.Color.AliceBlue;
            chartArea1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.LeftRight;
            chartArea1.BackSecondaryColor = System.Drawing.Color.White;
            chartArea1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea1.Name = "Default";
            chartArea1.ShadowColor = System.Drawing.Color.Transparent;
            this.chart1.ChartAreas.Add(chartArea1);

            // Set scrollbar size
            chart1.ChartAreas["Default"].AxisX.ScrollBar.Size = 15;
            chart1.ChartAreas["Default"].AxisX.ScrollBar.Enabled = true;
            chart1.ChartAreas["Default"].AxisX.ScrollBar.IsPositionedInside = true;
            chart1.ChartAreas["Default"].AxisX.ScrollBar.ButtonColor = System.Drawing.Color.White;
            chart1.ChartAreas["Default"].AxisX.ScrollBar.BackColor = System.Drawing.Color.White;
            chart1.ChartAreas["Default"].AxisX.ScaleView.Zoomable = false;
            chart1.ChartAreas["Default"].AxisX.ScaleView.SizeType = DateTimeIntervalType.Auto;
            chart1.ChartAreas["Default"].AxisX.ScaleView.SmallScrollMinSizeType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas["Default"].AxisX.ScaleView.SmallScrollSizeType = DateTimeIntervalType.Seconds;

            chart1.ChartAreas["Default"].CursorX.IsUserEnabled = true;
            chart1.ChartAreas["Default"].CursorX.IsUserSelectionEnabled = true;
            chart1.ChartAreas["Default"].CursorX.Interval = 1;
            chart1.ChartAreas["Default"].CursorX.IntervalType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas["Default"].AxisX.LabelStyle.Format = "HH:mm:ss:ffff";
            chart1.ChartAreas["Default"].AxisX.IsMarginVisible = true;

            chart1.ChartAreas["Default"].AxisY.ScaleView.Zoomable = false;
            chart1.ChartAreas["Default"].AxisY.Enabled = AxisEnabled.False;
            //chart1.ChartAreas["Default"].AxisY.Minimum = -5.5;
            //chart1.ChartAreas["Default"].AxisY.Maximum = 0.5;
            chart1.ChartAreas["Default"].AxisY.IsMarginVisible = true;

            legend1.BackColor = System.Drawing.Color.Transparent;
            legend1.Enabled = true;
            legend1.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular);
            legend1.IsTextAutoFit = false;
            legend1.TextWrapThreshold = 200;
            legend1.Name = "Default";
            //legend1.Position.Auto = false;
            //legend1.Position.X = 20;
            //legend1.Position.Y = 0;
            //legend1.Position.Width = 75;
            //legend1.Position.Height = 15;
            legend1.BackColor = System.Drawing.Color.White;
            legend1.BorderColor = System.Drawing.Color.LightGray;
            this.chart1.Legends.Add(legend1);
            //this.chart1.Legends["Default"].InsideChartArea = "Default";
            // Set legend position
            this.chart1.Legends["Default"].Docking = Docking.Top;


            //this.chart1.Location = new System.Drawing.Point(16, 56);
            this.chart1.Name = "chart1";

            AddSeries("Log", System.Drawing.Color.LightSlateGray);
            AddSeries("Critical", System.Drawing.Color.Magenta);
            AddSeries("Error", System.Drawing.Color.Red);
            AddSeries("Warning", System.Drawing.Color.Yellow);
            AddSeries("Informational", System.Drawing.Color.Gray);
            AddSeries("Verbose", System.Drawing.Color.LightGreen);

            //this.chart1.Size = new System.Drawing.Size(412, 296);
            this.chart1.TabIndex = 1;
            chart1.AxisScrollBarClicked += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.ScrollBarEventArgs>(this.chart1_AxisScrollBarClicked);
            //chart1.Customize += chart1_Customize;
            chart1.MouseDown += chart1_MouseDown;
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
        }


        private void AddSeries(string seriesName, System.Drawing.Color color)
        {
            System.Windows.Forms.DataVisualization.Charting.Series series = new System.Windows.Forms.DataVisualization.Charting.Series();
            series.BorderColor = color;
            series.ChartArea = "Default";
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            series.XValueType = ChartValueType.DateTime;
            series.YValueType = ChartValueType.Int32;
            series.Font = new System.Drawing.Font("Trebuchet MS", 9F);
            series.Legend = "Default";
            series.MarkerStyle = MarkerStyle.Square;
            series.MarkerSize = 3;
            series.MarkerColor = color;
            series.Name = seriesName;
            series.Sort(PointSortOrder.Descending);
            series.ShadowOffset = 1;

            this.chart1.Series.Add(series);
        }


        void chart1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                this.ZoomSelectionMenu.IsOpen = true;
            }
        }

        private void chart1_Customize(object sender, EventArgs e)
        {
            foreach (var label in chart1.ChartAreas[0].AxisY.CustomLabels)
            {
                int v;
                if (Int32.TryParse(label.Text, out v) && v > (int)Levels.None && v < (int)Levels.Max)
                {
                    label.Text = ((Levels)v).ToString();
                }
                else
                {
                    label.Text = string.Empty;
                }
            }
        }

        private void ZoomMenuClick(object sender, RoutedEventArgs e)
        {
            this.chart1.ChartAreas[0].AxisX.ScaleView.Zoom(
                this.chart1.ChartAreas[0].CursorX.SelectionStart,
                this.chart1.ChartAreas[0].CursorX.SelectionEnd);
        }

        private void SelectIntervalMenuClick(object sender, RoutedEventArgs e)
        {
            double start = Math.Min(this.chart1.ChartAreas[0].CursorX.SelectionStart, this.chart1.ChartAreas[0].CursorX.SelectionEnd);
            double stop = Math.Max(this.chart1.ChartAreas[0].CursorX.SelectionStart, this.chart1.ChartAreas[0].CursorX.SelectionEnd);
            this.Model.SelectionStart = DateTime.FromOADate(start).Ticks;
            this.Model.SelectionEnd = DateTime.FromOADate(stop).Ticks;
        }

        private void SelectViewClick(object sender, RoutedEventArgs e)
        {
            this.Model.SelectView();
        }

        private void chart1_AxisScrollBarClicked(object sender, System.Windows.Forms.DataVisualization.Charting.ScrollBarEventArgs e)
        {
            // Handle zoom reset button
            if (e.ButtonType == ScrollBarButtonType.ZoomReset)
            {
                // Event is handled, no more processing required
                e.IsHandled = true;

                // Reset zoom on X and Y axis
                //TODO : Does not restore the zoom states
                chart1.ChartAreas["Default"].AxisX.ScaleView.ZoomReset(1);
            }
        }

        internal void Populate(IList<TimelineEvent> list)
        {
            TimelineEvent current;
            TimelineState state;
            foreach (TimelineEvent item in list)
            {
                current = item;
                try
                {
                    state = states[current.Level % 6];
                    if ((current.Ticks - state.previous) > state.resolution)
                    {
                        state.previous = current.Ticks;
                        levelPoints[state.level].AddXY(new DateTime(current.Ticks), -state.level);
                    }
                }
                catch
                {
                }
            }
        }

        private void SetupStates()
        {
            this.Model.Reset();
            foreach (var item in this.levelPoints)
            {
                item.Clear();
            }

            resolution = TimeSpan.FromSeconds(5).Ticks;
            states = new TimelineState[6];
            for (int i = 0; i < 6; i++)
            {
                states[i] = new TimelineState(i);
                states[i].resolution = TimeSpan.FromMilliseconds(500).Ticks;
            }

            states[0].resolution = TimeSpan.FromMinutes(1).Ticks;
        }

        public enum Levels
        {
            None = -1,
            Log = 0,
            Critical,
            Error,
            Warning,
            Info,
            Verbose,
            Max,
        }

        class TimelineState
        {
            public int level;
            public long previous;
            public long resolution;

            public TimelineState(int level)
            {
                this.previous = DateTime.MinValue.Ticks;
                this.level = level;
                this.resolution = 0;
            }
        }

        internal void StartLoad()
        {
            this.SetupStates();
        }

        internal void LoadComplete()
        {
            AddStartToLegend();
        }

        private void AddStartToLegend()
        {
            this.chart1.Legends[0].CustomItems.Clear();
            var legendItem = new LegendItem("Start " + new DateTime(this.Model.StartTime).ToString(), System.Drawing.Color.Transparent, "");
            legendItem.ImageStyle = LegendImageStyle.Line;
            legendItem.MarkerStyle = MarkerStyle.Circle;
            this.chart1.Legends[0].CustomItems.Add(legendItem);
            var legendItemStop = new LegendItem("End " + new DateTime(this.Model.StopTime).ToString(), System.Drawing.Color.Transparent, "");
            legendItemStop.ImageStyle = LegendImageStyle.Line;
            legendItemStop.MarkerStyle = MarkerStyle.Circle;
            this.chart1.Legends[0].CustomItems.Add(legendItemStop);

        }

    }


    class TimelineEvent : IComparable<TimelineEvent>
    {
        public long Ticks { get; set; }
        public byte Level { get; set; }

        public int CompareTo(TimelineEvent other)
        {
            return this.Ticks.CompareTo(other.Ticks);
        }
    }
}
