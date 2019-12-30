﻿using System;
using System.Globalization;
using Xamarin.Forms;

namespace BracePLUS
{
    public class GuidConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Guid guid = (Guid)value;
            return guid.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("GuidConverter is one-way");
        }
    }
}
