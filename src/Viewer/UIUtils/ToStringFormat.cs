namespace EtlViewer.Viewer.UIUtils
{
    using System;
    using System.Windows.Data;
    
    [ValueConversion(typeof(object), typeof(string))]
    class ToStringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string fmt = parameter as string;
            if (!string.IsNullOrEmpty(fmt))
            {
                if (fmt == "x" && value is ulong)
                {
                    return "0x" + ((ulong)value).ToString("x");
                }

                return string.Format(culture, fmt, value);
            }
            else
            {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
