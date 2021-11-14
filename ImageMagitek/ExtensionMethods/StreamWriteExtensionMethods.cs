using System;
using System.IO;

namespace ImageMagitek.ExtensionMethods;

public static class StreamWriteExtensionMethods {
	public static void WriteUnshifted(this Stream stream, FileBitAddress address, int writeBits, ReadOnlySpan<byte> writeBuffer) {
		stream.Seek(address.FileOffset, SeekOrigin.Begin);
		stream.WriteUnshifted(address.BitOffset, writeBits, writeBuffer);
	}

	private static void WriteUnshifted(this Stream stream, int skipBits, int writeBits, ReadOnlySpan<byte> writeBuffer) {
		var totalBytes = (skipBits + writeBits + 7) / 8;

		if (totalBytes == 1) {
			var firstByte = (byte)stream.ReadByte();
			stream.Seek(-1, SeekOrigin.Current);

			var merged = MergeByte(firstByte, writeBuffer[0], skipBits, writeBits);
			stream.WriteByte(merged);
			return;
		}

		var writtenBytes = 0;

		if (skipBits != 0) {
			var firstByte = (byte)stream.ReadByte();
			stream.Seek(-1, SeekOrigin.Current);

			var merged = MergeByte(firstByte, writeBuffer[0], skipBits, 8 - skipBits);
			stream.WriteByte(merged);
			writtenBytes++;
		}

		var lastBits = (skipBits + writeBits) % 8;

		if (lastBits != 0) {
			var span = writeBuffer.Slice(writtenBytes, totalBytes - writtenBytes - 1);
			stream.Write(span);

			var lastByte = (byte)stream.ReadByte();
			stream.Seek(-1, SeekOrigin.Current);

			var merged = MergeByte(lastByte, writeBuffer[totalBytes - 1], 0, lastBits);
			stream.WriteByte(merged);
		} else {
			var span = writeBuffer[writtenBytes..totalBytes];
			stream.Write(span);
		}
	}

	public static void WriteShifted(this Stream stream, FileBitAddress address, int writeBits, ReadOnlySpan<byte> writeBuffer) {
		stream.Seek(address.FileOffset, SeekOrigin.Begin);
		stream.WriteShifted(address.BitOffset, writeBits, writeBuffer);
	}

	private static void WriteShifted(this Stream stream, int skipBits, int writeBits, ReadOnlySpan<byte> writeBuffer) {
		var totalWriteBytes = (skipBits + writeBits + 7) / 8;
		var firstWriteBytes = (writeBits + 7) / 8;

		if (skipBits == 0) {
			stream.WriteUnshifted(skipBits, writeBits, writeBuffer);
			return;
		} else {
			var buffer = new byte[totalWriteBytes]; // Allocation because ShiftRight does in-place shifting
			var bufferSpan = buffer.AsSpan();
			writeBuffer.CopyTo(buffer);
			bufferSpan.ShiftRight(skipBits);
			stream.WriteUnshifted(skipBits, writeBits, bufferSpan);
		}
	}

	private static byte MergeByte(byte original, byte write, int skipBits, int writeBits) {
		var mask = ((1 << writeBits) - 1) << (8 - skipBits - writeBits);
		write = (byte)(write & mask);
		mask = ~mask;
		var merged = original & mask;
		merged |= write;
		return (byte)merged;
	}
}
