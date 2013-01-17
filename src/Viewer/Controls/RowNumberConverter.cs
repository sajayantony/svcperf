namespace EtlViewer
{
    using System;
    using System.Windows.Controls;
    using System.Windows.Data;
    
    class RowNumberConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //get the grid and the item
            Object item = values[0];
            DataGridRow row = values[1] as DataGridRow;

            int index = row.GetIndex();
            return index.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
