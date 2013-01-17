namespace EtlViewer.Viewer.UIUtils
{
    using System.Windows.Data;
    using System;

    class SearchTermConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length < 1)
            {
                return false;
            }
            string searchTerm = values[0] != null ? values[0].ToString() : string.Empty;
            string stringValue;
            if (values.Length >= 1 && !string.IsNullOrEmpty(searchTerm))
            {
                for (int i = 1; i < values.Length; i++)
                {
                    stringValue = values[i].ToString();
                    bool isMatch = !string.IsNullOrEmpty(stringValue) && stringValue.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isMatch)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}