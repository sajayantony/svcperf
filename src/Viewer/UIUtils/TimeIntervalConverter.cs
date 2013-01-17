namespace EtlViewer.Viewer.UIUtils
{
    using System;
    using System.Windows.Data;
    
    class TimeIntervalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long ticks = (long)value;
            if (ticks == long.MaxValue)
            {
                return "Max";
            }
            else if (ticks == long.MinValue)
            {
                return "Min";
            }
            else
            {
                return new DateTime(ticks).ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long ticks;
            string dateTimeString = (string)value;
            DateTime dt;
            if (!String.IsNullOrEmpty(dateTimeString))
            {
                dateTimeString = dateTimeString.ToLower();
            }

            if (String.CompareOrdinal("max", dateTimeString.ToLower()) == 0)
            {
                return long.MaxValue;
            }
            else if (String.CompareOrdinal("min", dateTimeString) == 0)
            {
                return long.MinValue;
            }
            else if (long.TryParse(dateTimeString, out ticks))
            {
                return ticks;
            }
            else if (DateTime.TryParse(dateTimeString, out dt))
            {
                return dt.Ticks;
            }

            //This should cause a validation exception.
            return null;
        }
    }
}
