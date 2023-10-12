using System;
using System.Globalization;
using System.Windows.Data;

namespace ControlUnitFredrik.Utils;

public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOn) return isOn ? "Status: PÅ" : "Status: AV";

        return "Okänt";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}