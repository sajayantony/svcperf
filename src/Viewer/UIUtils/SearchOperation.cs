namespace EtlViewer.Viewer.UIUtils
{
    using System.Windows;

    public static class SearchOperations
    {
        public static string GetSearchTerm(DependencyObject obj)
        {
            return (string)obj.GetValue(SearchTermProperty);
        }

        public static void SetSearchTerm(DependencyObject obj, string value)
        {
            obj.SetValue(SearchTermProperty, value);
        }

        public static readonly DependencyProperty SearchTermProperty =
            DependencyProperty.RegisterAttached(
                "SearchTerm",
                typeof(string),
                typeof(SearchOperations),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsMatch(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMatchProperty);
        }

        public static void SetIsMatch(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMatchProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsMatch.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMatchProperty =
            DependencyProperty.RegisterAttached("IsMatch", typeof(bool), typeof(SearchOperations), new UIPropertyMetadata(false));
    }
}
