namespace EtlViewer.Viewer.UIUtils
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    
    class TimeSpanToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan t = (TimeSpan)value;
            if (t != TimeSpan.MinValue)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
