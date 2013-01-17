using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Globalization;
using System.Windows;

namespace EtlViewer
{
    class TimelineTickbar : TickBar
    {
        static Pen Timelineborder = new Pen() { Brush = Brushes.Navy, Thickness = 1.0 };
        static Typeface Verdana = new Typeface("Verdana");

        enum TimelineResolution
        {
            Hours,
            Minutes,
            Seconds,
            Milliseconds
        }

        protected override void OnRender(System.Windows.Media.DrawingContext dc)
        {
            if (this.TickFrequency <= 1)
            {
                return;
            }

            dc.DrawLine(Timelineborder, new Point(0, 0), new Point(this.ActualWidth, 0));
            double totalCount = this.Maximum - this.Minimum;
            double y = this.ReservedSpace * 0.5;
            FormattedText formattedText = null;
            double x = 0;
            double pixelDelta = this.ActualWidth / (totalCount / this.TickFrequency);
            double innerDelta = pixelDelta / 10;
            TimelineResolution resolution = this.GetMinResolution(totalCount);
            for (double i = 0; i <= totalCount; i += this.TickFrequency, x += pixelDelta)
            {
                string time = i == 0 ? this.GetDateTimeString(this.Minimum) : "+" + this.GetTimeString(resolution, i);
                formattedText = new FormattedText(time,
                            CultureInfo.CurrentUICulture,
                            System.Windows.FlowDirection.LeftToRight,
                            Verdana,
                            8,
                            Brushes.Black);

                dc.DrawText(formattedText, new Point(x, 10));
                //base.OnRender(dc);
                dc.DrawLine(Timelineborder, new Point(x, 0), new Point(x, 10));


                //Draw innter intervals;
                if (i < this.Maximum)
                {
                    for (double k = innerDelta; k < pixelDelta; k += innerDelta)
                    {
                        dc.DrawLine(Timelineborder, new Point(x + k, 0), new Point(x + k, 5));
                    }
                }
            }
        }

        private string GetTimeString(TimelineResolution resolution, double time)
        {
            switch (resolution)
            {
                case TimelineResolution.Hours:
                    return TimeSpan.FromTicks((long)time).TotalHours.ToString() + "hrs";
                case TimelineResolution.Minutes:
                    return TimeSpan.FromTicks((long)time).TotalMinutes.ToString() + "m";
                case TimelineResolution.Seconds:
                    return Math.Round(TimeSpan.FromTicks((long)time).TotalSeconds, 3).ToString() +"s";
                case TimelineResolution.Milliseconds:
                    return Math.Round(TimeSpan.FromTicks((long)time).TotalMilliseconds, 3).ToString() + "ms";
            }

            throw new InvalidOperationException();
        }

        public string GetDateTimeString(double time)
        {
            return new DateTime((long)time).ToString("MM/dd/yy-hh:mm:ss:ffff tt");
        }

        private TimelineResolution GetMinResolution(double totalCount)
        {
            TimeSpan total = TimeSpan.FromTicks((long)totalCount);

            if (total.TotalMinutes < 5)
            {
                return TimelineResolution.Milliseconds;
            }
            if (total.TotalMinutes < 120)
            {
                return TimelineResolution.Seconds;
            }
            else if (total.TotalHours < 4)
            {
                return TimelineResolution.Minutes;
            }

            return TimelineResolution.Hours;
        }
    }
}
