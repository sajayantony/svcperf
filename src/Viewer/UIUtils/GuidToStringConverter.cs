using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace EtlViewer.Viewer.UIUtils
{
    class GuidToStringConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ToString((Guid)value);
        }

        public static string ToString(Guid value)
        {
            if (value == Guid.Empty)
            {
                return string.Empty;
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
