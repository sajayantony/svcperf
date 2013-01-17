namespace EtlViewer.Viewer.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Forms.DataVisualization.Charting;
    using System.Windows.Forms.DataVisualization.Charting.Utilities;

    enum PerfChartType
    {
        HasXY,
        Distribution
    }

    /// <summary>
    /// Interaction logic for DurationHistogram.xaml
    /// </summary>
    partial class DurationHistogram : UserControl
    {
        private PerfChartType ChartType { get; set; }

        public DurationHistogram()
        {
            InitializeComponent();
            this.SetupChart();
            this.SetupLineChart();
            this.HideChartAreas();
        }

        private void HideChartAreas()
        {
            foreach (var item in this.chart1.ChartAreas)
            {
                item.Visible = false;
            }
        }

        private void SetupChart()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.chart1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(223)))), ((int)(((byte)(193)))));
            this.chart1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
            this.chart1.BorderlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(181)))), ((int)(((byte)(64)))), ((int)(((byte)(1)))));
            this.chart1.BorderlineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            this.chart1.BorderlineWidth = 2;
            //this.chart1.BorderSkin.SkinStyle = System.Windows.Forms.DataVisualization.Charting.BorderSkinStyle.Emboss;
            chartArea1.AlignWithChartArea = "HistogramArea";
            chartArea1.Area3DStyle.Inclination = 15;
            chartArea1.Area3DStyle.IsClustered = true;
            chartArea1.Area3DStyle.IsRightAngleAxes = false;
            chartArea1.Area3DStyle.Perspective = 10;
            chartArea1.Area3DStyle.Rotation = 10;
            chartArea1.Area3DStyle.WallWidth = 0;
            chartArea1.AxisX.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;
            chartArea1.AxisX.LabelAutoFitStyle = ((System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles)(((System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.IncreaseFont | System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.DecreaseFont)
                        | System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.WordWrap)));
            chartArea1.AxisX.LabelStyle.Enabled = false;
            chartArea1.AxisX.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            chartArea1.AxisX.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisX.MajorTickMark.LineColor = System.Drawing.Color.Transparent;
            chartArea1.AxisX.MajorTickMark.Size = 1.5F;
            chartArea1.AxisX.Title = "One axis data distribution chart";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("Trebuchet MS", 8F);
            chartArea1.AxisY.IsReversed = true;
            chartArea1.AxisY.LabelStyle.Enabled = false;
            chartArea1.AxisY.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            chartArea1.AxisY.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisY.MajorGrid.Enabled = false;
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisY.MajorTickMark.Enabled = false;
            chartArea1.AxisY.Maximum = 2;
            chartArea1.AxisY.Minimum = 0;
            chartArea1.AxisY2.IsLabelAutoFit = false;
            chartArea1.AxisY2.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            chartArea1.BackColor = System.Drawing.Color.OldLace;
            chartArea1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
            chartArea1.BackSecondaryColor = System.Drawing.Color.White;
            chartArea1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea1.Name = "Default";
            chartArea1.Position.Auto = false;
            chartArea1.Position.Height = 15F;
            chartArea1.Position.Width = 96F;
            chartArea1.Position.X = 3F;
            chartArea1.Position.Y = 4F;
            chartArea1.ShadowColor = System.Drawing.Color.Transparent;
            chartArea2.Area3DStyle.Inclination = 15;
            chartArea2.Area3DStyle.IsClustered = true;
            chartArea2.Area3DStyle.IsRightAngleAxes = false;
            chartArea2.Area3DStyle.Perspective = 10;
            chartArea2.Area3DStyle.Rotation = 10;
            chartArea2.Area3DStyle.WallWidth = 0;
            chartArea2.AxisX.LabelAutoFitStyle = ((System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles)((((System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.IncreaseFont | System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.DecreaseFont)
                        | System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.LabelsAngleStep90)
                        | System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.WordWrap)));
            chartArea2.AxisX.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            chartArea2.AxisX.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea2.AxisX.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea2.AxisX.Title = "Histogram (Frequency Diagram)";
            chartArea2.AxisX.TitleFont = new System.Drawing.Font("Trebuchet MS", 8F);
            chartArea2.AxisY.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            chartArea2.AxisY.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea2.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea2.AxisY2.IsLabelAutoFit = false;
            chartArea2.AxisY2.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            chartArea2.BackColor = System.Drawing.Color.OldLace;
            chartArea2.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
            chartArea2.BackSecondaryColor = System.Drawing.Color.White;
            chartArea2.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea2.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea2.Name = "HistogramArea";
            chartArea2.Position.Auto = false;
            chartArea2.Position.Height = 77F;
            chartArea2.Position.Width = 93F;
            chartArea2.Position.X = 3F;
            chartArea2.Position.Y = 18F;
            chartArea2.ShadowColor = System.Drawing.Color.Transparent;
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.ChartAreas.Add(chartArea2);
            legend1.BackColor = System.Drawing.Color.Transparent;
            legend1.Enabled = false;
            legend1.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            legend1.IsTextAutoFit = false;
            legend1.Name = "Default";
            this.chart1.Legends.Add(legend1);
            //this.chart1.Location = new System.Drawing.Point(16, 65);
            this.chart1.Name = "chart1";
            series1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(26)))), ((int)(((byte)(59)))), ((int)(((byte)(105)))));
            series1.ChartArea = "Default";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            series1.Color = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(252)))), ((int)(((byte)(180)))), ((int)(((byte)(65)))));
            series1.Enabled = false;
            series1.Legend = "Default";
            series1.MarkerSize = 9;
            series1.Name = "RawData";
            series1.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series1.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series2.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(26)))), ((int)(((byte)(59)))), ((int)(((byte)(105)))));
            series2.ChartArea = "Default";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            series2.Color = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(252)))), ((int)(((byte)(180)))), ((int)(((byte)(65)))));
            series2.Legend = "Default";
            series2.MarkerSize = 8;
            series2.Name = "DataDistribution";
            series2.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series2.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series3.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(26)))), ((int)(((byte)(59)))), ((int)(((byte)(105)))));
            series3.ChartArea = "HistogramArea";
            series3.Color = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(64)))), ((int)(((byte)(10)))));
            series3.IsValueShownAsLabel = true;
            series3.Legend = "Default";
            series3.Name = "Histogram";
            series3.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series3.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            this.chart1.Series.Add(series1);
            this.chart1.Series.Add(series2);
            this.chart1.Series.Add(series3);
            //this.chart1.Size = new System.Drawing.Size(412, 296);
            this.chart1.TabIndex = 1;
            // 
            // HistogramChart
            // 
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
        }

        private void SetupLineChart()
        {
            Chart chart = this.chart1;
            chart.SuspendLayout();
            ChartArea chartArea = new ChartArea();
            chartArea.Name = "AreaXY";
            chart.ChartAreas.Add(chartArea);
            Series series1 = new Series();
            series1.Name = "SeriesXY";
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 3;
            series1.MarkerColor = System.Drawing.Color.Black;
            series1.MarkerStyle = MarkerStyle.Circle;
            series1.MarkerSize = 5;
            series1.Legend = "legend1";
            series1.XValueMember = "Key";
            series1.YValueMembers = "Value";
            series1.ToolTip = "#VALY, #VALX";
            series1.ChartArea = "AreaXY";
            chart.Series.Add(series1);
            chart1.ResumeLayout();
        }

        public void LoadData(double[][] points, bool hasX)
        {
            this.Values = points;
            this.HideChartAreas();

            this.ChartType = hasX ? PerfChartType.HasXY : PerfChartType.Distribution;
            bool canShowLog = true;

            // Clear all the old points.
            foreach (var series in this.chart1.Series)
            {
                series.Points.Clear();
            }

            if (this.ChartType == PerfChartType.HasXY)
            {
                chart1.ChartAreas["AreaXY"].Visible = true;
                Series series = this.chart1.Series["SeriesXY"];
                foreach (var item in this.Values)
                {
                    series.Points.AddXY(item[0], item[1]);
                    if (canShowLog && item[0] <= 0)
                    {
                        canShowLog = false;
                    }
                }
            }
            else
            {
                chart1.ChartAreas["Default"].Visible = true;
                chart1.ChartAreas["HistogramArea"].Visible = true;
                for (int i = 0; i < Values.Length; i++)
                {
                    chart1.Series["RawData"].Points.AddY(Values[i][1]);
                    if (canShowLog && Values[i][0] <= 0)
                    {
                        canShowLog = false;
                    }
                }

                // Populate single axis data distribution series. Show Y value of the
                // data series as X value and set all Y values to 1.
                foreach (DataPoint dataPoint in chart1.Series["RawData"].Points)
                {
                    chart1.Series["DataDistribution"].Points.AddXY(dataPoint.YValues[0], 1);
                }

                // Update chart
                UpdateChartSettings();

            }

            this.MenuLog.Visibility = canShowLog ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateChartSettings()
        {
            //if (!this.loadingData)
            {
                // Create a histogram series
                HistogramChartHelper histogramHelper = new HistogramChartHelper();
                //histogramHelper.SegmentIntervalNumber = int.Parse(comboBoxIntervalNumber.Text);
                histogramHelper.ShowPercentOnSecondaryYAxis = true;
                // NOTE: Interval width may be specified instead of interval number
                //histogramHelper.SegmentIntervalWidth = 15;
                histogramHelper.CreateHistogram(chart1, "RawData", "Histogram");

                // Set same X axis scale and interval in the single axis data distribution 
                // chart area as in the histogram chart area.
                chart1.ChartAreas["Default"].AxisX.Minimum = chart1.ChartAreas["HistogramArea"].AxisX.Minimum;
                chart1.ChartAreas["Default"].AxisX.Maximum = chart1.ChartAreas["HistogramArea"].AxisX.Maximum;
                chart1.ChartAreas["Default"].AxisX.Interval = chart1.ChartAreas["HistogramArea"].AxisX.Interval;
            }
        }

        double[][] Values;

        private void ToggleIsLogarithmic(object sender, RoutedEventArgs e)
        {
            ToggleButton button = sender as ToggleButton;
            
            if (button != null)
            {
                bool isLog = button.IsChecked == true;
                foreach (var item in chart1.ChartAreas)
                {
                    item.AxisY.IsLogarithmic = isLog;
                }
            }
        }
    }
}
