namespace ImageMagitek.Colors.Converters;

public sealed class ColorConverterBgr15 : IColorConverter<ColorBgr15> {
	public ColorBgr15 ToForeignColor(ColorRgba32 nc) {
		var r = (byte)(nc.r >> 3);
		var g = (byte)(nc.g >> 3);
		var b = (byte)(nc.b >> 3);

		return new ColorBgr15(r, g, b);
	}

	public ColorRgba32 ToNativeColor(ColorBgr15 fc) {
		var r = (byte)(fc.r << 3);
		var g = (byte)(fc.g << 3);
		var b = (byte)(fc.b << 3);
		byte a = 0xFF;

		return new ColorRgba32(r, g, b, a);
	}
}
