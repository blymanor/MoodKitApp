using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MoodKitApp.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int starCount && parameter is string param)
            {
                if (int.TryParse(param, out int targetStar))
                {
                    return starCount >= targetStar;
                }
                if (param == "0") // กรณีไม่มีดาว
                {
                    return starCount == 0;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}