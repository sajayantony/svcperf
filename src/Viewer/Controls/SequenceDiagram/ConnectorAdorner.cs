namespace EtlViewer.Viewer.Controls
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;

    // Adorners must subclass the abstract base class Adorner. 
    public abstract class BaseActivityAdorner : Adorner
    {
        string message;
        public UIElement Source { get; set; }
        public double TopOffset { get; set; }
        public double Ratio { get; set; }
        public int Index { get; set; }
        public double ParentHeight { get; set; }
        public double ParentWidth { get; set; }

        public string Message
        {
            get
            {
                return this.message;
            }
            set
            {
                if (this.ToolTip == null)
                {
                    var tooltip = new ToolTip();
                    this.ToolTip = tooltip;
                    ToolTipService.SetToolTip(this, tooltip);
                }

                ((ToolTip)this.ToolTip).Content = value;
                this.message = value;
            }
        }

        // Be sure to call the base class constructor. 
        public BaseActivityAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }



        protected abstract override void OnRender(DrawingContext drawingContext);

        public double AvailbleHeight
        {
            get { return (ParentHeight - TopOffset) * 0.9; }
        }

        public double SegmentHeight
        {
            get { return AvailbleHeight * this.Ratio; }
        }

        public double SegmentTop
        {
            get { return TopOffset + this.SegmentHeight * this.Index; }
        }

        protected Rect GetTansformedRect(UIElement elem)
        {
            // we clip the top and use only 90% of the parents height.
            double height = AvailbleHeight;
            var toPoint = elem.TransformToAncestor(this.AdornedElement).Transform(new Point(0, this.TopOffset));
            var targetRect = new Rect(toPoint.X, toPoint.Y,
                                    elem.DesiredSize.Width,
                                    Math.Max(height, 1));
            return targetRect;
        }

        public Point GetMidPoint(ref Rect targetActivity)
        {
            return new Point(targetActivity.Width / 2.0 + targetActivity.X, SegmentTop + SegmentHeight / 2.0);
        }

        #region Arrow Head drawing

        protected static void CreateFromTriangle(DrawingContext dc, Point start)
        {
            LineSegment[] segments = new LineSegment[] { new LineSegment(new Point(5, 0), true), new LineSegment(new Point(5, 10), true) };
            PathFigure figure = new PathFigure(new Point(0, 5), segments, true);
            PathGeometry geo = new PathGeometry(new PathFigure[] { figure }, FillRule.Nonzero, new TranslateTransform(start.X, start.Y - 5));
            dc.DrawGeometry(Brushes.Red, null, geo);
        }


        protected static void CreateToTriangle(DrawingContext dc, Point start)
        {
            LineSegment[] segments = new LineSegment[] { new LineSegment(new Point(5, 5), true), new LineSegment(new Point(0, 10), true) };
            PathFigure figure = new PathFigure(new Point(0, 0), segments, true);
            PathGeometry geo = new PathGeometry(new PathFigure[] { figure }, FillRule.Nonzero, new TranslateTransform(start.X - 5, start.Y - 5));
            dc.DrawGeometry(Brushes.Red, null, geo);
        }

        #endregion

    }

    class PointActivity : BaseActivityAdorner
    {
        static SolidColorBrush renderBrush;
        static Pen renderPen;
        const double renderRadius = 5.0;

        static PointActivity()
        {
            renderBrush = new SolidColorBrush(Color.FromRgb(204, 230, 204));
            renderBrush.Opacity = 1.0;
            renderPen = new Pen(new SolidColorBrush(Colors.Navy), 1.5);
        }

        public PointActivity(UIElement adornedElement)
            : base(adornedElement)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            try
            {
                Rect adornedElementRect = new Rect(this.AdornedElement.DesiredSize);

                // Sequence item absolute rectangle
                var activityRect = this.GetTansformedRect(this.Source);
                var midPoint = GetMidPoint(ref activityRect);
                drawingContext.DrawEllipse(renderBrush,
                                            renderPen,
                                            midPoint,
                                            renderRadius,
                                            renderRadius);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }


    class ConnectorAdorner : BaseActivityAdorner
    {
        static Pen connectorPen = new Pen(new SolidColorBrush(Colors.Gray), 1.5);
        static SolidColorBrush textBrush = new SolidColorBrush(Colors.Black);

        static ConnectorAdorner()
        {
            textBrush.Opacity = 0.4;
        }

        public ConnectorAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }

        public UIElement To { get; set; }

        // A common way to implement an adorner's rendering behavior is to override the OnRender 
        // method, which is called by the layout system as part of a rendering pass. 
        protected override void OnRender(DrawingContext dc)
        {
            try
            {
                var fromRect = GetTansformedRect(this.Source);
                var segmentHeight = this.SegmentHeight;
                if (this.Source != this.To)
                {
                    var activityRect = this.GetTansformedRect(this.Source);
                    var midPoint = GetMidPoint(ref activityRect);
                    Point start = midPoint;

                    activityRect = this.GetTansformedRect(this.To);
                    Point stop = GetMidPoint(ref activityRect);
                    this.DrawConnector(dc, ref start, ref stop);
                }
                else
                {
                    var activityRect = this.GetTansformedRect(this.Source);
                    var midPoint = GetMidPoint(ref activityRect);
                    Point start = midPoint;

                    Point t = new Point(start.X, start.Y + segmentHeight / 10.0);
                    this.DrawloopbackConnector(dc, ref start, ref t, fromRect.Width);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void DrawloopbackConnector(DrawingContext dc,
                                    ref Point start,
                                    ref Point stop,
                                    double width)
        {
            Rect loop = new Rect(start.X, start.Y, width / 2.0, SegmentHeight / 2.0);
            dc.DrawLine(connectorPen, loop.TopLeft, loop.TopRight);
            dc.DrawLine(connectorPen, loop.TopRight, loop.BottomRight);
            dc.DrawLine(connectorPen, loop.BottomRight, loop.BottomLeft);
            dc.DrawText(new FormattedText(this.Message,
                                            CultureInfo.CurrentCulture,
                                            System.Windows.FlowDirection.LeftToRight,
                                            new Typeface("Arial"),
                                            12,
                                            textBrush),
                                            new Point(loop.Left + 5, loop.Top - 15));
            CreateFromTriangle(dc, loop.BottomLeft);
        }

        private void DrawConnector(DrawingContext dc, ref Point start, ref Point stop)
        {
            bool rtl = start.X <= stop.X;
            dc.DrawLine(connectorPen, start, stop);
            textBrush.Opacity = 0.5;
            dc.DrawText(new FormattedText(this.Message,
                                            CultureInfo.CurrentCulture,
                                            rtl ? System.Windows.FlowDirection.LeftToRight : System.Windows.FlowDirection.RightToLeft,
                                            new Typeface("Arial"),
                                            12,
                                            textBrush),
                                            new Point(start.X + (rtl ? 5 : -5), start.Y - 15));

            if (rtl)
            {
                CreateToTriangle(dc, stop);
            }
            else
            {
                CreateFromTriangle(dc, stop);
            }
        }
    }
}
