using System;
using Windows.UI.Xaml.Data;

namespace AtmoSync.Shared
{
    class SoundStatusToCanBeStoppedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (Status)value == Status.Playing || (Status)value == Status.Paused;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
