﻿using System;
using ImageMagitek.Colors;

namespace ImageMagitek.Codec;

public sealed class Bmp24Codec : DirectCodec {
	public override string Name => "Bmp24";
	public override int Width { get; } = 8;
	public override int Height { get; } = 8;
	public override ImageLayout Layout => ImageLayout.Single;
	public override int ColorDepth => 24;
	public override int StorageSize => Width * Height * 24;
	public override int RowStride { get; } = 0;
	public override int ElementStride { get; } = 0;
	public override bool CanEncode => true;

	public override bool CanResize => true;
	public override int WidthResizeIncrement => 1;
	public override int HeightResizeIncrement => 1;
	public override int DefaultWidth => 8;
	public override int DefaultHeight => 8;

	private readonly BitStream _bitStream;

	public Bmp24Codec() {
		Width = DefaultWidth;
		Height = DefaultHeight;

		_foreignBuffer = new byte[(StorageSize + 7) / 8];
		_nativeBuffer = new ColorRgba32[Height, Width];

		_bitStream = BitStream.OpenRead(_foreignBuffer, StorageSize);
	}

	public Bmp24Codec(int width, int height) {
		Width = width;
		Height = height;

		_foreignBuffer = new byte[(StorageSize + 7) / 8];
		_nativeBuffer = new ColorRgba32[Height, Width];

		_bitStream = BitStream.OpenRead(_foreignBuffer, StorageSize);
	}

	public override ColorRgba32[,] DecodeElement(in ArrangerElement el, ReadOnlySpan<byte> encodedBuffer) {
		if (encodedBuffer.Length * 8 < StorageSize) {
			throw new ArgumentException(nameof(encodedBuffer));
		}

		encodedBuffer.Slice(0, _foreignBuffer.Length).CopyTo(_foreignBuffer);
		_bitStream.SeekAbsolute(0);

		for (var y = el.Height - 1; y >= 0; y--) {
			for (var x = 0; x < el.Width; x++) {
				var b = _bitStream.ReadByte();
				var g = _bitStream.ReadByte();
				var r = _bitStream.ReadByte();

				_nativeBuffer[y, x] = new ColorRgba32(r, g, b, 0xFF);
			}
		}

		return NativeBuffer;
	}

	public override ReadOnlySpan<byte> EncodeElement(in ArrangerElement el, ColorRgba32[,] imageBuffer) {
		if (imageBuffer.GetLength(0) != Height || imageBuffer.GetLength(1) != Width) {
			throw new ArgumentException(nameof(imageBuffer));
		}

		var bs = BitStream.OpenWrite(StorageSize, 8);

		for (var y = el.Height - 1; y >= 0; y--) {
			for (var x = 0; x < el.Width; x++) {
				var imageColor = imageBuffer[y, x];
				bs.WriteByte(imageColor.B);
				bs.WriteByte(imageColor.G);
				bs.WriteByte(imageColor.R);
			}
		}

		return bs.Data;
	}
}
