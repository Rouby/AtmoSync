using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace AtmoSync.Shared
{
    class SoundStatusPauseVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (Status)value == Status.Playing ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
