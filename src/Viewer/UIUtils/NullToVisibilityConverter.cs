namespace EtlViewer.Viewer.UIUtils
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    
    class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || String.IsNullOrEmpty(value.ToString()))
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
