﻿using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using ImageMagitek.ExtensionMethods;

namespace ImageMagitek.Colors.Serialization;

public interface IColorSourceSerializer {
	IColor[] LoadColors(IColorSource[] sources, DataFile df, ColorModel colorModel, int entries);
	void StoreColors(IList<IColorSource> sources, DataFile df, IList<ColorRgba32> nativeColors, IList<IColor> foreignColors);
}

public class ColorSourceSerializer : IColorSourceSerializer {
	private readonly IColorFactory _colorFactory;
	private readonly byte[] _colorBuffer = new byte[8];

	public ColorSourceSerializer(IColorFactory colorFactory) => _colorFactory = colorFactory;

	public IColor[] LoadColors(IColorSource[] sources, DataFile df, ColorModel colorModel, int entries) {
		var result = new IColor[entries];
		var currentEntry = 0;
		var size = _colorFactory.CreateColor(colorModel).Size;

		foreach (var source in sources) {
			if (currentEntry == entries) {
				return result;
			}

			if (source is FileColorSource fileSource) {
				result[currentEntry] = ReadFileColor(df, fileSource.Offset, colorModel, size, fileSource.Endian);
			} else if (source is ProjectNativeColorSource nativeSource) {
				result[currentEntry] = nativeSource.Value;
			} else if (source is ProjectForeignColorSource foreignSource) {
				result[currentEntry] = foreignSource.Value;
			} else if (source is ScatteredColorSource scatteredSource) {

			}

			currentEntry++;
		}

		return result;
	}

	private IColor ReadFileColor(DataFile df, FileBitAddress offset, ColorModel colorModel, int size, Endian endian) {
		var readSize = (size + 7) / 8;
		var paletteData = df.Stream.ReadUnshifted(offset, readSize * 8);

		df.Stream.Seek(offset.FileOffset, SeekOrigin.Begin);
		df.Stream.ReadUnshifted(offset, size, _colorBuffer);

		uint readColor;
		if (readSize == 1) {
			readColor = _colorBuffer[0];
		} else if (readSize == 2 && endian == Endian.Little) {
			readColor = BinaryPrimitives.ReadUInt16LittleEndian(_colorBuffer);
		} else if (readSize == 2 && endian == Endian.Big) {
			readColor = BinaryPrimitives.ReadUInt16BigEndian(_colorBuffer);
		} else if (readSize == 3) {
			readColor = _colorBuffer[0];
			readColor |= ((uint)_colorBuffer[1]) << 8;
			readColor |= ((uint)_colorBuffer[2]) << 16;
		} else if (readSize == 4 && endian == Endian.Little) {
			readColor = BinaryPrimitives.ReadUInt32LittleEndian(_colorBuffer);
		} else if (readSize == 4 && endian == Endian.Big) {
			readColor = BinaryPrimitives.ReadUInt32BigEndian(_colorBuffer);
		} else {
			throw new NotSupportedException($"{nameof(LoadColors)}: Palette formats with entry sizes larger than 4 bytes are not supported");
		}

		return _colorFactory.CreateColor(colorModel, readColor);
	}

	public void StoreColors(IList<IColorSource> sources, DataFile df, IList<ColorRgba32> nativeColors, IList<IColor> foreignColors) {
		for (var i = 0; i < sources.Count; i++) {
			if (sources[i] is FileColorSource fileSource) {
				WriteFileColor(df, fileSource.Offset, foreignColors[i], fileSource.Endian);
			} else if (sources[i] is ProjectNativeColorSource nativeSource) {
				nativeSource.Value = nativeColors[i];
			} else if (sources[i] is ProjectForeignColorSource foreignSource) {
				foreignSource.Value = foreignColors[i];
			} else if (sources[i] is ScatteredColorSource scatteredSource) {
				throw new NotSupportedException();
			}
		}
	}

	private void WriteFileColor(DataFile df, FileBitAddress offset, IColor foreignColor, Endian endian) {
		var writeSize = (foreignColor.Size + 7) / 8;

		if (writeSize == 1) {
			_colorBuffer[0] = (byte)foreignColor.Color;
		} else if (writeSize == 2 && endian == Endian.Little) {
			BinaryPrimitives.WriteUInt16LittleEndian(_colorBuffer, (ushort)foreignColor.Color);
		} else if (writeSize == 2 && endian == Endian.Big) {
			BinaryPrimitives.WriteUInt16BigEndian(_colorBuffer, (ushort)foreignColor.Color);
		} else if (writeSize == 3) {
			_colorBuffer[0] = (byte)foreignColor.Color;
			_colorBuffer[1] = (byte)(foreignColor.Color >> 8);
			_colorBuffer[2] = (byte)(foreignColor.Color >> 16);
		} else if (writeSize == 4 && endian == Endian.Little) {
			BinaryPrimitives.WriteUInt32LittleEndian(_colorBuffer, foreignColor.Color);
		} else if (writeSize == 4 && endian == Endian.Big) {
			BinaryPrimitives.WriteUInt32BigEndian(_colorBuffer, foreignColor.Color);
		}

		df.Stream.Seek(offset.FileOffset, SeekOrigin.Begin);
		df.Stream.Write(_colorBuffer, 0, writeSize);
	}
}
