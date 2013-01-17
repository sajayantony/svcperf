namespace EtlViewer.Viewer.UIUtils
{
    using System.Windows;

    class TitleMenu
    {
        #region dependency property

        /// <summary>
        /// An attached dependency property which provides an
        /// <see cref="ImageSource" /> for arbitrary WPF elements.
        /// </summary>
        public static readonly DependencyProperty ContentProperty;

        /// <summary>
        /// Gets the <see cref="ContentProperty"/> for a given
        /// <see cref="DependencyObject"/>, which provides an
        /// <see cref="ImageSource" /> for arbitrary WPF elements.
        /// </summary>
        public static object GetContent(DependencyObject obj)
        {
            return (object)obj.GetValue(ContentProperty);
        }

        /// <summary>
        /// Sets the attached <see cref="ContentProperty"/> for a given
        /// <see cref="DependencyObject"/>, which provides an
        /// <see cref="ImageSource" /> for arbitrary WPF elements.
        /// </summary>
        public static void SetContent(DependencyObject obj, object value)
        {
            obj.SetValue(ContentProperty, value);
        }

        #endregion

        static TitleMenu()
        {
            //register attached dependency property
            var metadata = new FrameworkPropertyMetadata((object)null);
            ContentProperty = DependencyProperty.RegisterAttached("Content",
                                                                typeof(object),
                                                                typeof(TitleMenu), metadata);
        }
    }
}
