using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataLibrary;

namespace EtlViewer
{
    /// <summary>
    /// Interaction logic for EventRangeSlider.xaml
    /// </summary>
    public partial class EventRangeSlider : UserControl
    {
        public event Action<SelectedRange> OnSelectInterval;
        public event Action<SelectedRange> OnZoomToInterval;
        public event Action<SelectedRange> OnZoomOutInterval;

        public EventRangeSlider()
        {
            this.InitializeComponent();
            this.Loaded += RangeSlider_Loaded;
        }

        #region properties

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(EventRangeSlider), new UIPropertyMetadata(0d));

        public double LowerValue
        {
            get { return (double)GetValue(LowerValueProperty); }
            set { SetValue(LowerValueProperty, value); }
        }

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register("LowerValue", typeof(double), typeof(EventRangeSlider), new UIPropertyMetadata(0d));

        public double UpperValue
        {
            get { return (double)GetValue(UpperValueProperty); }
            set { SetValue(UpperValueProperty, value); }
        }

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register("UpperValue", typeof(double), typeof(EventRangeSlider), new UIPropertyMetadata(0d));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(EventRangeSlider), new UIPropertyMetadata(1d));

        #endregion

        #region Eventhandler

        void RangeSlider_Loaded(object sender, RoutedEventArgs e)
        {
            LowerSlider.ValueChanged += LowerSlider_ValueChanged;
            UpperSlider.ValueChanged += UpperSlider_ValueChanged;

        }

        private void InitializeTickBar()
        {
            double total = (this.Maximum - this.Minimum);
            double tickFrequency = (total / 10.0);

            for (double i = 0; i < total; i += tickFrequency)
            {
                this.TickbarSlider.Ticks.Add(i);
            }

            this.TickbarSlider.TickFrequency = total / 10;
        }

        private void LowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpperValue = Math.Max(UpperValue, LowerValue);
        }

        private void UpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LowerValue = Math.Min(UpperValue, LowerValue);
        }

        #endregion

        public void SetDataContext(ObservableNotifiableCollection<DataPoint> events, SelectedRange range)
        {
            this.Minimum = range.Start;
            this.Maximum = range.Stop;
            this.SetSelection(this.Minimum, this.Minimum);
            this.InitializeTickBar();
            this.DataContext = new EventRangeCollection()
            {
                DataPoints = events,
                Range = new SelectedRange()
                {
                    Start = this.Minimum,
                    Stop = this.Maximum
                }
            };
        }

        Point? selectionStart;
        Point? selectionEnd;
        private void LayoutRoot_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (selectionStart == null)
                {
                    selectionStart = e.GetPosition((IInputElement)this.LayoutRoot);
                    // Capture and track the mouse. 


                    // Initial placement of the drag selection box.                    
                    Canvas.SetLeft(selectionBox, selectionStart.Value.X);
                    Canvas.SetTop(selectionBox, selectionStart.Value.Y);
                    selectionBox.Width = 5;
                    selectionBox.Height = 5;
                    // Make the drag selection box visible. 
                    selectionBox.Visibility = Visibility.Visible;
                }
                else
                {
                    Point mouseDownPos = this.selectionStart.Value;
                    Point mousePos = e.GetPosition((IInputElement)this.LayoutRoot);

                    if (mouseDownPos.X < mousePos.X)
                    {
                        Canvas.SetLeft(selectionBox, mouseDownPos.X);
                        selectionBox.Width = mousePos.X - mouseDownPos.X;
                    }
                    else
                    {
                        Canvas.SetLeft(selectionBox, mousePos.X);
                        selectionBox.Width = mouseDownPos.X - mousePos.X;
                    }

                    if (mouseDownPos.Y < mousePos.Y)
                    {
                        Canvas.SetTop(selectionBox, mouseDownPos.Y);
                        selectionBox.Height = mousePos.Y - mouseDownPos.Y;
                    }
                    else
                    {
                        Canvas.SetTop(selectionBox, mousePos.Y);
                        selectionBox.Height = mouseDownPos.Y - mousePos.Y;
                    }

                }
            }
            else if (e.LeftButton == MouseButtonState.Released)
            {
                if (selectionStart != null)
                {
                    selectionEnd = e.GetPosition((IInputElement)this);
                    this.SelectRange(selectionStart.Value, selectionEnd.Value);
                    selectionBox.Visibility = System.Windows.Visibility.Hidden;
                    this.selectionStart = null;
                    this.selectionEnd = null;
                }
            }
            e.Handled = true;
        }

        public void SetSelection(double min, double max)
        {
            Trace.Assert(this.Minimum <= min, "min value should be greater than this.Minimum");
            Trace.Assert(this.Maximum >= max, "max value should be lesser than this.Maximum.");

            this.LowerValue = min;
            this.UpperValue = max;

            //this.LowerSlider.Value = min;
            //this.UpperSlider.Value = max;           
            //Highlight selectionRange
            this.LowerSlider.SelectionStart = min;
            this.LowerSlider.SelectionEnd = max;
        }

        private void SelectRange(Point p1, Point p2)
        {
            Point start = p1.X > p2.X ? p2 : p1;
            Point stop = p1.X < p2.X ? p2 : p1;
            double ratio = (this.Maximum - this.Minimum) / this.ActualWidth;
            double min = Math.Max(start.X * ratio + this.Minimum, this.Minimum);
            double max = stop.X * ratio + this.Minimum;
            this.SetSelection(min, max);
        }

        public ObservableNotifiableCollection<DataPoint> CreateDataPointsFromEvents(
            IEnumerable<TimelineEvent> events,            
            double minValue,
            double maxValue,
            double actualPixelWidth)
        {            
            var datapoints = new ObservableNotifiableCollection<DataPoint>();
            const int maxTypes = 6;
            
            double totalDuration = maxValue - minValue;
            //We want to display less dots if possible pixels. 
            double resolutionPerPixel = totalDuration / (actualPixelWidth * 96); //96DPI per inch
            double prev = 0;
            double[] prevType = new double[maxTypes];

            double tickDelta;
            foreach (var item in events)
            {
                if ((item.Ticks - prev) >= resolutionPerPixel
                    || item.Level != prevType[item.Level])
                {
                    prev = item.Ticks;
                    prevType[item.Level] = item.Level;

                    tickDelta = item.Ticks - minValue;
                    datapoints.Add(new DataLibrary.DataPoint()
                    {
                        VariableX = tickDelta / totalDuration,
                        VariableY = 0.1 + ((double)item.Level / 6.0),
                        Type = item.Level
                    });
                }
            }

            return datapoints;
        }

        class EventRangeCollection : NotifiableDataCollection
        {
            public SelectedRange Range { get; set; }
            public EventRangeCollection()
            {
                this.Range = new SelectedRange();
            }
        }

        private void SelectInterval_Click(object sender, RoutedEventArgs e)
        {            
            if (OnSelectInterval != null)
            {
                OnSelectInterval(new SelectedRange() { Start = this.LowerValue, Stop = this.UpperValue });
            }
        }

        private void DeselectInterval_Click(object sender, RoutedEventArgs e)
        {
            this.SetSelection(this.Minimum, this.Minimum);
        }

        private void ZoomToSelection_Click(object sender, RoutedEventArgs e)
        {
            if (this.OnZoomToInterval != null)
            {
                OnZoomToInterval(new SelectedRange() { Start = this.LowerValue, Stop = this.UpperValue });
            }
        }

        private void ZoomOutInterval_Click(object sender, RoutedEventArgs e)
        {
            if (OnZoomOutInterval != null)
            {
                OnZoomOutInterval(new SelectedRange() { Start = this.LowerValue, Stop = this.UpperValue });
            }
        }

        private void SelectFullRange_Click(object sender, RoutedEventArgs e)
        {
            this.SetSelection(this.Minimum, this.Maximum);
        }

    }

    public struct SelectedRange
    {
        public double Start { get; set; }
        public double Stop { get; set; }
    }
}