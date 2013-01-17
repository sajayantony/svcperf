namespace EtlViewer.Viewer.UIUtils
{
    using EtlViewer.Viewer.Models;
    using System;
    using System.Windows.Data;

    class EventLevelAggregator : IValueConverter
    {
        public object Convert(object values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null)
            {
                return string.Empty;
            }

            CollectionViewGroup group = values as CollectionViewGroup;
            long total = 0;

            if (group.IsBottomLevel)
            {
                foreach (EventStat s in group.Items)
                {
                    total += s.Count;
                }
            }
            else
            {
                foreach (CollectionViewGroup g in group.Items)
                {
                    foreach (EventStat s in g.Items)
                    {
                        total += s.Count;
                    }
                }
            }

            return total;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}