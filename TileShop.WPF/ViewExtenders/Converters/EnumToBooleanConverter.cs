using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TileShop.WPF.Converters;

public class EnumToBooleanConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (parameter is not string parameterString) {
			return DependencyProperty.UnsetValue;
		}

		if (Enum.IsDefined(value.GetType(), value) == false) {
			return DependencyProperty.UnsetValue;
		}

		var parameterValue = Enum.Parse(value.GetType(), parameterString);

		return parameterValue.Equals(value);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		if (parameter is not string parameterString) {
			return DependencyProperty.UnsetValue;
		}

		return Enum.Parse(targetType, parameterString);
	}
}
