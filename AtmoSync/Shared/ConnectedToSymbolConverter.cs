using System;
using Windows.UI.Xaml.Data;

namespace AtmoSync.Shared
{
    class ConnectedToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? "Target" : "Important";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
