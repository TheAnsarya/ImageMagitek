﻿using System;

namespace ImageMagitek.Colors.Converters;

public sealed class ColorConverterBgr9 : IColorConverter<ColorBgr9> {
	private static readonly byte[] _toNativeTable = new byte[]
	{
			0, 36, 72, 109, 145, 182, 218, 255
	};

	public ColorBgr9 ToForeignColor(ColorRgba32 nc) {
		var r = (byte)MathF.Round(nc.r / (255f / 7f));
		var g = (byte)MathF.Round(nc.g / (255f / 7f));
		var b = (byte)MathF.Round(nc.b / (255f / 7f));

		return new ColorBgr9(r, g, b);
	}

	public ColorRgba32 ToNativeColor(ColorBgr9 fc) {
		var r = _toNativeTable[fc.r & 0xFE];
		var g = _toNativeTable[fc.g & 0xFE];
		var b = _toNativeTable[fc.b & 0xFE];
		byte a = 0xFF;

		return new ColorRgba32(r, g, b, a);
	}
}
