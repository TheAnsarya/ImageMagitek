using System;
using System.Globalization;
using System.Windows.Data;

namespace ColorPicker.Converters;

internal class ProportialConverter : IMultiValueConverter {
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
		if (values[0] is double zero && values[1] is double one && values[2] is double two) {
			return zero * (one / two);
		}

		return 0;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
