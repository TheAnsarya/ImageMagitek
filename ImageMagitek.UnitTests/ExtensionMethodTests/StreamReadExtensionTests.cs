﻿using System.IO;
using ImageMagitek.ExtensionMethods;
using NUnit.Framework;

namespace ImageMagitek.UnitTests;

[TestFixture]
public class StreamReadExtensionTests {
	[TestCaseSource(typeof(StreamReadExtensionTestCases), "ReadUnshiftedCases")]
	public void ReadUnshifted_AsExpected(byte[] data, FileBitAddress offset, int numBits, byte[] expected) {
		var stream = new MemoryStream(data);
		var actual = new byte[(numBits + 7) / 8];
		stream.ReadUnshifted(offset, numBits, actual);

		CollectionAssert.AreEqual(expected, actual);
	}

	[TestCaseSource(typeof(StreamReadExtensionTestCases), "ReadShiftedCases")]
	public void ReadShifted_AsExpected(byte[] data, FileBitAddress offset, int numBits, byte[] expected) {
		var stream = new MemoryStream(data);
		var actual = new byte[(numBits + 7) / 8];
		stream.ReadShifted(offset, numBits, actual);

		CollectionAssert.AreEqual(expected, actual);
	}
}
