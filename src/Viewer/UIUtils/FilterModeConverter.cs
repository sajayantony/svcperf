namespace EtlViewer.Viewer.UIUtils
{
    using EtlViewer.Viewer.Models;
    using System;
    using System.Windows.Data;
    
    class FilterModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (FilterMode)Enum.Parse(typeof(FilterMode),value.ToString());
        }
    }
}
