using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace AtmoSync.Shared
{
    class VolumeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (double)value > 0 ? Symbol.Volume : Symbol.Mute;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
