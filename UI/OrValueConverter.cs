using System;
using System.Globalization;
using System.Windows.Data;

namespace QQS_UI.UI
{
    internal class OrValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parmeter, CultureInfo culture)
        {
            foreach (object obj in values)
            {
                if ((bool)obj)
                {
                    return true;
                }
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
