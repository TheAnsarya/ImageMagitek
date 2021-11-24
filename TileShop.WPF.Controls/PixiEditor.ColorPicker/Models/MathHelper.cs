namespace ColorPicker.Models;

internal static class MathHelper {
	public static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

	public static double Mod(double value, double m) => ((value % m) + m) % m;
}
