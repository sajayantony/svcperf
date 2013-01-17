using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace EtlViewer.Viewer.UIUtils
{
    class RealTimeSpinnyConverter : IValueConverter
    {
        static BrushConverter brushConverter = new BrushConverter();
        static SolidColorBrush historyLoadBrush = (SolidColorBrush)brushConverter.ConvertFromString("#FF01E2FF");
        static SolidColorBrush realtimeLoadBrush = new SolidColorBrush(Color.FromRgb(209, 72, 7));

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isRealTime = (bool)value;
            if (isRealTime)
            {
                return realtimeLoadBrush;
            }
            else
            {
                return historyLoadBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
