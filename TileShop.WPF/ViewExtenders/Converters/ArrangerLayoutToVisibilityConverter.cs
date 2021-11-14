﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ImageMagitek;

namespace TileShop.WPF.Converters;

public class ArrangerLayoutToVisibilityConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		var parameterString = parameter as string;

		if (value is ElementLayout state && Enum.TryParse(parameterString, out ElementLayout stateVisibility)) {
			var visibility = (state, stateVisibility) switch {
				(ElementLayout.Tiled, ElementLayout.Tiled) => Visibility.Visible,
				(ElementLayout.Single, ElementLayout.Single) => Visibility.Visible,
				_ => Visibility.Collapsed
			};

			return visibility;
		}

		return Visibility.Hidden;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
