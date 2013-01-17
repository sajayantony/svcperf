namespace EtlViewer.Viewer.UIUtils
{
    using System;
    using System.Diagnostics;
    using System.Windows.Data;
    
    class EventTimeDiffConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {            
            if (values != null && values.Length > 1 && values[0] != null && values[1] != null)
            {
                Debug.Write(values[0] == values[1]);
                return (((EventRecordProxy)values[0]).TimeStamp - ((EventRecordProxy)values[1]).TimeStamp).TotalMilliseconds.ToString();
            }
            
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
