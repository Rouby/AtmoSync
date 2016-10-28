using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace AtmoSync.Shared
{
    class IsSyncedToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Symbol.Accept : Symbol.Sync;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
