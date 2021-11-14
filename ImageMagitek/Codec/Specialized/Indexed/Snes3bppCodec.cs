﻿using System;

namespace ImageMagitek.Codec;

public sealed class Snes3bppCodec : IndexedCodec {
	public override string Name => "SNES 3bpp";
	public override int Width { get; }
	public override int Height { get; }
	public override int StorageSize => 3 * Width * Height;
	public override ImageLayout Layout => ImageLayout.Tiled;
	public override int ColorDepth => 3;
	public override bool CanEncode => true;

	public override int DefaultWidth => 8;
	public override int DefaultHeight => 8;
	public override int WidthResizeIncrement => 1;
	public override int HeightResizeIncrement => 1;
	public override bool CanResize => true;

	private BitStream _bitStream;

	public Snes3bppCodec() {
		Width = DefaultWidth;
		Height = DefaultHeight;
		Initialize();
	}

	public Snes3bppCodec(int width, int height) {
		Width = width;
		Height = height;
		Initialize();
	}

	private void Initialize() {
		_foreignBuffer = new byte[(StorageSize + 7) / 8];
		_nativeBuffer = new byte[Height, Width];
		_bitStream = BitStream.OpenRead(_foreignBuffer, StorageSize);
	}

	public override byte[,] DecodeElement(in ArrangerElement el, ReadOnlySpan<byte> encodedBuffer) {
		if (encodedBuffer.Length * 8 < StorageSize) // Decoding would require data past the end of the buffer
{
			throw new ArgumentException(nameof(encodedBuffer));
		}

		encodedBuffer.Slice(0, _foreignBuffer.Length).CopyTo(_foreignBuffer);

		_bitStream = BitStream.OpenRead(_foreignBuffer, StorageSize);

		var offsetPlane1 = 0;
		var offsetPlane2 = Width;
		var offsetPlane3 = Width * Height * 2;

		for (var y = 0; y < Height; y++) {
			for (var x = 0; x < Width; x++) {
				_bitStream.SeekAbsolute(offsetPlane1);
				var bp1 = _bitStream.ReadBit();
				_bitStream.SeekAbsolute(offsetPlane2);
				var bp2 = _bitStream.ReadBit();
				_bitStream.SeekAbsolute(offsetPlane3);
				var bp3 = _bitStream.ReadBit();

				var palIndex = (bp1 << 0) | (bp2 << 1) | (bp3 << 2);
				_nativeBuffer[y, x] = (byte)palIndex;

				offsetPlane1++;
				offsetPlane2++;
				offsetPlane3++;
			}

			offsetPlane1 += Width;
			offsetPlane2 += Width;
		}

		return _nativeBuffer;
	}

	public override ReadOnlySpan<byte> EncodeElement(in ArrangerElement el, byte[,] imageBuffer) {
		if (imageBuffer.GetLength(0) != Height || imageBuffer.GetLength(1) != Width) {
			throw new ArgumentException(nameof(imageBuffer));
		}

		var bs = BitStream.OpenWrite(StorageSize, 8);

		var offsetPlane1 = 0;
		var offsetPlane2 = Width;
		var offsetPlane3 = Width * Height * 2;

		for (var y = 0; y < Height; y++) {
			for (var x = 0; x < Width; x++) {
				var index = imageBuffer[y, x];

				var bp1 = (byte)(index & 1);
				var bp2 = (byte)((index >> 1) & 1);
				var bp3 = (byte)((index >> 2) & 1);

				bs.SeekAbsolute(offsetPlane1);
				bs.WriteBit(bp1);
				bs.SeekAbsolute(offsetPlane2);
				bs.WriteBit(bp2);
				bs.SeekAbsolute(offsetPlane3);
				bs.WriteBit(bp3);

				offsetPlane1++;
				offsetPlane2++;
				offsetPlane3++;
			}
			offsetPlane1 += Width;
			offsetPlane2 += Width;
		}

		return bs.Data;
	}
}
