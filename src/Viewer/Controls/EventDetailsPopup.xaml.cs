namespace EtlViewer.Viewer.Controls
{
    using System;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;

    partial class EventDetailsPopup : Popup
    {
        public string Text
        {
            get { return this.txtFind.Text; }
            set { this.txtFind.Text = value; }
        }

        public EventDetailsPopup()
        {
            this.InitializeComponent();
            this.Opened += (s, args) =>
            {
                this.txtFind.Focus();
            };
            
            var thumb = new Thumb
            {
                Width = 0,
                Height = 0,
            };
            ContentCanvas.Children.Add(thumb);

            MouseDown += (sender, e) =>
            {
                thumb.RaiseEvent(e);
            };

            thumb.DragDelta += (sender, e) =>
            {
                HorizontalOffset += e.HorizontalChange;
                VerticalOffset += e.VerticalChange;
            };
        }

        private void Button_CloseClick(object sender, System.Windows.RoutedEventArgs e)
        {
            this.IsOpen = false;
        }

        private void txtFind_KeyDown_1(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.IsOpen = false;
            }
        }
    }
}