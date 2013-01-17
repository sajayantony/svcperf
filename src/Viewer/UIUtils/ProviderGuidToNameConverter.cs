namespace EtlViewer.Viewer.UIUtils
{
    using EtlViewer.QueryFx;
    using System;
    using System.Windows.Data;

    class ProviderGuidToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Resolver resolver;
            if (value is Guid && SymbolHelper.TryGetValue((Guid)value, out resolver))
            {
                return resolver.ProviderName;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
