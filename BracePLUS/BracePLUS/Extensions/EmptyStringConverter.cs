using System;
using System.Globalization;
using Xamarin.Forms;

namespace BracePLUS
{
    public class EmptyStringConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string str = (string)value;
            return String.IsNullOrWhiteSpace(str) ? "<unnamed device>" : str;

        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException("EmptyStringConverter is one-way");
        }
    }
}
