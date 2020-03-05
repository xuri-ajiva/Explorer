#region using

using System;
using System.Globalization;
using System.Windows.Data;

#endregion

namespace ExplorerWpf.CustomControls {
    [ValueConversion( typeof(int), typeof(int) )]
    public class InvertSignConverter : IValueConverter {
        public static readonly InvertSignConverter Instance = new InvertSignConverter();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var val = (double) value;
            return val * -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var val = (double) value;
            return val * -1;
        }

        #endregion

    }
}
