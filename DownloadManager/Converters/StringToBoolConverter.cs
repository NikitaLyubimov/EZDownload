using System;
using System.Globalization;
using System.Windows.Data;

namespace DownloadManager.Converters
{
    public class StringToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value[0].ToString()) || string.IsNullOrEmpty(value[1].ToString()))
                return false;
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
