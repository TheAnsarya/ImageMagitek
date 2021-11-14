﻿using System.IO;
using ImageMagitek.Colors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageMagitek;

public sealed class ImageSharpFileAdapter : IImageFileAdapter {
	public void SaveImage(byte[] image, Arranger arranger, string imagePath) {
		var width = arranger.ArrangerPixelSize.Width;
		var height = arranger.ArrangerPixelSize.Height;
		using var outputImage = new Image<Rgba32>(width, height);

		var srcidx = 0;

		for (var y = 0; y < height; y++) {
			var span = outputImage.GetPixelRowSpan(y);

			for (var x = 0; x < width; x++, srcidx++) {
				if (arranger.GetElementAtPixel(x, y) is ArrangerElement el) {
					var pal = el.Palette;
					var index = image[srcidx];
					var color = pal[index];
					span[x] = color.ToRgba32();
				}
			}
		}

		using var outputStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read);
		outputImage.SaveAsPng(outputStream);
	}

	public void SaveImage(ColorRgba32[] image, int width, int height, string imagePath) {
		using var outputImage = new Image<Rgba32>(width, height);
		var srcidx = 0;

		for (var y = 0; y < height; y++) {
			var span = outputImage.GetPixelRowSpan(y);
			for (var x = 0; x < width; x++, srcidx++) {
				span[x] = image[srcidx].ToRgba32();
			}
		}

		using var outputStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read);
		outputImage.SaveAsPng(outputStream);
	}

	public byte[] LoadImage(string imagePath, Arranger arranger, ColorMatchStrategy matchStrategy) {
		using var inputImage = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);
		var width = inputImage.Width;
		var height = inputImage.Height;

		var outputImage = new byte[width * height];
		var destidx = 0;

		for (var y = 0; y < height; y++) {
			var span = inputImage.GetPixelRowSpan(y);
			for (var x = 0; x < width; x++, destidx++) {
				if (arranger.GetElementAtPixel(x, y) is ArrangerElement el) {
					var pal = el.Palette;
					var color = new ColorRgba32(span[x].PackedValue);
					var palIndex = pal.GetIndexByNativeColor(color, matchStrategy);
					outputImage[destidx] = palIndex;
				}
			}
		}

		return outputImage;
	}

	public MagitekResult TryLoadImage(string imagePath, Arranger arranger, ColorMatchStrategy matchStrategy, out byte[] image) {
		using var inputImage = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);
		var width = inputImage.Width;
		var height = inputImage.Height;

		if (width != arranger.ArrangerPixelSize.Width || height != arranger.ArrangerPixelSize.Height) {
			image = default;
			return new MagitekResult.Failed($"Arranger dimensions ({arranger.ArrangerPixelSize.Width}, {arranger.ArrangerPixelSize.Height})" +
				$" do not match image dimensions ({width}, {height})");
		}

		image = new byte[width * height];
		var destidx = 0;

		for (var y = 0; y < height; y++) {
			var span = inputImage.GetPixelRowSpan(y);
			for (var x = 0; x < width; x++, destidx++) {
				if (arranger.GetElementAtPixel(x, y) is ArrangerElement el) {
					var pal = el.Palette;
					var color = new ColorRgba32(span[x].PackedValue);

					if (pal.TryGetIndexByNativeColor(color, matchStrategy, out var palIndex)) {
						image[destidx] = palIndex;
					} else {
						return new MagitekResult.Failed($"Could not match image color (R: {color.R}, G: {color.G}, B: {color.B}, A: {color.A}) within palette '{pal.Name}'");
					}
				}
			}
		}

		return MagitekResult.SuccessResult;
	}

	public ColorRgba32[] LoadImage(string imagePath) {
		using var inputImage = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);
		var width = inputImage.Width;
		var height = inputImage.Height;

		var outputImage = new ColorRgba32[width * height];
		var destidx = 0;

		for (var y = 0; y < height; y++) {
			var span = inputImage.GetPixelRowSpan(y);
			for (var x = 0; x < width; x++) {
				var color = new ColorRgba32(span[x].PackedValue);
				outputImage[destidx] = color;
				destidx++;
			}
		}

		return outputImage;
	}
}
