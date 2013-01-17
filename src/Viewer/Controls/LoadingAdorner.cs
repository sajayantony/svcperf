namespace EtlViewer
{
    using System.Globalization;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;

    class LoadingAdorner : Adorner
    {
        private string message;
        //static SolidColorBrush background = new SolidColorBrush(Color.FromRgb(246, 246, 246));
        static SolidColorBrush background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        static SolidColorBrush foreground = new SolidColorBrush(Color.FromRgb(0, 86, 143));
        public LoadingAdorner(UIElement parent, string message)
            : base(parent)
        {
            this.Message = message;
        }

        public string Message
        {
            get { return this.message; }
            set
            {
                if (this.message != value)
                {
                    this.message = value;
                    this.InvalidateVisual();
                }
            }
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(background, new Pen(Brushes.White, 1), new Rect(new Point(0, 0), DesiredSize));
            drawingContext.DrawText(new FormattedText(this.Message, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface("SegoeUI"),
                20.0,
                foreground),
                new Point(10, 10));
            base.OnRender(drawingContext);
        }
    }
}
