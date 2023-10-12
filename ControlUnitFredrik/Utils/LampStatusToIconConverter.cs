using System;
using System.Globalization;
using System.Windows.Data;

namespace ControlUnitFredrik.Utils;

public class LampStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isOn = (bool)value;


        return isOn ? "LightbulbOutline" : "Lightbulb";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}